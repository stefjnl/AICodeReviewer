using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Domain.Interfaces;

/// <summary>
/// Service for browsing file system directories
/// </summary>
public interface IDirectoryBrowsingService
{
    /// <summary>
    /// Browse directory contents with file filtering
    /// </summary>
    /// <param name="currentPath">Path to browse</param>
    /// <returns>Directory contents with files and subdirectories</returns>
    DirectoryBrowseResponse BrowseDirectory(string currentPath);

    /// <summary>
    /// Get root drives for Windows systems
    /// </summary>
    /// <returns>List of available drives</returns>
    List<DirectoryItem> GetRootDrives();

    /// <summary>
    /// Check if a path is a git repository
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if the path contains a git repository</returns>
    bool IsGitRepository(string path);

    /// <summary>
    /// Get parent directory path, handling special cases like root
    /// </summary>
    /// <param name="currentPath">Current directory path</param>
    /// <returns>Parent path or null if at root</returns>
    string? GetParentPath(string currentPath);

    /// <summary>
    /// Validate directory path and check accessibility
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <returns>Validation result and error message</returns>
    (bool isValid, string? error) ValidateDirectoryPath(string path);
}