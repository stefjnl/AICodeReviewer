using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for browsing file system directories
/// </summary>
public class DirectoryBrowsingService : IDirectoryBrowsingService
{
    private readonly ILogger<DirectoryBrowsingService> _logger;
    private static readonly string[] AllowedExtensions = { ".cs", ".js", ".py", ".json", ".xml", ".config", ".md", ".txt", ".yml", ".yaml" };

    public DirectoryBrowsingService(ILogger<DirectoryBrowsingService> logger)
    {
        _logger = logger;
    }

    public DirectoryBrowseResponse BrowseDirectory(string currentPath)
    {
        try
        {
            // Validate the path exists
            if (!Directory.Exists(currentPath))
            {
                _logger.LogWarning("Directory not found: {Path}", currentPath);
                return new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = currentPath,
                    ParentPath = null,
                    IsGitRepository = false,
                    Error = "Directory not found"
                };
            }

            // Get parent directory
            string? parentPath = GetParentPath(currentPath);

            // Get directories and files
            var directories = new List<DirectoryItem>();
            var files = new List<DirectoryItem>();

            try
            {
                // Get directories
                var dirInfo = new DirectoryInfo(currentPath);
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
                foreach (var file in dirInfo.GetFiles())
                {
                    try
                    {
                        if (AllowedExtensions.Contains(file.Extension.ToLower()))
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
                _logger.LogError(ex, "Error accessing directory contents: {Path}", currentPath);
                return new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = currentPath,
                    ParentPath = parentPath,
                    IsGitRepository = false,
                    Error = $"Error accessing directory: {ex.Message}"
                };
            }

            // Check if current directory is a git repository
            bool isCurrentGitRepo = IsGitRepository(currentPath);

            _logger.LogInformation("Browsed directory: {Path}, found {DirectoryCount} directories and {FileCount} files", 
                currentPath, directories.Count, files.Count);

            return new DirectoryBrowseResponse
            {
                Directories = directories.OrderBy(d => d.Name).ToList(),
                Files = files.OrderBy(f => f.Name).ToList(),
                CurrentPath = currentPath,
                ParentPath = parentPath,
                IsGitRepository = isCurrentGitRepo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error browsing directory: {Path}", currentPath);
            return new DirectoryBrowseResponse
            {
                Directories = new List<DirectoryItem>(),
                Files = new List<DirectoryItem>(),
                CurrentPath = currentPath,
                ParentPath = null,
                IsGitRepository = false,
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public List<DirectoryItem> GetRootDrives()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new DirectoryItem
                {
                    Name = d.Name.TrimEnd('\\'),
                    FullPath = d.RootDirectory.FullName,
                    IsDirectory = true,
                    IsGitRepository = false,
                    LastModified = DateTime.Now,
                    Size = 0
                }).ToList();

            _logger.LogInformation("Retrieved {Count} available drives", drives.Count);
            return drives;
        }

        // Check if /git is mounted (Docker setup)
        if (Directory.Exists("/git"))
        {
            _logger.LogInformation("Found /git mount, using as root directory");
            return new List<DirectoryItem>
            {
                new DirectoryItem
                {
                    Name = "Git Repositories",
                    FullPath = "/git",
                    IsDirectory = true,
                    IsGitRepository = false,
                    LastModified = DateTime.Now,
                    Size = 0
                }
            };
        }

        // On Unix systems (including Docker containers), return root with a note
        return new List<DirectoryItem>
        {
            new DirectoryItem
            {
                Name = "Container Root (Docker)",
                FullPath = "/",
                IsDirectory = true,
                IsGitRepository = false,
                LastModified = DateTime.Now,
                Size = 0
            }
        };
    }

    public bool IsGitRepository(string path)
    {
        try
        {
            bool isGitRepo = Directory.Exists(Path.Combine(path, ".git"));
            _logger.LogDebug("Git repository check for {Path}: {IsGitRepo}", path, isGitRepo);
            return isGitRepo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if path is git repository: {Path}", path);
            return false;
        }
    }

    public string? GetParentPath(string currentPath)
    {
        try
        {
            string? parentPath = null;
            
            // Special case for root directory on Unix
            if (Environment.OSVersion.Platform != PlatformID.Win32NT && currentPath == "/")
            {
                return null;
            }
            
            // Special case for drive root on Windows
            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                (currentPath.Length == 3 && currentPath.EndsWith(":\\")))
            {
                return null; // Go back to drive selection
            }

            // Special case for /git root in Docker
            if (Environment.OSVersion.Platform != PlatformID.Win32NT && currentPath == "/git")
            {
                return null; // Go back to root selection
            }

            try
            {
                var parent = Directory.GetParent(currentPath);
                parentPath = parent?.FullName;
            }
            catch
            {
                parentPath = null;
            }

            _logger.LogDebug("Parent path for {CurrentPath}: {ParentPath}", currentPath, parentPath ?? "null");
            return parentPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting parent path for: {Path}", currentPath);
            return null;
        }
    }

    public (bool isValid, string? error) ValidateDirectoryPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return (false, "Directory path cannot be empty");
            }

            if (!Directory.Exists(path))
            {
                return (false, $"Directory not found: {path}");
            }

            // Test accessibility
            Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            
            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, $"Access denied to directory: {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating directory path: {Path}", path);
            return (false, $"Directory validation error: {ex.Message}");
        }
    }
}