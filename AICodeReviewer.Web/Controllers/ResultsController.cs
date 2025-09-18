using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AICodeReviewer.Web.Controllers;

/// <summary>
/// Controller for displaying code review analysis results with split-pane interface
/// </summary>
public class ResultsController : Controller
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResultsController> _logger;
    private readonly IAIPromptResponseService _aiPromptResponseService;

    public ResultsController(IMemoryCache cache, ILogger<ResultsController> logger, IAIPromptResponseService aiPromptResponseService)
    {
        _cache = cache;
        _logger = logger;
        _aiPromptResponseService = aiPromptResponseService;
    }

    /// <summary>
    /// Main results display page with split-pane layout
    /// </summary>
    [HttpGet("/results/{analysisId}")]
    public IActionResult Index(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId))
        {
            _logger.LogWarning("Results requested with empty analysis ID");
            return RedirectToAction("Index", "Home");
        }

        _logger.LogInformation($"Displaying results for analysis {analysisId}");
        // Redirect to main page - JavaScript will handle displaying results
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// API endpoint to retrieve structured analysis results
    /// </summary>
    [HttpGet("/api/results/{analysisId}")]
    public async Task<IActionResult> GetResults(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId))
        {
            return BadRequest(new { error = "Analysis ID is required" });
        }

        try
        {
            // Retrieve analysis result from cache
            if (!_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result) || result == null)
            {
                _logger.LogWarning($"Analysis {analysisId} not found in cache");
                return NotFound(new { error = "Analysis not found or expired" });
            }

            // Retrieve content from cache (could be git diff or file content)
            var rawContent = _cache.Get<string>($"content_{analysisId}") ?? string.Empty;

            // Parse raw AI response into structured feedback using the service
            var feedback = _aiPromptResponseService.ParseAIResponse(result.Result ?? string.Empty);
            _logger.LogInformation($"[Analysis {analysisId}] Parsed {feedback.Count} feedback items from AI response");

            var analysisResults = new AnalysisResults
            {
                AnalysisId = analysisId,
                Feedback = feedback,
                RawDiff = rawContent,
                RawResponse = result.Result,
                CreatedAt = result.CreatedAt,
                IsComplete = !string.IsNullOrEmpty(result.Result) && string.IsNullOrEmpty(result.Error),
                Error = result.Error
            };

            _logger.LogInformation($"[Analysis {analysisId}] Returning analysis results with {analysisResults.Feedback.Count} feedback items and {analysisResults.RawDiff.Length} bytes of diff data");
            return Json(analysisResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving results for analysis {analysisId}");
            return StatusCode(500, new { error = "Failed to retrieve analysis results" });
        }
    }

    /// <summary>
    /// API endpoint to retrieve raw content data (git diff or file content)
    /// </summary>
    [HttpGet("/api/diff/{analysisId}")]
    public async Task<IActionResult> GetDiff(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId))
        {
            return BadRequest(new { error = "Analysis ID is required" });
        }

        try
        {
            var rawContent = _cache.Get<string>($"content_{analysisId}") ?? string.Empty;
            
            if (string.IsNullOrEmpty(rawContent))
            {
                _logger.LogWarning($"Content for analysis {analysisId} not found in cache");
                return NotFound(new { error = "Content data not found" });
            }

            return Content(rawContent, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving content for analysis {analysisId}");
            return StatusCode(500, new { error = "Failed to retrieve content data" });
        }
    }

}