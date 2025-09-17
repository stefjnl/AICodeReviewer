namespace AICodeReviewer.Web.Domain;

/// <summary>
/// Represents a template for AI prompts based on programming language characteristics
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// The role or persona the AI should adopt
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The name of the programming language
    /// </summary>
    public string LanguageName { get; set; } = string.Empty;

    /// <summary>
    /// File extensions associated with this language
    /// </summary>
    public string[] FileExtensions { get; set; } = [];

    /// <summary>
    /// Naming conventions for the language
    /// </summary>
    public string NamingConvention { get; set; } = string.Empty;

    /// <summary>
    /// Asynchronous programming patterns used in the language
    /// </summary>
    public string AsyncPattern { get; set; } = string.Empty;

    /// <summary>
    /// Best practices for the language
    /// </summary>
    public string BestPractices { get; set; } = string.Empty;
}