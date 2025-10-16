using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for validating and resolving file paths
/// </summary>
public class PathValidationService : IPathValidationService
{
    private readonly ILogger<PathValidationService> _logger;
    private static readonly string[] SupportedExtensions = { ".cs", ".js", ".py" };

    public PathValidationService(ILogger<PathValidationService> logger)
    {
        _logger = logger;
    }

    public (string normalizedPath, bool isValid, string? error) NormalizePath(string path, string basePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("Empty path provided");
                return (path, false, "Path cannot be empty");
            }

            // Handle relative paths
            string normalizedPath = path;
            if (!Path.IsPathRooted(path))
            {
                normalizedPath = Path.Combine(basePath, path);
                _logger.LogInformation("Resolved relative path: {OriginalPath} to: {NormalizedPath}", path, normalizedPath);
            }
            else
            {
                _logger.LogInformation("Absolute path provided: {Path}", path);
            }

            return (normalizedPath, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing path: {Path} with base: {BasePath}", path, basePath);
            return (path, false, $"Path normalization error: {ex.Message}");
        }
    }

    public (string resolvedPath, bool isValid, string? error) ValidateSingleFilePath(
        string filePath,
        string repositoryPath,
        string contentRootPath)
    {
        try
        {
            _logger.LogInformation("Validating single file path: {FilePath} in repository: {RepositoryPath}", filePath, repositoryPath);

            var originalFilePath = filePath;

            // Check if it's just a filename (no path)
            if (!filePath.Contains(Path.DirectorySeparatorChar) && !filePath.Contains(Path.AltDirectorySeparatorChar))
            {
                _logger.LogInformation("File path appears to be just a filename: {FileName}", filePath);

                // Try to find the file in common locations
                var searchPaths = new List<string>();

                // Add repository path and its subdirectories
                if (Directory.Exists(repositoryPath))
                {
                    searchPaths.Add(repositoryPath);
                    try
                    {
                        // Add first level of subdirectories
                        var subDirs = Directory.GetDirectories(repositoryPath, "*", SearchOption.TopDirectoryOnly);
                        searchPaths.AddRange(subDirs);
                        _logger.LogInformation("Added {Count} subdirectories to search path", subDirs.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not enumerate subdirectories in repository: {Path} - {Message}", repositoryPath, ex.Message);
                    }
                }

                // Add other common paths
                searchPaths.Add(contentRootPath);
                searchPaths.Add(Directory.GetCurrentDirectory());

                // Search for the file
                bool fileFound = false;
                foreach (var searchPath in searchPaths)
                {
                    if (Directory.Exists(searchPath))
                    {
                        try
                        {
                            var files = Directory.GetFiles(searchPath, filePath, SearchOption.TopDirectoryOnly);
                            if (files.Length > 0)
                            {
                                filePath = files[0];
                                fileFound = true;
                                _logger.LogInformation("File found at: {ResolvedPath}", filePath);
                                break;
                            }

                            // Also try with common extensions if not provided
                            if (!Path.HasExtension(filePath))
                            {
                                foreach (var ext in SupportedExtensions)
                                {
                                    var filesWithExt = Directory.GetFiles(searchPath, filePath + ext, SearchOption.TopDirectoryOnly);
                                    if (filesWithExt.Length > 0)
                                    {
                                        filePath = filesWithExt[0];
                                        fileFound = true;
                                        _logger.LogInformation("File found with extension at: {ResolvedPath}", filePath);
                                        break;
                                    }
                                }
                                if (fileFound) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Error searching in {SearchPath}: {Message}", searchPath, ex.Message);
                        }
                    }
                }

                if (!fileFound)
                {
                    _logger.LogWarning("File not found in search paths. Original filename: {OriginalFile}", originalFilePath);
                    var errorMsg = $"File '{originalFilePath}' not found. ";
                    errorMsg += "Please provide the full file path (e.g., C:\\path\\to\\file.py or /path/to/file.py) or ";
                    errorMsg += "ensure the file is in the repository directory: " + repositoryPath;
                    return (originalFilePath, false, errorMsg);
                }
            }
            else
            {
                // User provided a path, check if it's relative and resolve it
                if (!Path.IsPathRooted(filePath))
                {
                    _logger.LogInformation("Relative path detected, resolving: {RelativePath}", filePath);

                    // Try resolving relative to repository path first
                    var relativePath = Path.Combine(repositoryPath, filePath);
                    if (File.Exists(relativePath))
                    {
                        filePath = relativePath;
                        _logger.LogInformation("Resolved relative path to: {ResolvedPath}", filePath);
                    }
                    else
                    {
                        // Try relative to content root
                        var contentRelativePath = Path.Combine(contentRootPath, filePath);
                        if (File.Exists(contentRelativePath))
                        {
                            filePath = contentRelativePath;
                            _logger.LogInformation("Resolved relative to content root: {ResolvedPath}", filePath);
                        }
                    }
                }
            }

            // Final validation - check if file exists and is readable
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found after all resolution attempts: {FinalPath}", filePath);
                var finalError = $"File not found: {originalFilePath}. ";
                finalError += "Please verify the file path is correct and the file exists. ";
                finalError += "If using a relative path, ensure it's relative to the repository root: " + repositoryPath;
                return (originalFilePath, false, finalError);
            }

            // Validate file extension
            var (isSupported, extensionError) = ValidateFileExtension(filePath);
            if (!isSupported)
            {
                return (filePath, false, extensionError);
            }

            _logger.LogInformation("File validation passed for: {FilePath}", filePath);
            return (filePath, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating single file path: {FilePath}", filePath);
            return (filePath, false, $"File path validation error: {ex.Message}");
        }
    }

    public (bool isSupported, string? error) ValidateFileExtension(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLower();
            if (!SupportedExtensions.Contains(extension))
            {
                var error = $"Unsupported file type '{extension}'. Allowed extensions: {string.Join(", ", SupportedExtensions)}";
                _logger.LogWarning("Unsupported file extension: {Extension} for file: {FilePath}", extension, filePath);
                return (false, error);
            }

            _logger.LogInformation("File extension validated: {Extension} for file: {FilePath}", extension, filePath);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file extension for: {FilePath}", filePath);
            return (false, $"File extension validation error: {ex.Message}");
        }
    }

    public (bool isValid, string? error) ValidateDirectoryExists(string directoryPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                _logger.LogWarning("Empty directory path provided");
                return (false, "Directory path cannot be empty");
            }

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory does not exist: {Path}", directoryPath);
                return (false, $"Directory not found: {directoryPath}");
            }

            // Test accessibility
            Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);

            _logger.LogInformation("Directory validated: {Path}", directoryPath);
            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access denied to directory: {Path}", directoryPath);
            return (false, $"Access denied to directory: {directoryPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating directory: {Path}", directoryPath);
            return (false, $"Directory validation error: {ex.Message}");
        }
    }

    public string GetDocumentsFolderPath(string contentRootPath)
    {
        try
        {
            Console.WriteLine($"[DEBUG] GetDocumentsFolderPath called with: '{contentRootPath}'");
            Console.WriteLine($"[DEBUG] contentRootPath is null or empty: {string.IsNullOrEmpty(contentRootPath)}");

            var basePath = string.IsNullOrEmpty(contentRootPath)
                ? Directory.GetCurrentDirectory()
                : contentRootPath;

            if (string.IsNullOrEmpty(contentRootPath))
            {
                Console.WriteLine($"[DEBUG] contentRootPath missing, falling back to current directory: '{basePath}'");
            }

            // In production/Docker, Documents is directly in the app folder
            var documentsFolder = Path.Combine(basePath, "Documents");
            Console.WriteLine($"[DEBUG] Combined path result: '{documentsFolder}'");
            Console.WriteLine($"[DEBUG] Directory.Exists: {Directory.Exists(documentsFolder)}");

            // Check if this path exists
            if (Directory.Exists(documentsFolder))
            {
                var files = Directory.GetFiles(documentsFolder, "*.md");
                Console.WriteLine($"[DEBUG] Found {files.Length} .md files in {documentsFolder}");
                foreach (var file in files)
                {
                    Console.WriteLine($"[DEBUG]   - {file}");
                }
                _logger.LogInformation("Found documents folder at: {Path}", documentsFolder);
                return documentsFolder;
            }

            Console.WriteLine($"[DEBUG] Documents folder not found at {documentsFolder}, trying parent");

            // Fallback: Try parent directory (for local development)
            var solutionRoot = Directory.GetParent(basePath)?.FullName;
            Console.WriteLine($"[DEBUG] solutionRoot = {solutionRoot}");

            if (!string.IsNullOrEmpty(solutionRoot))
            {
                var parentDocuments = Path.Combine(solutionRoot, "Documents");
                Console.WriteLine($"[DEBUG] parentDocuments = {parentDocuments}");
                Console.WriteLine($"[DEBUG] Directory.Exists(parentDocuments) = {Directory.Exists(parentDocuments)}");

                if (Directory.Exists(parentDocuments))
                {
                    _logger.LogInformation("Found documents folder in parent at: {Path}", parentDocuments);
                    return parentDocuments;
                }
            }

            // Default to app folder
            Console.WriteLine($"[DEBUG] Returning default: {documentsFolder}");
            _logger.LogWarning("Documents folder not found, using default: {Path}", documentsFolder);
            return documentsFolder;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception: {ex.Message}");
            _logger.LogError(ex, "Error resolving documents folder path");
            var fallbackBase = string.IsNullOrEmpty(contentRootPath) ? Directory.GetCurrentDirectory() : contentRootPath;
            return Path.Combine(fallbackBase, "Documents");
        }
    }
}