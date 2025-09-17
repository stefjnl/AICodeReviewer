using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for AI-powered code analysis operations
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Analyze code using AI service with specified parameters
    /// </summary>
    /// <param name="content">Code content to analyze</param>
    /// <param name="codingStandards">List of coding standards to apply</param>
    /// <param name="requirements">Additional requirements for analysis</param>
    /// <param name="apiKey">API key for AI service</param>
    /// <param name="model">AI model to use</param>
    /// <param name="language">Programming language of the code</param>
    /// <param name="isFileContent">Whether the content is a single file or git diff</param>
    /// <returns>Analysis result, error status, and error message if any</returns>
    Task<(string analysis, bool isError, string? errorMessage)> AnalyzeCodeAsync(
        string content, 
        List<string> codingStandards, 
        string requirements, 
        string apiKey, 
        string model, 
        string language, 
        bool isFileContent = false);
}