using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace AICodeReviewer.Web.Controllers;

/// <summary>
/// Controller for displaying code review analysis results with split-pane interface
/// </summary>
public class ResultsController : Controller
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResultsController> _logger;

    public ResultsController(IMemoryCache cache, ILogger<ResultsController> logger)
    {
        _cache = cache;
        _logger = logger;
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
        ViewBag.AnalysisId = analysisId;
        return View();
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

            // Retrieve git diff from cache
            var rawDiff = _cache.Get<string>($"diff_{analysisId}") ?? string.Empty;

            // Parse raw AI response into structured feedback
            var feedback = ParseAIResponse(result.Result ?? string.Empty);
            _logger.LogInformation($"[Analysis {analysisId}] Parsed {feedback.Count} feedback items from AI response");

            var analysisResults = new AnalysisResults
            {
                AnalysisId = analysisId,
                Feedback = feedback,
                RawDiff = rawDiff,
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
    /// API endpoint to retrieve raw git diff data
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
            var rawDiff = _cache.Get<string>($"diff_{analysisId}") ?? string.Empty;
            
            if (string.IsNullOrEmpty(rawDiff))
            {
                _logger.LogWarning($"Diff for analysis {analysisId} not found in cache");
                return NotFound(new { error = "Diff data not found" });
            }

            return Content(rawDiff, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving diff for analysis {analysisId}");
            return StatusCode(500, new { error = "Failed to retrieve diff data" });
        }
    }

    /// <summary>
    /// Parse raw AI response text into structured feedback items
    /// </summary>
    private List<FeedbackItem> ParseAIResponse(string rawResponse)
    {
        var feedback = new List<FeedbackItem>();

        if (string.IsNullOrEmpty(rawResponse))
        {
            return feedback;
        }

        try
        {
            // Define patterns for different severity levels
            var patterns = new Dictionary<string, Regex>
            {
                ["Critical"] = new Regex(@"(?:critical|error|must fix|security issue):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
                ["Warning"] = new Regex(@"(?:warning|should|performance issue|potential problem):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
                ["Suggestion"] = new Regex(@"(?:suggestion|consider|recommend|improvement):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
                ["Style"] = new Regex(@"(?:style|formatting|naming|convention):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase)
            };

            // Extract feedback items using patterns
            foreach (var (severity, pattern) in patterns)
            {
                var matches = pattern.Matches(rawResponse);
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var item = new FeedbackItem
                        {
                            Severity = severity,
                            Message = match.Groups[1].Value.Trim(),
                            Category = DetermineCategory(match.Groups[1].Value),
                            FilePath = ExtractFilePath(match.Groups[1].Value),
                            LineNumber = ExtractLineNumber(match.Groups[1].Value)
                        };

                        feedback.Add(item);
                    }
                }
            }

            // If no structured patterns found, create a general feedback item
            if (feedback.Count == 0 && !string.IsNullOrWhiteSpace(rawResponse))
            {
                feedback.Add(new FeedbackItem
                {
                    Severity = "Suggestion",
                    Message = rawResponse,
                    Category = "General",
                    FilePath = string.Empty
                });
            }

            return feedback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response into structured feedback");
            
            // Fallback: return raw response as single feedback item
            return new List<FeedbackItem>
            {
                new FeedbackItem
                {
                    Severity = "Suggestion",
                    Message = rawResponse,
                    Category = "General",
                    FilePath = string.Empty
                }
            };
        }
    }

    /// <summary>
    /// Determine the category of feedback based on message content
    /// </summary>
    private string DetermineCategory(string message)
    {
        if (message.Contains("security", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("vulnerability", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("injection", StringComparison.OrdinalIgnoreCase))
        {
            return "Security";
        }

        if (message.Contains("performance", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("slow", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("optimization", StringComparison.OrdinalIgnoreCase))
        {
            return "Performance";
        }

        if (message.Contains("naming", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("style", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("formatting", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("convention", StringComparison.OrdinalIgnoreCase))
        {
            return "Style";
        }

        if (message.Contains("error handling", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return "Error Handling";
        }

        return "General";
    }

    /// <summary>
    /// Extract file path from feedback message
    /// </summary>
    private string ExtractFilePath(string message)
    {
        // Look for file paths in various formats
        var patterns = new[]
        {
            @"([A-Za-z]:\\[^:\n]+)", // Windows paths
            @"([A-Za-z0-9_/\\]+\.(cs|js|ts|html|css|json|xml|config))", // Relative paths
            @"(\./[^:\n]+)", // Unix-style paths
            @"([A-Za-z0-9_\-]+\.(cs|js|ts|html|css|json|xml|config))" // Just filename
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extract line number from feedback message
    /// </summary>
    private int? ExtractLineNumber(string message)
    {
        // Look for line numbers in various formats
        var patterns = new[]
        {
            @"line (\d+)", // "line 123"
            @"line:(\d+)", // "line:123"
            @"L(\d+)", // "L123"
            @":(\d+):", // ":123:"
            @"at line (\d+)" // "at line 123"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNumber))
            {
                return lineNumber;
            }
        }

        return null;
    }
}