using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Hubs;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for broadcasting real-time updates via SignalR
/// </summary>
public class SignalRBroadcastService : ISignalRBroadcastService
{
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly ILogger<SignalRBroadcastService> _logger;

    public SignalRBroadcastService(IHubContext<ProgressHub> hubContext, ILogger<SignalRBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastProgressAsync(string analysisId, string status, IMemoryCache cache)
    {
        try
        {
            var progressDto = new ProgressDto(status, null, null, false);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Store in cache for fallback
            StoreProgressInCache(analysisId, status, cache);
            
            _logger.LogInformation("Broadcasted progress for analysis {AnalysisId}: {Status}", analysisId, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastCompleteAsync(string analysisId, string? result, ISession session, IMemoryCache cache)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis complete", result, null, true);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Store analysis ID in session for view switching
            session.SetString("AnalysisId", analysisId);
            
            // Store in cache for fallback
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            
            cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation("Broadcasted completion for analysis {AnalysisId}", analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast completion failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastErrorAsync(string analysisId, string error, IMemoryCache cache)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis failed", null, error, true);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Store in cache for fallback
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            
            cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation("Broadcasted error for analysis {AnalysisId}: {Error}", analysisId, error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast error failed for analysis {AnalysisId}", analysisId);
        }
    }

    public void StoreProgressInCache(string analysisId, string status, IMemoryCache cache)
    {
        try
        {
            // Create AnalysisResult from ProgressDto for cache storage
            var cacheResult = new AnalysisResult
            {
                Status = status,
                Result = null,
                Error = null,
                CreatedAt = DateTime.UtcNow
            };
            
            cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            _logger.LogDebug("Stored progress in cache for analysis {AnalysisId}: {Status}", analysisId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store progress in cache for analysis {AnalysisId}", analysisId);
        }
    }
}