using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Services;

public interface IDirectoryBrowserService
{
    Task<DirectoryBrowseResponse> BrowseDirectoryAsync(string? currentPath);
}