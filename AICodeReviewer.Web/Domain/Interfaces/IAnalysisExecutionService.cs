using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Interface for executing AI analysis and processing results
/// </summary>
public interface IAnalysisExecutionService
{
    /// <summary>
    /// Executes AI analysis using the provided request context
    /// </summary>
    /// <param name="request">The analysis execution request containing all necessary parameters</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteAnalysisAsync(AnalysisExecutionRequest request);
}