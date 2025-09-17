using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for broadcasting real-time updates via SignalR
/// </summary>
public interface ISignalRBroadcastService
{
    /// <summary>
    /// Broadcast progress update for an analysis
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="status">Current status message</param>
    Task BroadcastProgressAsync(string analysisId, string status);

    /// <summary>
    /// Broadcast progress update with model information for an analysis
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="status">Current status message</param>
    /// <param name="modelUsed">Primary model being used</param>
    /// <param name="fallbackModel">Fallback model available</param>
    Task BroadcastProgressWithModelAsync(string analysisId, string status, string? modelUsed = null, string? fallbackModel = null);

    /// <summary>
    /// Broadcast analysis completion with results
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="result">Analysis results</param>
    /// <param name="session">Http session for storing analysis ID</param>
    Task BroadcastCompleteAsync(string analysisId, string? result, ISession session);

    /// <summary>
    /// Broadcast analysis completion with results and model information
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="result">Analysis results</param>
    /// <param name="session">Http session for storing analysis ID</param>
    /// <param name="modelUsed">Primary model used</param>
    /// <param name="fallbackModel">Fallback model available</param>
    Task BroadcastCompleteWithModelAsync(string analysisId, string? result, ISession session, string? modelUsed = null, string? fallbackModel = null);

    /// <summary>
    /// Broadcast analysis error
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="error">Error message</param>
    Task BroadcastErrorAsync(string analysisId, string error);

    /// <summary>
    /// Store progress in cache for fallback access
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="status">Current status</param>
    void StoreProgressInCache(string analysisId, string status);
}