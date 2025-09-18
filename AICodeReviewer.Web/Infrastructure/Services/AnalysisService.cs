using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Extensions;
using AICodeReviewer.Web.Models;
using AICodeReviewer.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for orchestrating code analysis operations
/// </summary>
public class AnalysisService : IAnalysisService
{
    private readonly ILogger<AnalysisService> _logger;
    private readonly IRepositoryManagementService _repositoryService;
    private readonly IDocumentManagementService _documentService;
    private readonly IPathValidationService _pathService;
    private readonly ISignalRBroadcastService _signalRService;
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly IMemoryCache _cache;
    private readonly IAIService _aiService;

    public AnalysisService(
        ILogger<AnalysisService> logger,
        IRepositoryManagementService repositoryService,
        IDocumentManagementService documentService,
        IPathValidationService pathService,
        ISignalRBroadcastService signalRService,
        IHubContext<ProgressHub> hubContext,
        IMemoryCache cache,
        IAIService aiService)
    {
        _logger = logger;
        _repositoryService = repositoryService;
        _documentService = documentService;
        _pathService = pathService;
        _signalRService = signalRService;
        _hubContext = hubContext;
        _cache = cache;
        _aiService = aiService;
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

            // Validate required fields
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API key not configured");
                return ("", false, "API key not configured");
            }

            if (selectedDocuments.Count == 0)
            {
                _logger.LogError("No coding standards selected");
                return ("", false, "No coding standards selected");
            }

            // Validate based on analysis type
            if (analysisType == AnalysisType.SingleFile)
            {
                _logger.LogInformation("[RunAnalysis] Single file analysis requested with filePath: {FilePath}", filePath);
                
                // Validate file path for single file analysis
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("[RunAnalysis] File path is missing for single file analysis");
                    return ("", false, "File path is required for single file analysis");
                }

                // Validate the file path
                var (resolvedPath, isValid, validationError) = _pathService.ValidateSingleFilePath(filePath, repositoryPath, environment.ContentRootPath);
                if (!isValid)
                {
                    return ("", false, validationError);
                }
                
