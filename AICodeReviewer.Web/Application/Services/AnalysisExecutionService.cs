using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Application.Services;

/// <summary>
/// Service responsible for executing AI analysis and processing results
/// </summary>
public class AnalysisExecutionService : IAnalysisExecutionService
{
    private readonly ILogger<AnalysisExecutionService> _logger;
    private readonly IDocumentRetrievalService _documentRetrievalService;
    private readonly IAIAnalysisOrchestrator _aiAnalysisOrchestrator;
    private readonly IResultProcessorService _resultProcessorService;

    public AnalysisExecutionService(
        ILogger<AnalysisExecutionService> logger,
        IDocumentRetrievalService documentRetrievalService,
        IAIAnalysisOrchestrator aiAnalysisOrchestrator,
        IResultProcessorService resultProcessorService)
    {
        _logger = logger;
        _documentRetrievalService = documentRetrievalService;
        _aiAnalysisOrchestrator = aiAnalysisOrchestrator;
        _resultProcessorService = resultProcessorService;
    }

    /// <summary>
    /// Executes AI analysis using the provided request context
    /// </summary>
    /// <param name="request">The analysis execution request containing all necessary parameters</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ExecuteAnalysisAsync(AnalysisExecutionRequest request)
    {
        _logger.LogInformation("[Analysis {AnalysisId}] Starting AI analysis execution", request.AnalysisId);

        try
        {
            // Load documents
            _logger.LogInformation("[Analysis {AnalysisId}] Loading documents...", request.AnalysisId);
            var codingStandards = await _documentRetrievalService.LoadDocumentsAsync(request.SelectedDocuments, request.DocumentsFolder);
            _logger.LogInformation("[Analysis {AnalysisId}] Loaded {Count} documents", request.AnalysisId, codingStandards.Count);

            // Get requirements
            var requirements = "Follow .NET best practices and coding standards";
            _logger.LogInformation("[Analysis {AnalysisId}] Requirements: {Requirements}", request.AnalysisId, requirements);

            // Perform AI analysis
            _logger.LogInformation("[Analysis {AnalysisId}] Performing AI analysis with model: {Model}", request.AnalysisId, request.PrimaryModel);
            var aiResult = await _aiAnalysisOrchestrator.AnalyzeAsync(
                request.Content,
                codingStandards,
                requirements,
                request.ApiKey,
                request.PrimaryModel,
                request.FallbackModel,
                request.Language,
                request.IsFileContent);

            string aiAnalysis = aiResult.analysis;
            bool aiError = aiResult.error;
            string? aiErrorMsg = aiResult.errorMsg;
            string usedModel = aiResult.usedModel;

            _logger.LogInformation("[Analysis {AnalysisId}] AI analysis completed using model: {UsedModel}, Error: {HasError}",
                request.AnalysisId, usedModel, aiError);

            // Process results
            await _resultProcessorService.ProcessAndBroadcastAsync(
                request.AnalysisId,
                aiAnalysis,
                aiErrorMsg,
                aiError,
                usedModel,
                request.FallbackModel ?? string.Empty,
                request.Session);

            _logger.LogInformation("[Analysis {AnalysisId}] Analysis execution completed", request.AnalysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Analysis {AnalysisId}] Error during AI analysis execution", request.AnalysisId);
            throw;
        }
    }
}