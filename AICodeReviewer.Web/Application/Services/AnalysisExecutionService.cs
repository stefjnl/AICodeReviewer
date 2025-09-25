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
    /// Executes AI analysis with the provided content and documents
    /// </summary>
    /// <param name="analysisId">The unique analysis identifier</param>
    /// <param name="content">The code content to analyze</param>
    /// <param name="selectedDocuments">List of selected coding standards documents</param>
    /// <param name="documentsFolder">Path to the documents folder</param>
    /// <param name="apiKey">API key for AI service</param>
    /// <param name="primaryModel">Primary AI model to use</param>
    /// <param name="fallbackModel">Fallback AI model</param>
    /// <param name="language">Programming language</param>
    /// <param name="isFileContent">Whether the content is from a single file</param>
    /// <param name="session">User session</param>
    /// <returns>Task representing the async operation</returns>
    public async Task ExecuteAnalysisAsync(
        string analysisId,
        string content,
        List<string> selectedDocuments,
        string documentsFolder,
        string apiKey,
        string primaryModel,
        string? fallbackModel,
        string language,
        bool isFileContent,
        ISession session)
    {
        _logger.LogInformation("[Analysis {AnalysisId}] Starting AI analysis execution", analysisId);

        try
        {
            // Load documents
            _logger.LogInformation("[Analysis {AnalysisId}] Loading documents...", analysisId);
            var codingStandards = await _documentRetrievalService.LoadDocumentsAsync(selectedDocuments, documentsFolder);
            _logger.LogInformation("[Analysis {AnalysisId}] Loaded {Count} documents", analysisId, codingStandards.Count);

            // Get requirements
            var requirements = "Follow .NET best practices and coding standards";
            _logger.LogInformation("[Analysis {AnalysisId}] Requirements: {Requirements}", analysisId, requirements);

            // Perform AI analysis
            _logger.LogInformation("[Analysis {AnalysisId}] Performing AI analysis with model: {Model}", analysisId, primaryModel);
            var aiResult = await _aiAnalysisOrchestrator.AnalyzeAsync(
                content, 
                codingStandards, 
                requirements, 
                apiKey, 
                primaryModel, 
                fallbackModel, 
                language, 
                isFileContent);

            string aiAnalysis = aiResult.analysis;
            bool aiError = aiResult.error;
            string? aiErrorMsg = aiResult.errorMsg;
            string usedModel = aiResult.usedModel;

            _logger.LogInformation("[Analysis {AnalysisId}] AI analysis completed using model: {UsedModel}, Error: {HasError}", 
                analysisId, usedModel, aiError);

            // Process results
            await _resultProcessorService.ProcessAndBroadcastAsync(
                analysisId, 
                aiAnalysis, 
                aiErrorMsg, 
                aiError, 
                usedModel, 
                fallbackModel ?? string.Empty, 
                session);

            _logger.LogInformation("[Analysis {AnalysisId}] Analysis execution completed", analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Analysis {AnalysisId}] Error during AI analysis execution", analysisId);
            throw;
        }
    }
}