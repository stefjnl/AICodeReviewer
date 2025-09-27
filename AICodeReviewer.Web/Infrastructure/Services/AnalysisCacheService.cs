using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for managing analysis-related cache operations
/// </summary>
public class AnalysisCacheService : IAnalysisCacheService
{
    private readonly ILogger<AnalysisCacheService> _logger;
    private readonly IMemoryCache _cache;

    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
        .SetSize(1);

    public AnalysisCacheService(
        ILogger<AnalysisCacheService> logger,
        IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Store analysis result in cache
    /// </summary>
    public void StoreAnalysisResult(string analysisId, AnalysisResult result)
    {
        _cache.Set($"analysis_{analysisId}", result, CacheEntryOptions);
        _logger.LogInformation($"[Analysis {analysisId}] Analysis result stored in cache");
    }

    /// <summary>
    /// Retrieve analysis result from cache
    /// </summary>
    public AnalysisResult? GetAnalysisResult(string analysisId)
    {
        if (_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result))
        {
            _logger.LogInformation($"[Analysis {analysisId}] Retrieved analysis result from cache");
            return result;
        }

        _logger.LogWarning($"[Analysis {analysisId}] Analysis result not found in cache");
        return null;
    }

    /// <summary>
    /// Store content in cache
    /// </summary>
    public void StoreContent(string analysisId, string content)
    {
        _cache.Set($"content_{analysisId}", content, CacheEntryOptions);
        _logger.LogInformation($"[Analysis {analysisId}] Content stored in cache with length: {content.Length}");
    }

    /// <summary>
    /// Store isFileContent flag in cache
    /// </summary>
    public void StoreIsFileContent(string analysisId, bool isFileContent)
    {
        _cache.Set($"isFileContent_{analysisId}", isFileContent, CacheEntryOptions);
        _logger.LogInformation($"[Analysis {analysisId}] IsFileContent flag stored in cache: {isFileContent}");
    }

    /// <summary>
    /// Retrieve content from cache
    /// </summary>
    public string? GetContent(string analysisId)
    {
        if (_cache.TryGetValue($"content_{analysisId}", out string? content))
        {
            _logger.LogInformation($"[Analysis {analysisId}] Retrieved content from cache");
            return content;
        }

        _logger.LogWarning($"[Analysis {analysisId}] Content not found in cache");
        return null;
    }

    /// <summary>
    /// Retrieve isFileContent flag from cache
    /// </summary>
    public bool? GetIsFileContent(string analysisId)
    {
        if (_cache.TryGetValue($"isFileContent_{analysisId}", out bool isFileContent))
        {
            _logger.LogInformation($"[Analysis {analysisId}] Retrieved isFileContent flag from cache: {isFileContent}");
            return isFileContent;
        }

        _logger.LogWarning($"[Analysis {analysisId}] IsFileContent flag not found in cache");
        return null;
    }

    /// <summary>
    /// Update analysis status in cache
    /// </summary>
    public void UpdateAnalysisStatus(string analysisId, string status)
    {
        var result = GetAnalysisResult(analysisId);
        if (result != null)
        {
            result.Status = status;
            StoreAnalysisResult(analysisId, result);
            _logger.LogInformation($"[Analysis {analysisId}] Updated status to '{status}'");
        }
    }
}
