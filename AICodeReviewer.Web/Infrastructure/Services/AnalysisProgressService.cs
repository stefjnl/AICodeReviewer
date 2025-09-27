using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for managing analysis progress broadcasting via SignalR
/// </summary>
public class AnalysisProgressService : IAnalysisProgressService
{
    private readonly ILogger<AnalysisProgressService> _logger;
    private readonly ISignalRBroadcastService _signalRService;

    public AnalysisProgressService(
        ILogger<AnalysisProgressService> logger,
        ISignalRBroadcastService signalRService)
    {
        _logger = logger;
        _signalRService = signalRService;
    }

    /// <summary>
    /// Broadcast progress update with model information
    /// </summary>
    public async Task BroadcastProgressAsync(string analysisId, string status, string primaryModel, string fallbackModel)
    {
        try
        {
            await _signalRService.BroadcastProgressWithModelAsync(analysisId, status, primaryModel, fallbackModel);
            _logger.LogInformation($"[Analysis {analysisId}] Broadcasted progress: {status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Analysis {analysisId}] Failed to broadcast progress: {status}");
            throw;
        }
    }

    /// <summary>
    /// Broadcast progress update with custom message
    /// </summary>
    public async Task BroadcastProgressAsync(string analysisId, string message)
    {
        try
        {
            await _signalRService.BroadcastProgressAsync(analysisId, message);
            _logger.LogInformation($"[Analysis {analysisId}] Broadcasted progress: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Analysis {analysisId}] Failed to broadcast progress: {message}");
            throw;
        }
    }

    /// <summary>
    /// Broadcast error message
    /// </summary>
    public async Task BroadcastErrorAsync(string analysisId, string errorMessage)
    {
        try
        {
            await _signalRService.BroadcastErrorAsync(analysisId, errorMessage);
            _logger.LogInformation($"[Analysis {analysisId}] Broadcasted error: {errorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Analysis {analysisId}] Failed to broadcast error: {errorMessage}");
            throw;
        }
    }

    /// <summary>
    /// Broadcast analysis completion with results
    /// </summary>
    public async Task BroadcastCompletionAsync(string analysisId, string result, string? errorMessage, bool hasError, ISession session)
    {
        try
        {
            if (hasError)
            {
                await _signalRService.BroadcastErrorAsync(analysisId, errorMessage ?? "Unknown error");
            }
            else
            {
                await _signalRService.BroadcastCompleteAsync(analysisId, result, session);
            }
            _logger.LogInformation($"[Analysis {analysisId}] Broadcasted completion: {(hasError ? "Error" : "Success")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Analysis {analysisId}] Failed to broadcast completion");
            throw;
        }
    }
}
