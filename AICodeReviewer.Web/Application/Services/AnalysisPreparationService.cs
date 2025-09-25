using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using AICodeReviewer.Web.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AICodeReviewer.Web.Application.Services;

/// <summary>
/// Service responsible for preparing analysis data through validation and content extraction
/// </summary>
public class AnalysisPreparationService : IAnalysisPreparationService
{
    private readonly ILogger<AnalysisPreparationService> _logger;
    private readonly IValidationService _validationService;
    private readonly IContentExtractionService _contentExtractionService;
    private readonly AnalysisProgressService _progressService;

    public AnalysisPreparationService(
        ILogger<AnalysisPreparationService> logger,
        IValidationService validationService,
        IContentExtractionService contentExtractionService,
        AnalysisProgressService progressService)
    {
        _logger = logger;
        _validationService = validationService;
        _contentExtractionService = contentExtractionService;
        _progressService = progressService;
    }

    /// <summary>
    /// Validates the analysis request and extracts content for analysis
    /// </summary>
    /// <param name="request">The analysis request</param>
    /// <param name="session">The user session</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>A tuple containing validation result, extracted content, and related metadata</returns>
    public async Task<(bool isValid, string? error, string? resolvedFilePath, string? content, bool isFileContent, string? contentError)>
        PrepareAnalysisAsync(RunAnalysisRequest request, ISession session, IWebHostEnvironment environment)
    {
        _logger.LogInformation("Starting analysis preparation");

        // Validate the request using validation service
        var (isValid, validationError, resolvedFilePath) = await _validationService.ValidateAnalysisRequestAsync(request, session, environment);
        if (!isValid)
        {
            _logger.LogWarning("[AnalysisPreparation] Validation failed: {Error}", validationError);
            return (false, validationError, null, null, false, null);
        }

        // Extract content
        var repositoryPath = request.RepositoryPath ?? session.GetString("RepositoryPath") ?? Path.Combine(environment.ContentRootPath, "..");
        
        // Broadcast progress update for content extraction
        var model = request.Model ?? "";
        var fallbackModel = "";
        await _progressService.BroadcastProgressAsync("preparation", "Reading git changes...", model, fallbackModel);
        
        var extractionResult = await _contentExtractionService.ExtractContentAsync(
            repositoryPath,
            request.AnalysisType ?? AnalysisType.Uncommitted,
            request.CommitId,
            resolvedFilePath ?? request.FilePath,
            request.FileContent);

        if (extractionResult.contentError)
        {
            _logger.LogError("[AnalysisPreparation] Content extraction failed: {Error}", extractionResult.error);
            return (false, extractionResult.error, resolvedFilePath, null, false, extractionResult.error);
        }

        _logger.LogInformation("Analysis preparation completed successfully, content length: {Length}", extractionResult.content.Length);
        return (true, null, resolvedFilePath, extractionResult.content, extractionResult.isFileContent, null);
    }
}