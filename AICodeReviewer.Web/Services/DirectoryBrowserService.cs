using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Services;

public class DirectoryBrowserService : IDirectoryBrowserService
{
    private readonly ILogger<DirectoryBrowserService> _logger;

    public DirectoryBrowserService(ILogger<DirectoryBrowserService> logger)
    {
        _logger = logger;
    }

    public async Task<DirectoryBrowseResponse> BrowseDirectoryAsync(string? currentPath)
    {
        try
        {
            string resolvedPath;
            
            // If no path provided, start with drives on Windows or root on Unix
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // On Windows, start with drive selection
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.IsReady)
                        .Select(d => new DirectoryItem
                        {
                            Name = d.Name.TrimEnd('\\'),
                            FullPath = d.RootDirectory.FullName,
                            IsDirectory = true,
                            LastModified = DateTime.Now,
                            Size = 0
                        }).ToList();

                    return new DirectoryBrowseResponse
                    {
                        Directories = drives,
                        Files = new List<DirectoryItem>(),
                        CurrentPath = "Computer",
                        ParentPath = null,
                        IsGitRepository = false
                    };
                }
                else
                {
                    // On Unix systems, start with root
                    resolvedPath = "/";
                }
            }
            else
            {
                resolvedPath = currentPath;
            }

            // Validate the path exists
            if (!Directory.Exists(resolvedPath))
            {
                return new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = resolvedPath,
                    ParentPath = null,
                    IsGitRepository = false,
                    Error = "Directory not found"
                };
            }

            // Get parent directory
            string? parentPath = null;
            try
            {
                var parent = Directory.GetParent(resolvedPath);
                parentPath = parent?.FullName;
                
                // Special case for root directory on Unix
                if (Environment.OSVersion.Platform != PlatformID.Win32NT && resolvedPath == "/")
                {
                    parentPath = null;
                }
                
                // Special case for drive root on Windows
                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    (resolvedPath.Length == 3 && resolvedPath.EndsWith(":\\")))
                {
                    parentPath = null; // Go back to drive selection
                }
            }
            catch
            {
                parentPath = null;
            }

            // Get directories and files
            var directories = new List<DirectoryItem>();
            var files = new List<DirectoryItem>();

            try
            {
                // Get directories
                var dirInfo = new DirectoryInfo(resolvedPath);
                
                // Get directories - synchronous operation since Directory.Exists is CPU-bound
                foreach (var dir in dirInfo.GetDirectories())
                {
                    try
                    {
                        bool isGitRepo = Directory.Exists(Path.Combine(dir.FullName, ".git"));
                        directories.Add(new DirectoryItem
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            IsDirectory = true,
                            IsGitRepository = isGitRepo,
                            LastModified = dir.LastWriteTime,
                            Size = 0
                        });
                    }
                    catch
                    {
                        // Skip directories we can't access
                        continue;
                    }
                }

                // Get files (only show relevant types)
                var allowedExtensions = new[] { ".cs", ".js", ".py", ".json", ".xml", ".config", ".md", ".txt", ".yml", ".yaml" };
                
                foreach (var file in dirInfo.GetFiles())
                {
                    try
                    {
                        if (allowedExtensions.Contains(file.Extension.ToLower()))
                        {
                            files.Add(new DirectoryItem
                            {
                                Name = file.Name,
                                FullPath = file.FullName,
                                IsDirectory = false,
                                IsGitRepository = false,
                                LastModified = file.LastWriteTime,
                                Size = file.Length
                            });
                        }
                    }
                    catch
                    {
                        // Skip files we can't access
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                return new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = resolvedPath,
                    ParentPath = parentPath,
                    IsGitRepository = false,
                    Error = $"Error accessing directory: {ex.Message}"
                };
            }

            // Check if current directory is a git repository
            bool isCurrentGitRepo = Directory.Exists(Path.Combine(resolvedPath, ".git"));

            return new DirectoryBrowseResponse
            {
                Directories = directories.OrderBy(d => d.Name).ToList(),
                Files = files.OrderBy(f => f.Name).ToList(),
                CurrentPath = resolvedPath,
                ParentPath = parentPath,
                IsGitRepository = isCurrentGitRepo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing directory: {CurrentPath}", currentPath);
            return new DirectoryBrowseResponse
            {
                Directories = new List<DirectoryItem>(),
                Files = new List<DirectoryItem>(),
                CurrentPath = currentPath ?? string.Empty,
                ParentPath = null,
                IsGitRepository = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }
}