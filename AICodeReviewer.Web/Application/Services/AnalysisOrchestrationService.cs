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
    private readonly IAnalysisPreparationService _preparationService;
    private readonly IAnalysisExecutionService _executionService;
    private readonly AnalysisCacheService _cacheService;
    private readonly BackgroundTaskService _backgroundTaskService;
    private readonly AnalysisProgressService _progressService;

    public AnalysisOrchestrationService(
        ILogger<AnalysisOrchestrationService> logger,
        IAnalysisPreparationService preparationService,
        IAnalysisExecutionService executionService,
        AnalysisCacheService cacheService,
        BackgroundTaskService backgroundTaskService,
        AnalysisProgressService progressService)
    {
        _logger = logger;
        _preparationService = preparationService;
        _executionService = executionService;
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
            var fileContent = request.FileContent;

            // Store language in session for consistency
            session.SetString("Language", language);

            // Generate unique analysis ID
            var analysisId = Guid.NewGuid().ToString();

            // Prepare analysis (validation and content extraction)
            var preparationResult = await _preparationService.PrepareAnalysisAsync(request, session, environment);
            if (!preparationResult.isValid)
            {
                _logger.LogWarning("[RunAnalysis] Preparation failed: {Error}", preparationResult.error);
                return ("", false, preparationResult.error);
            }

            filePath = preparationResult.resolvedFilePath ?? filePath;

            // Cache the content and isFileContent flag after successful preparation
            if (!string.IsNullOrEmpty(preparationResult.content))
            {
                _cacheService.StoreContent(analysisId, preparationResult.content);
                _cacheService.StoreIsFileContent(analysisId, preparationResult.isFileContent);
                _logger.LogInformation("[Analysis {AnalysisId}] Content and isFileContent flag cached after preparation, length: {Length}, isFileContent: {IsFileContent}",
                    analysisId, preparationResult.content.Length, preparationResult.isFileContent);
            }

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
                    selectedDocuments,
                    documentsFolder,
                    apiKey,
                    model,
                    fallbackModel,
                    language,
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
        List<string> selectedDocuments,
        string documentsFolder,
        string apiKey,
        string model,
        string fallbackModel,
        string language,
        ISession session)
    {
        _logger.LogInformation($"[Analysis {analysisId}] Starting background analysis");
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

            // Get content from cache (stored during preparation phase)
            var content = _cacheService.GetContent(analysisId);
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogError($"[Analysis {analysisId}] Content not found in cache - aborting");
                await _progressService.BroadcastErrorAsync(analysisId, "Analysis content not found");
                return;
            }

            // Get isFileContent flag from cache
            var isFileContentFromCache = _cacheService.GetIsFileContent(analysisId);
            bool isFileContent = isFileContentFromCache ?? false;
            
            if (isFileContentFromCache == null)
            {
                _logger.LogWarning("[Analysis {AnalysisId}] IsFileContent flag not found in cache, defaulting to false", analysisId);
            }

            _logger.LogInformation($"[Analysis {analysisId}] Retrieved content from cache, length: {content.Length}");

            // Update status to document loading
            _cacheService.UpdateAnalysisStatus(analysisId, "Loading documents...");
            await _progressService.BroadcastProgressAsync(analysisId, "Loading documents...", model, fallbackModel);

            // Execute AI analysis using the execution service
            _cacheService.UpdateAnalysisStatus(analysisId, $"AI analysis... (Using: {model})");
            await _progressService.BroadcastProgressAsync(analysisId, $"AI analysis... (Using: {model})", model, fallbackModel);

            await _executionService.ExecuteAnalysisAsync(
                analysisId,
                content,
                selectedDocuments,
                documentsFolder,
                apiKey,
                model,
                fallbackModel,
                language,
                isFileContent,
                session);

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
