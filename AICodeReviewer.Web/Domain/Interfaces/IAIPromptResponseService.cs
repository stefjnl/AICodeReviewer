using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for parsing AI responses and extracting structured feedback
/// </summary>
public interface IAIPromptResponseService
{
    /// <summary>
    /// Parse raw AI response text into structured feedback items with enhanced issue extraction
    /// </summary>
    List<FeedbackItem> ParseAIResponse(string rawResponse);
}