                filePath = resolvedPath;
                _logger.LogInformation("[RunAnalysis] File validation passed for: {FilePath}", filePath);
            }
            else
            {
                // Validate git repository for git-based analysis
                var (isValid, repoError) = _repositoryService.ValidateRepositoryForAnalysis(repositoryPath);
                if (!isValid)
                {
                    return ("", false, repoError);
                }

                // Validate commit ID if commit analysis requested
                if (analysisType == AnalysisType.Commit)
                {
                    if (string.IsNullOrEmpty(commitId))
                    {
                        return ("", false, "Commit ID is required for commit analysis");
                    }
                }

                // Validate staged changes if staged analysis requested
                if (analysisType == AnalysisType.Staged)
                {
                    var (hasStaged, stagedError) = _repositoryService.HasStagedChanges(repositoryPath);
                    if (!hasStaged)
                    {
                        return ("", false, "No staged changes found. Use 'git add' to stage files for analysis.");
                    }
                }
            }

            // Generate unique analysis ID
            var analysisId = Guid.NewGuid().ToString();

            // Create initial analysis result
            var analysisResult = new AnalysisResult
            {
                Status = "Starting",
                CreatedAt = DateTime.UtcNow
            };

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

    /// <summary>
    /// Checks if an error message indicates a rate-limiting issue
    /// </summary>
    private static bool IsRateLimitError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return false;
            
        // Check for HTTP 429 status code or rate limit keywords
        return errorMessage.Contains("429") ||
               errorMessage.Contains("rate limit") ||
               errorMessage.Contains("rate-limit") ||
               errorMessage.Contains("Rate limit") ||
               errorMessage.Contains("too many requests");
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
        AnalysisType analysisType = AnalysisType.Uncommitted,
        string? commitId = null,
        string? filePath = null,
        ISession? session = null)
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
            AnalysisResult? result;
            try
            {
                // Note: This would need to be passed in or accessed differently in a real implementation
                // For now, we'll create a new result
                result = new AnalysisResult
                {
                    Status = "Starting",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Error accessing cache - aborting");
                return;
            }
            _logger.LogInformation($"[Analysis {analysisId}] Found existing result in cache");

            // Update status via SignalR
            if (result != null)
            {
                result.Status = "Reading git changes...";
                await _signalRService.BroadcastProgressWithModelAsync(analysisId, "Reading git changes...", model, fallbackModel);
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Reading git changes...'");
            }

            // Get content based on analysis type
            _logger.LogInformation($"[Analysis {analysisId}] Analysis type: {analysisType}");
            string content;
            bool contentError;
            bool isFileContent = false;
            
            if (analysisType == AnalysisType.SingleFile && !string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation($"[Analysis {analysisId}] Reading single file: {filePath}");
                try
                {
                    content = await File.ReadAllTextAsync(filePath);
                    contentError = false;
                    isFileContent = true;
                    _logger.LogInformation($"[Analysis {analysisId}] File read successfully, length: {content.Length}");
                }
                catch (Exception ex)
                {
                    content = $"Error reading file: {ex.Message}";
                    contentError = true;
                    _logger.LogError(ex, $"[Analysis {analysisId}] Failed to read file: {filePath}");
                }
            }
            else if (analysisType == AnalysisType.Commit && !string.IsNullOrEmpty(commitId))
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting commit diff for commit: {commitId}");
                (content, contentError) = _repositoryService.GetCommitDiff(repositoryPath, commitId);
            }
            else if (analysisType == AnalysisType.Staged)
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting staged changes only");
                (content, contentError) = _repositoryService.ExtractStagedDiff(repositoryPath);
            }
            else
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting uncommitted changes");
                (content, contentError) = _repositoryService.ExtractDiff(repositoryPath);
            }
            
            _logger.LogInformation($"[Analysis {analysisId}] Content extraction complete - Error: {contentError}, Content length: {content?.Length ?? 0}");

            if (contentError)
            {
                _logger.LogError($"[Analysis {analysisId}] Content extraction failed: {content}");
                if (result != null)
                {
                    result.Status = "Error";
                    result.Error = isFileContent ? $"File reading error: {content}" : $"Git diff error: {content}";
                    result.CompletedAt = DateTime.UtcNow;
                    
                    await _signalRService.BroadcastErrorAsync(analysisId, result.Error);
                    _logger.LogInformation($"[Analysis {analysisId}] Set error status and completed");
                }
                return;
            }
            _logger.LogInformation($"[Analysis {analysisId}] Content extracted successfully");

            // Update status via SignalR
            _logger.LogInformation($"[Analysis {analysisId}] Starting document loading phase");
            if (result != null)
            {
                result.Status = "Loading documents...";
                await _signalRService.BroadcastProgressWithModelAsync(analysisId, "Loading documents...", model, fallbackModel);
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Loading documents...'");
            }
            
            // Load selected documents asynchronously
            _logger.LogInformation($"[Analysis {analysisId}] Loading {selectedDocuments.Count} selected documents");
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder path: {documentsFolder}");
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder exists: {Directory.Exists(documentsFolder)}");
            
            // Create tasks for parallel document loading
            var documentTasks = selectedDocuments.Select(async docName =>
            {
                _logger.LogInformation($"[Analysis {analysisId}] Loading document: {docName} from folder: {documentsFolder}");
                var (docContent, docError) = await _documentService.LoadDocumentAsync(docName, documentsFolder);
                _logger.LogInformation($"[Analysis {analysisId}] Document {docName} - Error: {docError}, Content length: {docContent?.Length ?? 0}");
                
                if (!docError)
                {
                    _logger.LogInformation($"[Analysis {analysisId}] Document {docName} loaded successfully");
                    return (docContent, docError, docName);
                }
                else
                {
                    _logger.LogWarning($"[Analysis {analysisId}] Failed to load document {docName}: {docContent}");
                    return (docContent, docError, docName);
                }
            }).ToList();
            
            // Wait for all documents to load in parallel
            var documentResults = await Task.WhenAll(documentTasks);
            
            // Collect successful documents
            var codingStandards = new List<string>();
            foreach (var (docContent, docError, docName) in documentResults)
            {
                if (!docError)
                {
                    codingStandards.Add(docContent);
                }
            }
            
            _logger.LogInformation($"[Analysis {analysisId}] Document loading complete - loaded {codingStandards.Count} documents");
            _logger.LogInformation($"[Analysis {analysisId}] Final codingStandards count: {codingStandards.Count}");

            // Update status via SignalR
            _logger.LogInformation($"[Analysis {analysisId}] Starting AI analysis phase");
            if (result != null)
            {
                result.Status = "AI analysis...";
                await _signalRService.BroadcastProgressWithModelAsync(analysisId, $"AI analysis... (Using: {model})", model, fallbackModel);
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'AI analysis...' with model {model}");
            }
            
            // Get requirements
            string requirements = "Follow .NET best practices and coding standards";
            _logger.LogInformation($"[Analysis {analysisId}] Requirements: {requirements}");

            // Call AI service with timeout protection
            _logger.LogInformation($"[Analysis {analysisId}] Calling AI service with:");
            _logger.LogInformation($"[Analysis {analysisId}] - Content length: {content?.Length ?? 0}");
            _logger.LogInformation($"[Analysis {analysisId}] - Coding standards count: {codingStandards.Count}");
            _logger.LogInformation($"[Analysis {analysisId}] - API key configured: {!string.IsNullOrEmpty(apiKey)}");
            _logger.LogInformation($"[Analysis {analysisId}] - Primary model: {model}");
            _logger.LogInformation($"[Analysis {analysisId}] - Fallback model: {fallbackModel ?? "None"}");
            _logger.LogInformation($"[Analysis {analysisId}] - Analysis type: {(isFileContent ? "Single File" : "Git Diff")}");
            
            // Add timeout protection (60 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            
            string analysis;
            bool aiError;
            string? errorMessage;
            bool fallbackWasUsed = false; // Track if fallback model was used
            
            try
            {
                _logger.LogInformation($"[Analysis {analysisId}] Starting AI service call with 60-second timeout");
                
                // Call AI service with timeout
                var (analysisResult, isError, errorMsg) = await Task.Run(async () =>
                    await _aiService.AnalyzeCodeAsync(content ?? "", codingStandards, requirements, apiKey, model, language, isFileContent),
                    cts.Token);
                
                analysis = analysisResult;
                aiError = isError;
                errorMessage = errorMsg;
                
                _logger.LogInformation($"[Analysis {analysisId}] AI service call complete - Error: {aiError}, Analysis length: {analysis?.Length ?? 0}");
                if (aiError)
                {
                    _logger.LogError($"[Analysis {analysisId}] AI analysis failed with detailed error: {errorMessage}");
                    
                    // Check if this is a rate-limit error and we have a fallback model
                    if (IsRateLimitError(errorMessage) && !string.IsNullOrEmpty(fallbackModel))
                    {
                        fallbackWasUsed = true; // Mark that fallback was used
                        _logger.LogInformation($"[Analysis {analysisId}] Rate limit detected for primary model {model}, falling back to {fallbackModel}");
                        await _signalRService.BroadcastProgressWithModelAsync(analysisId, $"Rate limited, switching to fallback model ({fallbackModel})...", model, fallbackModel);
                        
                        // Retry with fallback model
                        (analysisResult, isError, errorMsg) = await Task.Run(async () =>
                            await _aiService.AnalyzeCodeAsync(content ?? "", codingStandards, requirements, apiKey, fallbackModel, language, isFileContent),
                            cts.Token);
                        
                        analysis = analysisResult;
                        aiError = isError;
                        errorMessage = errorMsg;
                        
                        _logger.LogInformation($"[Analysis {analysisId}] Fallback model call complete - Error: {aiError}, Analysis length: {analysis?.Length ?? 0}");
                        if (aiError)
                        {
                            _logger.LogError($"[Analysis {analysisId}] Fallback model analysis failed with detailed error: {errorMessage}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError($"[Analysis {analysisId}] AI analysis timed out after 60 seconds");
                analysis = "";
                aiError = true;
                errorMessage = "AI analysis timed out after 60 seconds";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Unexpected exception during AI service call");
                analysis = "";
                aiError = true;
                errorMessage = $"Unexpected error calling AI service: {ex.Message}";
            }

            // Broadcast completion
            if (aiError)
            {
                await _signalRService.BroadcastErrorAsync(analysisId, $"AI analysis failed: {errorMessage}");
            }
            else
            {
                // Determine which model was actually used and show it in the completion message
                string usedModel = fallbackWasUsed ? fallbackModel : model;
                await _signalRService.BroadcastCompleteWithModelAsync(analysisId, analysis, session!, usedModel, fallbackModel);
                
                _logger.LogInformation($"[Analysis {analysisId}] Analysis completed successfully using model: {usedModel}");
            }

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
        finally
        {
            _logger.LogInformation($"[Analysis {analysisId}] Background analysis task completed");
        }
    }
}