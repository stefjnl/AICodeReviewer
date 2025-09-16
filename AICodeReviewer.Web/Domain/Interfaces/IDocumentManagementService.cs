namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for managing documents and coding standards
/// </summary>
public interface IDocumentManagementService
{
    /// <summary>
    /// Scan documents folder for available markdown files
    /// </summary>
    /// <param name="folderPath">Path to the documents folder</param>
    /// <returns>List of document names and error status</returns>
    (List<string> files, bool isError) ScanDocumentsFolder(string folderPath);

    /// <summary>
    /// Load a specific document from the folder
    /// </summary>
    /// <param name="fileName">Name of the document without extension</param>
    /// <param name="folderPath">Path to the documents folder</param>
    /// <returns>Document content and error status</returns>
    (string content, bool isError) LoadDocument(string fileName, string folderPath);

    /// <summary>
    /// Load a specific document from the folder asynchronously
    /// </summary>
    /// <param name="fileName">Name of the document without extension</param>
    /// <param name="folderPath">Path to the documents folder</param>
    /// <returns>Document content and error status</returns>
    Task<(string content, bool isError)> LoadDocumentAsync(string fileName, string folderPath);

    /// <summary>
    /// Convert filename to user-friendly display name
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <returns>Display-friendly name</returns>
    string GetDocumentDisplayName(string fileName);

    /// <summary>
    /// Validate documents folder path
    /// </summary>
    /// <param name="folderPath">Path to validate</param>
    /// <returns>Validation result and normalized path</returns>
    (bool isValid, string? normalizedPath, string? error) ValidateDocumentsFolder(string folderPath);
}