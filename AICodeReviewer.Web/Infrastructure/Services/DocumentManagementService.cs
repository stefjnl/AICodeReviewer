using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for managing documents and coding standards
/// </summary>
public class DocumentManagementService : IDocumentManagementService
{
    private readonly ILogger<DocumentManagementService> _logger;
    private static readonly string[] SupportedExtensions = { ".md" };

    public DocumentManagementService(ILogger<DocumentManagementService> logger)
    {
        _logger = logger;
    }

    public (List<string> files, bool isError) ScanDocumentsFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Documents folder does not exist: {Path}", folderPath);
                return (new List<string>(), false); // Empty list, not error
            }
            
            var markdownFiles = Directory.GetFiles(folderPath, "*.md")
                                       .Select(f => Path.GetFileNameWithoutExtension(f)!)
                                       .OrderBy(name => name)
                                       .ToList();
                                       
            _logger.LogInformation("Scanned documents folder: {Path}, found {Count} files", folderPath, markdownFiles.Count);
            return (markdownFiles, false);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access to documents folder: {Path}", folderPath);
            return (new List<string>(), true); // Return empty list with error flag
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning documents folder: {Path}", folderPath);
            return (new List<string>(), true); // Return empty list with error flag
        }
    }

    public (string content, bool isError) LoadDocument(string fileName, string folderPath)
    {
        var fullPath = Path.Combine(folderPath, fileName + ".md");
        
        try
        {
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Document not found: {FileName} in folder: {Path}", fileName, folderPath);
                return ($"Document not found: {fileName}", true);
            }
                
            var content = File.ReadAllText(fullPath);
            _logger.LogInformation("Loaded document: {FileName} from: {Path}, size: {Size} bytes", fileName, folderPath, content.Length);
            return (content, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading document: {FileName} from: {Path}", fileName, folderPath);
            return ($"Error reading document: {ex.Message}", true);
        }
    }

    public async Task<(string content, bool isError)> LoadDocumentAsync(string fileName, string folderPath)
    {
        var fullPath = Path.Combine(folderPath, fileName + ".md");
        
        try
        {
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Document not found: {FileName} in folder: {Path}", fileName, folderPath);
                return ($"Document not found: {fileName}", true);
            }
                
            var content = await File.ReadAllTextAsync(fullPath);
            _logger.LogInformation("Loaded document asynchronously: {FileName} from: {Path}, size: {Size} bytes", fileName, folderPath, content.Length);
            return (content, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading document asynchronously: {FileName} from: {Path}", fileName, folderPath);
            return ($"Error reading document: {ex.Message}", true);
        }
    }

    public string GetDocumentDisplayName(string fileName)
    {
        // Convert filename to friendly display name
        var displayName = fileName.Replace("-", " ").Replace("_", " ");
        _logger.LogDebug("Converted filename '{FileName}' to display name '{DisplayName}'", fileName, displayName);
        return displayName;
    }

    public (bool isValid, string? normalizedPath, string? error) ValidateDocumentsFolder(string folderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _logger.LogWarning("Empty documents folder path provided");
                return (false, null, "Documents folder path cannot be empty");
            }

            // Normalize the path
            string normalizedPath = folderPath;
            
            // Check if path exists
            if (!Directory.Exists(normalizedPath))
            {
                _logger.LogWarning("Documents folder does not exist: {Path}", normalizedPath);
                // Don't treat as error - will show empty list
                return (true, normalizedPath, null);
            }

            // Check if path is accessible
            try
            {
                Directory.GetFiles(normalizedPath, "*.md", SearchOption.TopDirectoryOnly);
                _logger.LogInformation("Validated documents folder: {Path}", normalizedPath);
                return (true, normalizedPath, null);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Access denied to documents folder: {Path}", normalizedPath);
                return (false, normalizedPath, "Access denied to documents folder");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing documents folder: {Path}", normalizedPath);
                return (false, normalizedPath, $"Error accessing documents folder: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating documents folder: {Path}", folderPath);
            return (false, null, $"Documents folder validation error: {ex.Message}");
        }
    }
}