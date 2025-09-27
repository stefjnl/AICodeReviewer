using AICodeReviewer.Web.Application.Factories;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Extensions;
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
    private readonly IAnalysisCacheService _cacheService;
    private readonly IBackgroundTaskService _backgroundTaskService;
    private readonly IAnalysisProgressService _progressService;
    private readonly IAnalysisContextFactory _analysisContextFactory;

    public AnalysisOrchestrationService(
        ILogger<AnalysisOrchestrationService> logger,
        IAnalysisPreparationService preparationService,
        IAnalysisExecutionService executionService,
        IAnalysisCacheService cacheService,
        IBackgroundTaskService backgroundTaskService,
        IAnalysisProgressService progressService,
        IAnalysisContextFactory analysisContextFactory)
    {
        _logger = logger;
        _preparationService = preparationService;
        _executionService = executionService;
        _cacheService = cacheService;
        _backgroundTaskService = backgroundTaskService;
        _progressService = progressService;
        _analysisContextFactory = analysisContextFactory;
    }

    public async Task<(string analysisId, bool success, string? error)> StartAnalysisAsync(
        RunAnalysisRequest request,
        ISession session,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        try
        {
            var context = _analysisContextFactory.Create(request, session, environment, configuration);
            session.SetString(SessionKeys.Language, context.Language);

            var analysisId = Guid.NewGuid().ToString();

            var preparationResult = await _preparationService.PrepareAnalysisAsync(request, session, environment);
            if (!preparationResult.isValid)
            {
                _logger.LogWarning("[RunAnalysis] Preparation failed: {Error}", preparationResult.error);
                return ("", false, preparationResult.error);
            }

            if (!string.IsNullOrEmpty(preparationResult.content))
            {
                _cacheService.StoreContent(analysisId, preparationResult.content);
                _cacheService.StoreIsFileContent(analysisId, preparationResult.isFileContent);
                _logger.LogInformation("[Analysis {AnalysisId}] Content and isFileContent flag cached after preparation, length: {Length}, isFileContent: {IsFileContent}",
                    analysisId, preparationResult.content.Length, preparationResult.isFileContent);
            }

            var analysisResult = new AnalysisResult { Status = "Starting", CreatedAt = DateTime.UtcNow };
            _cacheService.StoreAnalysisResult(analysisId, analysisResult);
            _logger.LogInformation($"[Analysis {analysisId}] Initial analysis result stored in cache");

            _backgroundTaskService.ExecuteBackgroundTask(
                "Analysis Execution",
                analysisId,
                async () => await RunBackgroundAnalysisAsync(analysisId, context.SelectedDocuments, context.DocumentsFolder, context.ApiKey, context.Model, context.FallbackModel, context.Language, session),
                async (ex) => await _progressService.BroadcastErrorAsync(analysisId, $"Background analysis error: {ex.Message}")
            );

            _logger.LogInformation("Started analysis {AnalysisId} of type {AnalysisType}", analysisId, context.AnalysisType);
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

        session.SetString(SessionKeys.AnalysisId, analysisId);
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
            await _progressService.BroadcastProgressAsync(analysisId, "Loading documents...", model, fallbackModel ?? string.Empty);

            // Execute AI analysis using the execution service
            _cacheService.UpdateAnalysisStatus(analysisId, $"AI analysis... (Using: {model})");
            await _progressService.BroadcastProgressAsync(analysisId, $"AI analysis... (Using: {model})", model, fallbackModel ?? string.Empty);

            var executionRequest = new AnalysisExecutionRequest(
                analysisId,
                content,
                selectedDocuments,
                documentsFolder,
                apiKey,
                model,
                fallbackModel,
                language,
                isFileContent,
                session
            );
            await _executionService.ExecuteAnalysisAsync(executionRequest);

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
