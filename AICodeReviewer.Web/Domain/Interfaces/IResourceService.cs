namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for loading embedded resources
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Load the prompt template for git diff analysis
    /// </summary>
    /// <returns>The prompt template content</returns>
    string GetPromptTemplate();

    /// <summary>
    /// Load the prompt template for single file analysis
    /// </summary>
    /// <returns>The single file prompt template content</returns>
    string GetSingleFilePromptTemplate();
}