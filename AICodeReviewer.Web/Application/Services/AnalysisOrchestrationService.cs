using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Extensions;
using AICodeReviewer.Web.Infrastructure.Services;
using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Application.Services;

/// <summary>
/// Application service for orchestrating code analysis operations
/// </summary>
public class AnalysisOrchestrationService : IAnalysisService
{
    private readonly ILogger<AnalysisOrchestrationService> _logger;
    private readonly IValidationService _validationService;
    private readonly IContentExtractionService _contentExtractionService;
    private readonly IDocumentRetrievalService _documentRetrievalService;
    private readonly IAIAnalysisOrchestrator _aiAnalysisOrchestrator;
    private readonly IResultProcessorService _resultProcessorService;
    private readonly AnalysisCacheService _cacheService;
    private readonly BackgroundTaskService _backgroundTaskService;
    private readonly AnalysisProgressService _progressService;

    public AnalysisOrchestrationService(
        ILogger<AnalysisOrchestrationService> logger,
        IValidationService validationService,
        IContentExtractionService contentExtractionService,
        IDocumentRetrievalService documentRetrievalService,
        IAIAnalysisOrchestrator aiAnalysisOrchestrator,
        IResultProcessorService resultProcessorService,
        AnalysisCacheService cacheService,
        BackgroundTaskService backgroundTaskService,
        AnalysisProgressService progressService)
    {
        _logger = logger;
        _validationService = validationService;
        _contentExtractionService = contentExtractionService;
        _documentRetrievalService = documentRetrievalService;
        _aiAnalysisOrchestrator = aiAnalysisOrchestrator;
        _resultProcessorService = resultProcessorService;
        _cacheService = cacheService;
        _backgroundTaskService = backgroundTaskService;
        _progressService = progressService;
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

            _cacheService.StoreAnalysisResult(analysisId, analysisResult);
            _logger.LogInformation($"[Analysis {analysisId}] Initial analysis result stored in cache");

            // Get configuration values
            var apiKey = configuration["OpenRouter:ApiKey"] ?? "";
            var model = request.Model ?? configuration["OpenRouter:Model"] ?? "";
            var fallbackModel = configuration["OpenRouter:FallbackModel"] ?? "";

            // Start background analysis
            _backgroundTaskService.ExecuteBackgroundTask(
                "Analysis Execution",
                analysisId,
                () => RunBackgroundAnalysisAsync(
                    analysisId,
                    repositoryPath,
                    selectedDocuments,
                    documentsFolder,
                    apiKey,
                    model,
                    fallbackModel,
                    language,
                    analysisType,
                    commitId,
                    filePath,
                    session),
                async (ex) => await _progressService.BroadcastErrorAsync(analysisId, $"Background analysis error: {ex.Message}")
            );

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

        var result = _cacheService.GetAnalysisResult(analysisId);
        if (result != null)
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
        ISession session)
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
            var result = _cacheService.GetAnalysisResult(analysisId);
            if (result == null)
            {
                _logger.LogError($"[Analysis {analysisId}] Analysis result not found in cache - aborting");
                return;
            }

            // Update status to content extraction
            _cacheService.UpdateAnalysisStatus(analysisId, "Reading git changes...");
            await _progressService.BroadcastProgressAsync(analysisId, "Reading git changes...", model, fallbackModel);

            // Extract content
            var extractionResult = await _contentExtractionService.ExtractContentAsync(repositoryPath, analysisType, commitId, filePath);
            var (content, contentError, isFileContent, contentErrorMsg) = extractionResult;

            if (contentError)
            {
                _logger.LogError($"[Analysis {analysisId}] Content extraction failed: {contentErrorMsg}");
                await _resultProcessorService.ProcessAndBroadcastAsync(analysisId, "", contentErrorMsg, true, model, fallbackModel, session);
                return;
            }

            // Cache the content
            _cacheService.StoreContent(analysisId, content);

            _logger.LogInformation($"[Analysis {analysisId}] Content extraction complete, length: {content.Length}");

            // Update status to document loading
            _cacheService.UpdateAnalysisStatus(analysisId, "Loading documents...");
            await _progressService.BroadcastProgressAsync(analysisId, "Loading documents...", model, fallbackModel);

            // Load documents
            var codingStandards = await _documentRetrievalService.LoadDocumentsAsync(selectedDocuments, documentsFolder);
            _logger.LogInformation($"[Analysis {analysisId}] Loaded {codingStandards.Count} documents");

            // Update status to AI analysis
            _cacheService.UpdateAnalysisStatus(analysisId, $"AI analysis... (Using: {model})");
            await _progressService.BroadcastProgressAsync(analysisId, $"AI analysis... (Using: {model})", model, fallbackModel);

            // Get requirements
            var requirements = "Follow .NET best practices and coding standards";
            _logger.LogInformation($"[Analysis {analysisId}] Requirements: {requirements}");

            // Perform AI analysis
            var aiResult = await _aiAnalysisOrchestrator.AnalyzeAsync(content, codingStandards, requirements, apiKey, model, fallbackModel, language, isFileContent);
            var (aiAnalysis, aiError, aiErrorMsg, usedModel) = aiResult;

            // Process results
            await _resultProcessorService.ProcessAndBroadcastAsync(analysisId, aiAnalysis, aiErrorMsg, aiError, usedModel, fallbackModel, session);

            _logger.LogInformation($"[Analysis {analysisId}] Background analysis task completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Analysis {analysisId}] Unhandled exception in background analysis");
            try
            {
                await _progressService.BroadcastErrorAsync(analysisId, $"Analysis error: {ex.Message}");
                _logger.LogInformation($"[Analysis {analysisId}] Set error status due to exception: {ex.Message}");
            }
            catch (Exception broadcastEx)
            {
                _logger.LogError(broadcastEx, $"[Analysis {analysisId}] Failed to broadcast error status");
            }
        }
    }
}
