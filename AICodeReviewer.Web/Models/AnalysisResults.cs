namespace AICodeReviewer.Web.Models;

/// <summary>
/// Complete analysis results including structured feedback and raw diff data
/// </summary>
public class AnalysisResults
{
    /// <summary>
    /// Unique identifier for this analysis
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Structured feedback items from AI analysis
    /// </summary>
    public List<FeedbackItem> Feedback { get; set; } = new();

    /// <summary>
    /// Raw git diff content
    /// </summary>
    public string RawDiff { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when analysis was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the analysis is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Raw AI response text (for fallback parsing)
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Any error message if analysis failed
    /// </summary>
    public string? Error { get; set; }
}