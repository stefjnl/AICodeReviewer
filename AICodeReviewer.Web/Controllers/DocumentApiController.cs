using Microsoft.AspNetCore.Mvc;

namespace AICodeReviewer.Web.Controllers;

/// <summary>
/// API controller for document-related operations
/// Provides endpoints for scanning and retrieving available documents
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentApiController : ControllerBase
{
    private readonly ILogger<DocumentApiController> _logger;
    private readonly IConfiguration _configuration;

    public DocumentApiController(ILogger<DocumentApiController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Scans the documents folder for available markdown files
    /// </summary>
    /// <returns>JSON response containing list of available documents</returns>
    [HttpGet("scan")]
    public IActionResult ScanDocuments()
    {
        try
        {
            // Get documents folder path from configuration
            var documentsFolder = _configuration["Documents:FolderPath"] ?? "Documents";
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "..", documentsFolder);

            _logger.LogInformation("Scanning documents folder: {FolderPath}", fullPath);

            // Use existing DocumentService to scan for documents
            var (files, isError) = DocumentService.ScanDocumentsFolder(fullPath);

            if (isError)
            {
                _logger.LogWarning("Error occurred while scanning documents folder");
                return StatusCode(500, new
                {
                    success = false,
                    documents = new List<string>(),
                    error = "An error occurred while scanning the documents folder"
                });
            }

            if (!files.Any())
            {
                _logger.LogInformation("No documents found in folder: {FolderPath}", fullPath);
                return Ok(new
                {
                    success = true,
                    documents = new List<string>(),
                    error = (string?)null
                });
            }

            _logger.LogInformation("Found {DocumentCount} documents in folder", files.Count);
            return Ok(new
            {
                success = true,
                documents = files,
                error = (string?)null
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning documents folder");
            return StatusCode(500, new
            {
                success = false,
                documents = new List<string>(),
                error = "An unexpected error occurred while scanning documents"
            });
        }
    }

    /// <summary>
    /// Gets the content of a specific document
    /// </summary>
    /// <param name="documentName">Name of the document (without extension)</param>
    /// <returns>JSON response containing document content</returns>
    [HttpGet("content/{documentName}")]
    public IActionResult GetDocumentContent(string documentName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(documentName))
            {
                return BadRequest(new
                {
                    success = false,
                    content = string.Empty,
                    error = "Document name is required"
                });
            }

            // Get documents folder path from configuration
            var documentsFolder = _configuration["Documents:FolderPath"] ?? "Documents";
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "..", documentsFolder);

            _logger.LogInformation("Loading document content: {DocumentName}", documentName);

            // Use existing DocumentService to load document content
            var (content, isError) = DocumentService.LoadDocument(documentName, fullPath);

            if (isError)
            {
                _logger.LogWarning("Error loading document: {DocumentName}, Error: {Error}", documentName, content);
                return NotFound(new
                {
                    success = false,
                    content = string.Empty,
                    error = content
                });
            }

            _logger.LogInformation("Successfully loaded document: {DocumentName}", documentName);
            return Ok(new
            {
                success = true,
                content,
                error = (string?)null
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading document: {DocumentName}", documentName);
            return StatusCode(500, new
            {
                success = false,
                content = string.Empty,
                error = "An unexpected error occurred while loading the document"
            });
        }
    }
}