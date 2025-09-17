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
    private readonly IMemoryCache _cache;

    public SignalRBroadcastService(IHubContext<ProgressHub> hubContext, ILogger<SignalRBroadcastService> logger, IMemoryCache cache)
    {
        _hubContext = hubContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task BroadcastProgressAsync(string analysisId, string status)
    {
        try
        {
            var progressDto = new ProgressDto(status, null, null, false);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Store in cache for fallback
            StoreProgressInCache(analysisId, status);
            
            _logger.LogInformation("Broadcasted progress for analysis {AnalysisId}: {Status}", analysisId, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastProgressWithModelAsync(string analysisId, string status, string? modelUsed = null, string? fallbackModel = null)
    {
        try
        {
            var progressDto = new ProgressDto(status, null, null, false, modelUsed, fallbackModel);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Store in cache for fallback
            StoreProgressInCache(analysisId, status);
            
            _logger.LogInformation("Broadcasted progress for analysis {AnalysisId}: {Status}, Model: {ModelUsed}, Fallback: {FallbackModel}",
                analysisId, status, modelUsed ?? "None", fallbackModel ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast with model failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastCompleteAsync(string analysisId, string? result, ISession session)
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
            
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation("Broadcasted completion for analysis {AnalysisId}", analysisId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast completion failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastCompleteWithModelAsync(string analysisId, string? result, ISession session, string? modelUsed = null, string? fallbackModel = null)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis complete", result, null, true, modelUsed, fallbackModel);
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
            
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation("Broadcasted completion for analysis {AnalysisId} with model {ModelUsed}", analysisId, modelUsed ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast completion with model failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastErrorAsync(string analysisId, string error)
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
            
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation("Broadcasted error for analysis {AnalysisId}: {Error}", analysisId, error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast error failed for analysis {AnalysisId}", analysisId);
        }
    }

    public void StoreProgressInCache(string analysisId, string status)
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
            
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            _logger.LogDebug("Stored progress in cache for analysis {AnalysisId}: {Status}", analysisId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store progress in cache for analysis {AnalysisId}", analysisId);
        }
    }
}