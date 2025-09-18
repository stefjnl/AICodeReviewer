using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Web.Domain.Interfaces;

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
    private readonly IDocumentManagementService _documentService;
    private readonly IWebHostEnvironment _environment;
    
public DocumentApiController(ILogger<DocumentApiController> logger, IDocumentManagementService documentService, IWebHostEnvironment environment)
{
    _logger = logger;
    _documentService = documentService;
    _environment = environment;
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
            _logger.LogInformation("Scanning documents via API");
            
            // Use default documents folder if none provided
            var defaultDocumentsFolder = Path.Combine(_environment.ContentRootPath, "..", "Documents");
            var documentsFolder = defaultDocumentsFolder;

            // Use document service to scan for documents
            var (files, isError) = _documentService.ScanDocumentsFolder(documentsFolder);

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
                _logger.LogInformation("No documents found");
                return Ok(new
                {
                    success = true,
                    documents = new List<string>(),
                    error = (string?)null
                });
            }

            _logger.LogInformation("Found {DocumentCount} documents", files.Count);
            return Ok(new
            {
                success = true,
                documents = files,
                error = (string?)null
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning documents");
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

            _logger.LogInformation("Loading document content: {DocumentName}", documentName);
            
            // Use default documents folder if none provided
            var defaultDocumentsFolder = Path.Combine(_environment.ContentRootPath, "..", "Documents");
            var documentsFolder = defaultDocumentsFolder;

            // Use document service to load document content
            var (content, isError) = _documentService.LoadDocument(documentName, documentsFolder);

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