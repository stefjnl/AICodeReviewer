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
    /// Parse raw AI response text into structured feedback items with enhanced issue extraction
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
            // First, try to split by numbered or bulleted issues
            var issues = SplitIntoIssues(rawResponse);
            
            foreach (var issue in issues)
            {
                var feedbackItem = ParseIndividualIssue(issue);
                if (feedbackItem != null)
                {
                    feedback.Add(feedbackItem);
                }
            }

            // If no issues found, try pattern-based extraction
            if (feedback.Count == 0)
            {
                feedback = ExtractIssuesByPatterns(rawResponse);
            }

            // If still no issues, create a general feedback item
            if (feedback.Count == 0 && !string.IsNullOrWhiteSpace(rawResponse))
            {
                feedback.Add(CreateGeneralFeedbackItem(rawResponse));
            }

            _logger.LogInformation($"Successfully parsed {feedback.Count} feedback items");
            return feedback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI response into structured feedback");
            return new List<FeedbackItem> { CreateGeneralFeedbackItem(rawResponse) };
        }
    }

    /// <summary>
    /// Split AI response into individual issues using various delimiters
    /// </summary>
    private List<string> SplitIntoIssues(string response)
    {
        var issues = new List<string>();
        
        // Try different patterns to split issues
        var patterns = new[]
        {
            @"\d+\.\s+(.+?)(?=\d+\.\s+|\n\n|$)", // Numbered: "1. Issue text"
            @"[-•]\s+(.+?)(?=[-•]\s+|\n\n|$)",    // Bulleted: "- Issue text" or "• Issue text"
            @"\*\s*(.+?)(?=\*\s*|\n\n|$)",       // Asterisk: "* Issue text"
            @"\n\n(.+?)(?=\n\n|$)"               // Double newline separated
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(response, pattern, RegexOptions.Singleline);
            if (matches.Count > 1) // Only use if we found multiple issues
            {
                issues.AddRange(matches.Select(m => m.Groups[1].Value.Trim()));
                break;
            }
        }

        // If no patterns matched, treat the whole response as one issue
        if (issues.Count == 0)
        {
            issues.Add(response);
        }

        return issues.Where(issue => !string.IsNullOrWhiteSpace(issue)).ToList();
    }

    /// <summary>
    /// Parse an individual issue into a structured FeedbackItem
    /// </summary>
    private FeedbackItem? ParseIndividualIssue(string issue)
    {
        if (string.IsNullOrWhiteSpace(issue))
            return null;

        // Determine severity from issue text
        var severity = DetermineSeverity(issue);
        
        // Extract suggestion if present
        var (message, suggestion) = ExtractMessageAndSuggestion(issue);
        
        // Create feedback item
        return new FeedbackItem
        {
            Severity = severity,
            Message = message,
            Suggestion = suggestion,
            Category = DetermineCategory(issue),
            FilePath = ExtractFilePath(issue),
            LineNumber = ExtractLineNumber(issue)
        };
    }

    /// <summary>
    /// Extract severity level from issue text
    /// </summary>
    private string DetermineSeverity(string issue)
    {
        var lowerIssue = issue.ToLower();
        
        if (Regex.IsMatch(lowerIssue, @"(critical|error|must fix|security|vulnerability|injection)"))
            return "Critical";
        if (Regex.IsMatch(lowerIssue, @"(warning|should|performance|potential|consider|recommend)"))
            return "Warning";
        if (Regex.IsMatch(lowerIssue, @"(style|formatting|naming|convention)"))
            return "Style";
        
        return "Suggestion"; // Default
    }

    /// <summary>
    /// Extract main message and suggestion from issue text
    /// </summary>
    private (string message, string? suggestion) ExtractMessageAndSuggestion(string issue)
    {
        // Look for suggestion patterns like "Suggestion:", "Consider:", "Try:"
        var suggestionPattern = @"(suggestion|consider|try|you could|it would be better):?\s*(.+)";
        var suggestionMatch = Regex.Match(issue, suggestionPattern, RegexOptions.IgnoreCase);
        
        if (suggestionMatch.Success)
        {
            var mainMessage = issue.Substring(0, suggestionMatch.Index).Trim();
            var suggestion = suggestionMatch.Groups[2].Value.Trim();
            return (mainMessage, suggestion);
        }
        
        // Look for "to fix this" or similar patterns
        var fixPattern = @"(to fix this|to resolve this|solution):?\s*(.+)";
        var fixMatch = Regex.Match(issue, fixPattern, RegexOptions.IgnoreCase);
        
        if (fixMatch.Success)
        {
            var mainMessage = issue.Substring(0, fixMatch.Index).Trim();
            var suggestion = fixMatch.Groups[2].Value.Trim();
            return (mainMessage, suggestion);
        }
        
        return (issue.Trim(), null);
    }

    /// <summary>
    /// Extract issues using predefined patterns
    /// </summary>
    private List<FeedbackItem> ExtractIssuesByPatterns(string response)
    {
        var feedback = new List<FeedbackItem>();
        
        var patterns = new Dictionary<string, Regex>
        {
            ["Critical"] = new Regex(@"(critical|error|must fix|security issue):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
            ["Warning"] = new Regex(@"(warning|should|performance issue|potential problem):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
            ["Suggestion"] = new Regex(@"(suggestion|consider|recommend|improvement):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
            ["Style"] = new Regex(@"(style|formatting|naming|convention):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase)
        };

        foreach (var (severity, pattern) in patterns)
        {
            var matches = pattern.Matches(response);
            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    var issue = match.Groups[1].Value.Trim();
                    var (message, suggestion) = ExtractMessageAndSuggestion(issue);
                    
                    feedback.Add(new FeedbackItem
                    {
                        Severity = severity,
                        Message = message,
                        Suggestion = suggestion,
                        Category = DetermineCategory(issue),
                        FilePath = ExtractFilePath(issue),
                        LineNumber = ExtractLineNumber(issue)
                    });
                }
            }
        }

        return feedback;
    }

    /// <summary>
    /// Create a general feedback item when no specific issues are found
    /// </summary>
    private FeedbackItem CreateGeneralFeedbackItem(string rawResponse)
    {
        var (message, suggestion) = ExtractMessageAndSuggestion(rawResponse);
        
        return new FeedbackItem
        {
            Severity = "Suggestion",
            Message = message,
            Suggestion = suggestion,
            Category = "General",
            FilePath = string.Empty
        };
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