using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Hubs;
using AICodeReviewer.Web.Infrastructure.Extensions;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace AICodeReviewer.Web.Infrastructure.Services
{
    /// <summary>
    /// Coordinator service for high-level code analysis orchestration.
    /// Delegates to specialized services for specific responsibilities.
    /// </summary>
    public class AnalysisCoordinatorService : IAnalysisService
    {
        private readonly ILogger<AnalysisCoordinatorService> _logger;
        private readonly IValidationService _validationService;
        private readonly IContentExtractionService _contentExtractionService;
        private readonly IDocumentRetrievalService _documentRetrievalService;
        private readonly IAIAnalysisOrchestrator _aiAnalysisOrchestrator;
        private readonly IResultProcessorService _resultProcessorService;
        private readonly ISignalRBroadcastService _signalRService;
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly IMemoryCache _cache;

        private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
            .SetSize(1);

        public AnalysisCoordinatorService(
            ILogger<AnalysisCoordinatorService> logger,
            IValidationService validationService,
            IContentExtractionService contentExtractionService,
            IDocumentRetrievalService documentRetrievalService,
            IAIAnalysisOrchestrator aiAnalysisOrchestrator,
            IResultProcessorService resultProcessorService,
            ISignalRBroadcastService signalRService,
            IHubContext<ProgressHub> hubContext,
            IMemoryCache cache)
        {
            _logger = logger;
            _validationService = validationService;
            _contentExtractionService = contentExtractionService;
            _documentRetrievalService = documentRetrievalService;
            _aiAnalysisOrchestrator = aiAnalysisOrchestrator;
            _resultProcessorService = resultProcessorService;
            _signalRService = signalRService;
            _hubContext = hubContext;
            _cache = cache;
        }

        public async Task<(string analysisId, bool success, string? error)> StartAnalysisAsync(
            RunAnalysisRequest request,
            ISession session,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            try
            {
                // Use request data or fall back to session data
                var defaultRepositoryPath = Path.Combine(environment.ContentRootPath, "..");
                var repositoryPath = request.RepositoryPath ?? session.GetString("RepositoryPath") ?? defaultRepositoryPath;
                var selectedDocuments = request.SelectedDocuments ?? session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
                var documentsFolder = !string.IsNullOrEmpty(request.DocumentsFolder) ? request.DocumentsFolder : session.GetString("DocumentsFolder") ?? Path.Combine(environment.ContentRootPath, "..", "Documents");
                var language = request.Language ?? session.GetString("Language") ?? "NET";
                var analysisType = request.AnalysisType ?? AnalysisType.Uncommitted;
                var commitId = request.CommitId;
                var filePath = request.FilePath;
                
                // DEBUG: Log document selection details
                _logger.LogInformation($"[RunAnalysis] Request SelectedDocuments: {(request.SelectedDocuments != null ? string.Join(", ", request.SelectedDocuments) : "null")}");
                _logger.LogInformation($"[RunAnalysis] Session SelectedDocuments: {string.Join(", ", session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>())}");
                _logger.LogInformation($"[RunAnalysis] Final selectedDocuments count: {selectedDocuments.Count}");
                
                var apiKey = configuration["OpenRouter:ApiKey"] ?? "";
                var model = request.Model ?? configuration["OpenRouter:Model"] ?? "";
                var fallbackModel = configuration["OpenRouter:FallbackModel"] ?? "";

                // Store language in session for consistency
                session.SetString("Language", language);

                // Validate the request using validation service
                var (isValid, validationError, resolvedFilePath) = await _validationService.ValidateAnalysisRequestAsync(request, session, environment);
                if (!isValid)
                {
                    _logger.LogWarning("[RunAnalysis] Validation failed: {Error}", validationError);
                    return ("", false, validationError);
                }

                filePath = resolvedFilePath ?? filePath;

                // Generate unique analysis ID
                var analysisId = Guid.NewGuid().ToString();

                // Create initial analysis result and store in cache
                var analysisResult = new AnalysisResult
                {
                    Status = "Starting",
                    CreatedAt = DateTime.UtcNow
                };
                
                // Store initial result in cache with 30-minute expiration and size
                _cache.Set($"analysis_{analysisId}", analysisResult, CacheEntryOptions);
                _logger.LogInformation($"[Analysis {analysisId}] Initial analysis result stored in cache");

                // Start background analysis with captured references
                var docsFolder = request.DocumentsFolder ?? session.GetString("DocumentsFolder") ?? Path.Combine(environment.ContentRootPath, "..", "Documents");
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RunBackgroundAnalysisAsync(
                            analysisId,
                            repositoryPath,
                            selectedDocuments,
                            docsFolder,
                            apiKey,
                            model,
                            fallbackModel,
                            language,
                            analysisType,
                            commitId,
                            filePath,
                            session);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background analysis failed for analysis {AnalysisId}", analysisId);
                        await _signalRService.BroadcastErrorAsync(analysisId, $"Background analysis error: {ex.Message}");
                    }
                }).ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        _logger.LogError(t.Exception, "Unobserved exception in background task for analysis {AnalysisId}", analysisId);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);

                _logger.LogInformation("Started analysis {AnalysisId} of type {AnalysisType}", analysisId, analysisType);
                return (analysisId, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting analysis");
                return ("", false, ex.Message);
            }
        }

        public ProgressDto GetAnalysisStatus(string analysisId)
        {
            if (string.IsNullOrEmpty(analysisId))
            {
                _logger.LogWarning("Analysis status requested with empty analysis ID");
                return new ProgressDto("NotStarted", null, null, false);
            }

            if (_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result))
            {
                _logger.LogInformation($"[Analysis {analysisId}] Serving result: Status={result.Status}, ResultLength={result.Result?.Length ?? 0}");
                return new ProgressDto(result.Status, result.Result, result.Error, result.IsComplete);
            }

            _logger.LogWarning("Analysis {AnalysisId} not found in cache", analysisId);
            return new ProgressDto("NotFound", null, "Analysis not found or expired", true);
        }

        public void StoreAnalysisId(string analysisId, ISession session)
        {
            if (string.IsNullOrEmpty(analysisId))
            {
                _logger.LogWarning("Attempted to store empty analysis ID");
                return;
            }
            
            session.SetString("AnalysisId", analysisId);
            _logger.LogInformation("Stored analysis ID {AnalysisId} in session", analysisId);
        }

        private async Task RunBackgroundAnalysisAsync(
            string analysisId,
            string repositoryPath,
            List<string> selectedDocuments,
            string documentsFolder,
            string apiKey,
            string model,
            string fallbackModel,
            string language,
            AnalysisType analysisType,
            string? commitId,
            string? filePath,
            ISession? session)
        {
            _logger.LogInformation($"[Analysis {analysisId}] Starting background analysis");
            _logger.LogInformation($"[Analysis {analysisId}] Repository path: {repositoryPath}");
            _logger.LogInformation($"[Analysis {analysisId}] Selected documents: {string.Join(", ", selectedDocuments)}");
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder: {documentsFolder}");
            _logger.LogInformation($"[Analysis {analysisId}] Language: {language}");
            _logger.LogInformation($"[Analysis {analysisId}] API key configured: {!string.IsNullOrEmpty(apiKey)}");
            _logger.LogInformation($"[Analysis {analysisId}] Primary model: {model}");
            _logger.LogInformation($"[Analysis {analysisId}] Fallback model: {fallbackModel ?? "None"}");

            try
            {
                // Get current result from cache
                _logger.LogInformation($"[Analysis {analysisId}] Checking cache for existing result");
                if (!_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result))
                {
                    _logger.LogError($"[Analysis {analysisId}] Analysis result not found in cache - aborting");
                    return;
                }
                _logger.LogInformation($"[Analysis {analysisId}] Found existing result in cache");

                // Update status to content extraction
                result.Status = "Reading git changes...";
                await _signalRService.BroadcastProgressWithModelAsync(analysisId, "Reading git changes...", model, fallbackModel);
                _cache.Set($"analysis_{analysisId}", result, CacheEntryOptions);
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Reading git changes...'");

                // Extract content
                var extractionResult = await _contentExtractionService.ExtractContentAsync(repositoryPath, analysisType, commitId, filePath);
                var (content, contentError, isFileContent, contentErrorMsg) = extractionResult;

                if (contentError)
                {
                    _logger.LogError($"[Analysis {analysisId}] Content extraction failed: {contentErrorMsg}");
                    await _resultProcessorService.ProcessAndBroadcastAsync(analysisId, "", contentErrorMsg, true, model, fallbackModel, session ?? throw new ArgumentNullException(nameof(session)));
                    return;
                }

                // Cache the content
                _cache.Set($"content_{analysisId}", content, CacheEntryOptions);
                _logger.LogInformation($"[Analysis {analysisId}] Content stored in cache with length: {content.Length}");

                _logger.LogInformation($"[Analysis {analysisId}] Content extraction complete, length: {content.Length}");

                // Update status to document loading
                result.Status = "Loading documents...";
                await _signalRService.BroadcastProgressWithModelAsync(analysisId, "Loading documents...", model, fallbackModel);
                _cache.Set($"analysis_{analysisId}", result, CacheEntryOptions);
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Loading documents...'");

                // Load documents
                var codingStandards = await _documentRetrievalService.LoadDocumentsAsync(selectedDocuments, documentsFolder);
                _logger.LogInformation($"[Analysis {analysisId}] Loaded {codingStandards.Count} documents");

                // Update status to AI analysis
                result.Status = "AI analysis...";
                await _signalRService.BroadcastProgressWithModelAsync(analysisId, $"AI analysis... (Using: {model})", model, fallbackModel);
                _cache.Set($"analysis_{analysisId}", result, CacheEntryOptions);
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'AI analysis...' with model {model}");

                // Get requirements
                var requirements = "Follow .NET best practices and coding standards";
                _logger.LogInformation($"[Analysis {analysisId}] Requirements: {requirements}");

                // Perform AI analysis
                var aiResult = await _aiAnalysisOrchestrator.AnalyzeAsync(content, codingStandards, requirements, apiKey, model, fallbackModel, language, isFileContent);
                var (aiAnalysis, aiError, aiErrorMsg, usedModel) = aiResult;

                // Process results
                await _resultProcessorService.ProcessAndBroadcastAsync(analysisId, aiAnalysis, aiErrorMsg, aiError, usedModel, fallbackModel, session ?? throw new ArgumentNullException(nameof(session)));

                _logger.LogInformation($"[Analysis {analysisId}] Background analysis task completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Unhandled exception in background analysis");
                try
                {
                    await _signalRService.BroadcastErrorAsync(analysisId, $"Analysis error: {ex.Message}");
                    _logger.LogInformation($"[Analysis {analysisId}] Set error status due to exception: {ex.Message}");
                }
                catch (Exception broadcastEx)
                {
                    _logger.LogError(broadcastEx, $"[Analysis {analysisId}] Failed to broadcast error status");
                }
            }
        }
    }
}