namespace AICodeReviewer.Web.Models;

/// <summary>
/// Severity levels for code review feedback
/// </summary>
public enum Severity
{
    Critical,
    Warning,
    Suggestion,
    Style,
    Info
}

/// <summary>
/// Categories for code review feedback
/// </summary>
public enum Category
{
    Security,
    Performance,
    Style,
    ErrorHandling,
    General,
    Maintainability,
    Readability
}

/// <summary>
/// Constants for feedback parsing and display
/// </summary>
public static class FeedbackConstants
{
    public const string DEFAULT_SEVERITY = "Suggestion";
    public const string DEFAULT_CATEGORY = "General";
    
    public static readonly Dictionary<Severity, string> SeverityColors = new()
    {
        { Severity.Critical, "#dc3545" }, // Red
        { Severity.Warning, "#fd7e14" },  // Orange
        { Severity.Suggestion, "#0dcaf0" }, // Cyan
        { Severity.Style, "#6f42c1" },    // Purple
        { Severity.Info, "#6c757d" }      // Gray
    };
    
    public static readonly Dictionary<Severity, string> SeverityIcons = new()
    {
        { Severity.Critical, "🚨" },
        { Severity.Warning, "⚠️" },
        { Severity.Suggestion, "💡" },
        { Severity.Style, "🎨" },
        { Severity.Info, "ℹ️" }
    };
}