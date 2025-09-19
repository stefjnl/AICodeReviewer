using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for managing background task execution
/// </summary>
public class BackgroundTaskService
{
    private readonly ILogger<BackgroundTaskService> _logger;

    public BackgroundTaskService(ILogger<BackgroundTaskService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Execute a background task with proper error handling and logging
    /// </summary>
    public void ExecuteBackgroundTask(string taskName, string analysisId, Func<Task> taskFunc)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation($"[Analysis {analysisId}] Starting background task: {taskName}");
                await taskFunc();
                _logger.LogInformation($"[Analysis {analysisId}] Background task completed: {taskName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Background task failed: {taskName}");
                throw; // Re-throw to allow caller to handle
            }
        }).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                _logger.LogError(t.Exception, $"[Analysis {analysisId}] Unobserved exception in background task: {taskName}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Execute a background task with custom error handling
    /// </summary>
    public void ExecuteBackgroundTask(
        string taskName,
        string analysisId,
        Func<Task> taskFunc,
        Func<Exception, Task> errorHandler)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation($"[Analysis {analysisId}] Starting background task: {taskName}");
                await taskFunc();
                _logger.LogInformation($"[Analysis {analysisId}] Background task completed: {taskName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Background task failed: {taskName}");
                await errorHandler(ex);
            }
        }).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                _logger.LogError(t.Exception, $"[Analysis {analysisId}] Unobserved exception in background task: {taskName}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
