using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Interface for preparing analysis data through validation and content extraction
/// </summary>
public interface IAnalysisPreparationService
{
    /// <summary>
    /// Validates the analysis request and extracts content for analysis
    /// </summary>
    /// <param name="request">The analysis request</param>
    /// <param name="session">The user session</param>
    /// <param name="environment">The hosting environment</param>
    /// <returns>A tuple containing validation result, extracted content, and related metadata</returns>
    Task<(bool isValid, string? error, string? resolvedFilePath, string? content, bool isFileContent, string? contentError)> 
        PrepareAnalysisAsync(RunAnalysisRequest request, ISession session, IWebHostEnvironment environment);
}