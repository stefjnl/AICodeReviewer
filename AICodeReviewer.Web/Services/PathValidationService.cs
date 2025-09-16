using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Services;

public class PathValidationService : IPathValidationService
{
    private readonly ILogger<PathValidationService> _logger;

    public PathValidationService(ILogger<PathValidationService> logger)
    {
        _logger = logger;
    }

    public (string resolvedPath, string? error) ResolveAndValidateFilePath(string filePath, string repositoryPath, string contentRootPath)
    {
        _logger.LogInformation($"[PathValidation] Validating file path: {filePath}");
        _logger.LogInformation($"[PathValidation] Repository path: {repositoryPath}");
        _logger.LogInformation($"[PathValidation] Content root path: {contentRootPath}");

        var originalFilePath = filePath;

        // Check if it's just a filename (no path)
        if (!filePath.Contains(Path.DirectorySeparatorChar) && !filePath.Contains(Path.AltDirectorySeparatorChar))
        {
            _logger.LogWarning($"[PathValidation] File path appears to be just a filename: {filePath}");

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
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[PathValidation] Could not enumerate subdirectories: {ex.Message}");
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
                            _logger.LogInformation($"[PathValidation] File found at: {filePath}");
                            break;
                        }

                        // Also try with common extensions if not provided
                        if (!Path.HasExtension(filePath))
                        {
                            foreach (var ext in new[] { ".cs", ".js", ".py" })
                            {
                                var filesWithExt = Directory.GetFiles(searchPath, filePath + ext, SearchOption.TopDirectoryOnly);
                                if (filesWithExt.Length > 0)
                                {
                                    filePath = filesWithExt[0];
                                    fileFound = true;
                                    _logger.LogInformation($"[PathValidation] File found with extension at: {filePath}");
                                    break;
                                }
                            }
                            if (fileFound) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"[PathValidation] Error searching in {searchPath}: {ex.Message}");
                    }
                }
            }

            if (!fileFound)
            {
                _logger.LogWarning($"[PathValidation] File not found in search paths");
                var errorMsg = $"File '{originalFilePath}' not found. ";
                errorMsg += "Please provide the full file path (e.g., C:\\path\\to\\file.py or /path/to/file.py) or ";
                errorMsg += "ensure the file is in the repository directory: " + repositoryPath;
                return (string.Empty, errorMsg);
            }
        }
        else
        {
            // User provided a path, check if it's relative and resolve it
            if (!Path.IsPathRooted(filePath))
            {
                _logger.LogInformation($"[PathValidation] Relative path detected, resolving: {filePath}");

                // Try resolving relative to repository path first
                var relativePath = Path.Combine(repositoryPath, filePath);
                if (File.Exists(relativePath))
                {
                    filePath = relativePath;
                    _logger.LogInformation($"[PathValidation] Resolved relative path to: {filePath}");
                }
                else
                {
                    // Try relative to content root
                    var contentRelativePath = Path.Combine(contentRootPath, filePath);
                    if (File.Exists(contentRelativePath))
                    {
                        filePath = contentRelativePath;
                        _logger.LogInformation($"[PathValidation] Resolved relative to content root: {filePath}");
                    }
                }
            }
        }

        // Final validation - check if file exists and is readable
        if (!File.Exists(filePath))
        {
            _logger.LogError($"[PathValidation] File not found after all resolution attempts: {filePath}");
            var finalError = $"File not found: {originalFilePath}. ";
            finalError += "Please verify the file path is correct and the file exists. ";
            finalError += "If using a relative path, ensure it's relative to the repository root: " + repositoryPath;
            return (string.Empty, finalError);
        }

        _logger.LogInformation($"[PathValidation] File validation passed for: {filePath}");
        return (filePath, null);
    }

    public bool IsValidFileExtension(string filePath, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return allowedExtensions.Contains(extension);
    }
}