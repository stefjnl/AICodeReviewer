using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Interface for executing AI analysis and processing results
/// </summary>
public interface IAnalysisExecutionService
{
    /// <summary>
    /// Executes AI analysis with the provided content and documents
    /// </summary>
    /// <param name="analysisId">The unique analysis identifier</param>
    /// <param name="content">The code content to analyze</param>
    /// <param name="selectedDocuments">List of selected coding standards documents</param>
    /// <param name="documentsFolder">Path to the documents folder</param>
    /// <param name="apiKey">API key for AI service</param>
    /// <param name="primaryModel">Primary AI model to use</param>
    /// <param name="fallbackModel">Fallback AI model</param>
    /// <param name="language">Programming language</param>
    /// <param name="isFileContent">Whether the content is from a single file</param>
    /// <param name="session">User session</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAnalysisAsync(
        string analysisId,
        string content,
        List<string> selectedDocuments,
        string documentsFolder,
        string apiKey,
        string primaryModel,
        string? fallbackModel,
        string language,
        bool isFileContent,
        ISession session);
}