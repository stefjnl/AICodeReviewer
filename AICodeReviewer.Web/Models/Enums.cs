using System.Text.Json.Serialization;
using System.Runtime.Serialization;

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

/// <summary>
/// Supported programming languages for AI code analysis
/// </summary>
public enum SupportedLanguage
{
    NET,
    Python,
    JavaScript,
    HTML
}

/// <summary>
/// Types of analysis that can be performed
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisType
{
    /// <summary>
    /// Analyze all uncommitted changes (staged and unstaged)
    /// </summary>
    [EnumMember(Value = "uncommitted")]
    Uncommitted,
    
    /// <summary>
    /// Analyze only staged changes (files added with git add)
    /// </summary>
    [EnumMember(Value = "staged")]
    Staged,
    
    /// <summary>
    /// Analyze changes in a specific commit
    /// </summary>
    [EnumMember(Value = "commit")]
    Commit,
    
    /// <summary>
    /// Analyze a single file
    /// </summary>
    [EnumMember(Value = "singlefile")]
    SingleFile,
    
    /// <summary>
    /// Analyze changes between two branches (pull request differential)
    /// </summary>
    [EnumMember(Value = "pullrequest")]
    PullRequestDifferential
}