namespace AICodeReviewer.Web.Models;

/// <summary>
/// Represents a single piece of structured feedback from AI code review analysis
/// </summary>
public class FeedbackItem
{
    /// <summary>
    /// Severity level of the feedback (Critical/Warning/Suggestion/Style)
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// File path where the issue was found
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the issue occurs (optional)
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Detailed message about the issue
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Category of the feedback (Security/Performance/Style/etc)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Optional code snippet showing the problematic code
    /// </summary>
    public string? CodeSnippet { get; set; }

    /// <summary>
    /// Optional suggestion for fixing the issue
    /// </summary>
    public string? Suggestion { get; set; }
}