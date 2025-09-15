namespace AICodeReviewer.Web.Models;

/// <summary>
/// Represents a single piece of structured feedback from AI code review analysis
/// </summary>
public class FeedbackItem
{
    /// <summary>
    /// Severity level of the feedback (Critical/Warning/Suggestion/Style)
    /// </summary>
    public Severity Severity { get; set; } = Severity.Suggestion;

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
    public Category Category { get; set; } = Category.General;

    /// <summary>
    /// Optional code snippet showing the problematic code
    /// </summary>
    public string? CodeSnippet { get; set; }

    /// <summary>
    /// Optional suggestion for fixing the issue
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// Get the severity as a string for display purposes
    /// </summary>
    public string SeverityString => Severity.ToString();

    /// <summary>
    /// Get the category as a string for display purposes
    /// </summary>
    public string CategoryString => Category.ToString();

    /// <summary>
    /// Get the color associated with this severity
    /// </summary>
    public string SeverityColor => FeedbackConstants.SeverityColors.GetValueOrDefault(Severity, "#6c757d");

    /// <summary>
    /// Get the icon associated with this severity
    /// </summary>
    public string SeverityIcon => FeedbackConstants.SeverityIcons.GetValueOrDefault(Severity, "ℹ️");
}