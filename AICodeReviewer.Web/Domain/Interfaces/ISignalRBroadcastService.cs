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
    /// <param name="cache">Memory cache for storing progress</param>
    Task BroadcastProgressAsync(string analysisId, string status, IMemoryCache cache);

    /// <summary>
    /// Broadcast analysis completion with results
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="result">Analysis results</param>
    /// <param name="session">Http session for storing analysis ID</param>
    /// <param name="cache">Memory cache for storing results</param>
    Task BroadcastCompleteAsync(string analysisId, string? result, ISession session, IMemoryCache cache);

    /// <summary>
    /// Broadcast analysis error
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="error">Error message</param>
    /// <param name="cache">Memory cache for storing error</param>
    Task BroadcastErrorAsync(string analysisId, string error, IMemoryCache cache);

    /// <summary>
    /// Store progress in cache for fallback access
    /// </summary>
    /// <param name="analysisId">Analysis identifier</param>
    /// <param name="status">Current status</param>
    /// <param name="cache">Memory cache</param>
    void StoreProgressInCache(string analysisId, string status, IMemoryCache cache);
}