using Microsoft.AspNetCore.SignalR;
using AICodeReviewer.Web.Hubs;
using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AICodeReviewer.Web.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(IHubContext<ProgressHub> hubContext, IMemoryCache cache, ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task BroadcastProgress(string analysisId, string status)
    {
        try
        {
            var progressDto = new ProgressDto(status, null, null, false);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Keep cache for fallback - create AnalysisResult from ProgressDto
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow
            };
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation($"[SignalR] Broadcasted progress for analysis {analysisId}: {status}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastComplete(string analysisId, string result)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis complete", result, null, true);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Cache as AnalysisResult - create AnalysisResult from ProgressDto
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogInformation($"[SignalR] Broadcasted completion for analysis {analysisId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
    }

    public async Task BroadcastError(string analysisId, string error)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis failed", null, error, true);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Cache as AnalysisResult - create AnalysisResult from ProgressDto
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
            
            _logger.LogError($"[SignalR] Broadcasted error for analysis {analysisId}: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
    }
}