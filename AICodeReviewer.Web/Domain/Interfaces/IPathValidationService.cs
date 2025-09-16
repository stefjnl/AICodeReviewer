namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for validating and resolving file paths
/// </summary>
public interface IPathValidationService
{
    /// <summary>
    /// Normalize and validate a path, handling relative and absolute paths
    /// </summary>
    /// <param name="path">Path to normalize</param>
    /// <param name="basePath">Base path for relative path resolution</param>
    /// <returns>Normalized path and validation status</returns>
    (string normalizedPath, bool isValid, string? error) NormalizePath(string path, string basePath);

    /// <summary>
    /// Validate a file path for single file analysis
    /// </summary>
    /// <param name="filePath">File path to validate</param>
    /// <param name="repositoryPath">Repository path for context</param>
    /// <param name="contentRootPath">Web application content root path</param>
    /// <returns>Resolved file path and validation status</returns>
    (string resolvedPath, bool isValid, string? error) ValidateSingleFilePath(
        string filePath,
        string repositoryPath,
        string contentRootPath);

    /// <summary>
    /// Check if file has a supported extension for analysis
    /// </summary>
    /// <param name="filePath">File path to check</param>
    /// <returns>Validation result and error message if unsupported</returns>
    (bool isSupported, string? error) ValidateFileExtension(string filePath);

    /// <summary>
    /// Validate that a directory exists and is accessible
    /// </summary>
    /// <param name="directoryPath">Directory path to validate</param>
    /// <returns>Validation result and error message</returns>
    (bool isValid, string? error) ValidateDirectoryExists(string directoryPath);
}