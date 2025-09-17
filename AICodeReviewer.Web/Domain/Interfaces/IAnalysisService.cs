using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for orchestrating code analysis operations
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Start a new analysis with the specified parameters
    /// </summary>
    /// <param name="request">Analysis request parameters</param>
    /// <param name="session">Http session for retrieving stored data</param>
    /// <param name="environment">Web host environment for path resolution</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Analysis ID and success status</returns>
    Task<(string analysisId, bool success, string? error)> StartAnalysisAsync(
        RunAnalysisRequest request,
        ISession session,
        IWebHostEnvironment environment,
        IConfiguration configuration);

    /// <summary>
    /// Get the current status of an analysis
    /// </summary>
    /// <param name="analysisId">Unique analysis identifier</param>
    /// <returns>Analysis status and results</returns>
    ProgressDto GetAnalysisStatus(string analysisId);

    /// <summary>
    /// Store analysis ID in session for tracking
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="session">Http session</param>
    void StoreAnalysisId(string analysisId, ISession session);
}