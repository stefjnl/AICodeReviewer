using AICodeReviewer.Web.Models;
using System.Text.RegularExpressions;
using AICodeReviewer.Web.Domain.Interfaces;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for parsing AI responses and extracting structured feedback
/// </summary>
public class AIPromptResponseService : IAIPromptResponseService
{
    private readonly ILogger<AIPromptResponseService> _logger;

    public AIPromptResponseService(ILogger<AIPromptResponseService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse raw AI response text into structured feedback items with enhanced issue extraction
    /// </summary>
    public List<FeedbackItem> ParseAIResponse(string rawResponse)
    {
        var feedback = new List<FeedbackItem>();

        if (string.IsNullOrEmpty(rawResponse))
        {
            return feedback;
        }

        try
        {
            // Try parsing new format first
            var newFormatFeedback = ParseNewFormatResponse(rawResponse);
            if (newFormatFeedback.Count > 0)
            {
                return newFormatFeedback;
            }

            // Fallback to old format parsing
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
    private Severity DetermineSeverity(string issue)
    {
        var lowerIssue = issue.ToLower();
        
        if (Regex.IsMatch(lowerIssue, @"(critical|error|must fix|security|vulnerability|injection)"))
            return Severity.Critical;
        if (Regex.IsMatch(lowerIssue, @"(warning|should|performance|potential|consider|recommend)"))
            return Severity.Warning;
        if (Regex.IsMatch(lowerIssue, @"(style|formatting|naming|convention)"))
            return Severity.Style;
        
        return Severity.Suggestion; // Default
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
        
        var patterns = new Dictionary<Severity, Regex>
        {
            [Severity.Critical] = new Regex(@"(critical|error|must fix|security issue):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
            [Severity.Warning] = new Regex(@"(warning|should|performance issue|potential problem):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
            [Severity.Suggestion] = new Regex(@"(suggestion|consider|recommend|improvement):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase),
            [Severity.Style] = new Regex(@"(style|formatting|naming|convention):\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase)
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
            Severity = Severity.Suggestion,
            Message = message,
            Suggestion = suggestion,
            Category = Category.General,
            FilePath = string.Empty
        };
    }

    /// <summary>
    /// Determine the category of feedback based on message content
    /// </summary>
    private Category DetermineCategory(string message)
    {
        if (message.Contains("security", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("vulnerability", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("injection", StringComparison.OrdinalIgnoreCase))
        {
            return Category.Security;
        }

        if (message.Contains("performance", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("slow", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("optimization", StringComparison.OrdinalIgnoreCase))
        {
            return Category.Performance;
        }

        if (message.Contains("naming", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("style", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("formatting", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("convention", StringComparison.OrdinalIgnoreCase))
        {
            return Category.Style;
        }

        if (message.Contains("error handling", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return Category.ErrorHandling;
        }

        return Category.General;
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

    /// <summary>
    /// Parse new format response with enhanced layout
    /// </summary>
    private List<FeedbackItem> ParseNewFormatResponse(string response)
    {
        var feedback = new List<FeedbackItem>();
        
        // Only attempt new format parsing if we have the exact format markers
        if (!response.Contains("📊 SUMMARY:") &&
            !response.Contains("🚨 CRITICAL ISSUES"))
        {
            return feedback;
        }
        
        // Parse critical issues section
        var criticalSection = ExtractSection(response, "🚨 CRITICAL ISSUES", "⚠️ WARNINGS");
        feedback.AddRange(ParseSection(criticalSection, Severity.Critical));
        
        // Parse warnings section
        var warningSection = ExtractSection(response, "⚠️ WARNINGS", "💡 IMPROVEMENTS");
        feedback.AddRange(ParseSection(warningSection, Severity.Warning));
        
        // Parse improvements section
        var improvementSection = ExtractSection(response, "💡 IMPROVEMENTS", null);
        feedback.AddRange(ParseSection(improvementSection, Severity.Suggestion));
        
        return feedback;
    }

    /// <summary>
    /// Extract a section from the response based on start and end markers
    /// </summary>
    private string ExtractSection(string response, string startMarker, string? endMarker)
    {
        var startIndex = response.IndexOf(startMarker);
        if (startIndex == -1) return string.Empty;
        
        var actualStart = startIndex + startMarker.Length;
        var endIndex = endMarker != null ? response.IndexOf(endMarker, actualStart) : -1;
        var length = endIndex == -1 ? response.Length - actualStart : endIndex - actualStart;
        
        return response.Substring(actualStart, length);
    }

    /// <summary>
    /// Parse a section into feedback items (new format only)
    /// </summary>
    private List<FeedbackItem> ParseSection(string section, Severity defaultSeverity)
    {
        var items = new List<FeedbackItem>();
        
        if (string.IsNullOrWhiteSpace(section))
            return items;
        
        // Parse individual items from section
        var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentItem = new FeedbackItem { Severity = defaultSeverity };
        bool hasValidContent = false;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
            
            if (trimmedLine.StartsWith("🚨") || trimmedLine.StartsWith("⚠️") || trimmedLine.StartsWith("💡"))
            {
                if (hasValidContent)
                {
                    items.Add(currentItem);
                    currentItem = new FeedbackItem { Severity = defaultSeverity };
                }
                currentItem.Message = trimmedLine.Substring(1).Trim();
                hasValidContent = true;
            }
            else if (trimmedLine.StartsWith("File:"))
            {
                var fileMatch = Regex.Match(trimmedLine, @"File:\s*([^\n:]+)(?::(\d+))?");
                if (fileMatch.Success)
                {
                    currentItem.FilePath = fileMatch.Groups[1].Value.Trim();
                    if (int.TryParse(fileMatch.Groups[2].Value, out int lineNum))
                        currentItem.LineNumber = lineNum;
                }
            }
            else if (trimmedLine.StartsWith("Issue:") || trimmedLine.StartsWith("Fix:") ||
                     trimmedLine.StartsWith("Suggestion:") || trimmedLine.StartsWith("Better:"))
            {
                var content = trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim();
                if (trimmedLine.StartsWith("Issue:"))
                    currentItem.Message = content;
                else
                    currentItem.Suggestion = content;
            }
        }
        
        if (hasValidContent)
            items.Add(currentItem);
        
        return items;
    }

    /// <summary>
    /// Parse individual item from new format
    /// </summary>
    private FeedbackItem? ParseNewFormatItem(string itemText, Severity severity)
    {
        if (string.IsNullOrWhiteSpace(itemText))
            return null;

        // Extract brief description
        var descriptionMatch = Regex.Match(itemText, @"^([^\n]+)");
        var briefDescription = descriptionMatch.Success ? descriptionMatch.Groups[1].Value.Trim() : "Issue found";

        // Extract file and line
        var fileLineMatch = Regex.Match(itemText, @"File:\s*([^\n:]+)(?::(\d+))?");
        var filePath = fileLineMatch.Success ? fileLineMatch.Groups[1].Value.Trim() : string.Empty;
        var lineNumber = fileLineMatch.Success && fileLineMatch.Groups.Count > 2 && int.TryParse(fileLineMatch.Groups[2].Value, out int line) ? line : (int?)null;

        // Extract issue/suggestion text
        var issueMatch = Regex.Match(itemText, @"Issue:\s*([^\n]+)");
        var suggestionMatch = Regex.Match(itemText, @"(?:Fix|Suggestion|Better):\s*([^\n]+)");
        var impactMatch = Regex.Match(itemText, @"Impact|Benefit:\s*([^\n]+)");
        
        var message = issueMatch.Success ? issueMatch.Groups[1].Value.Trim() : briefDescription;
        var suggestion = suggestionMatch.Success ? suggestionMatch.Groups[1].Value.Trim() : null;

        return new FeedbackItem
        {
            Severity = severity,
            Message = message,
            Suggestion = suggestion,
            FilePath = filePath,
            LineNumber = lineNumber,
            Category = DetermineCategory(message)
        };
    }
}