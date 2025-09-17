using AICodeReviewer.Web.Models;
using AICodeReviewer.Web.Services;
using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using AICodeReviewer.Web.Hubs;

namespace AICodeReviewer.Web.Controllers;

/// <summary>
/// Home controller for the AI Code Reviewer application.
/// This controller handles the main UI and delegates business logic to services.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<ProgressHub> _hubContext;
    
    // Domain services
    private readonly IAnalysisService _analysisService;
    private readonly IRepositoryManagementService _repositoryService;
    private readonly IDocumentManagementService _documentService;
    private readonly IPathValidationService _pathService;
    private readonly ISignalRBroadcastService _signalRService;
    private readonly IDirectoryBrowsingService _directoryService;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IMemoryCache cache,
        IHubContext<ProgressHub> hubContext,
        IAnalysisService analysisService,
        IRepositoryManagementService repositoryService,
        IDocumentManagementService documentService,
        IPathValidationService pathService,
        ISignalRBroadcastService signalRService,
        IDirectoryBrowsingService directoryService)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        _cache = cache;
        _hubContext = hubContext;
        
        // Domain services
        _analysisService = analysisService;
        _repositoryService = repositoryService;
        _documentService = documentService;
        _pathService = pathService;
        _signalRService = signalRService;
        _directoryService = directoryService;
    }

    /// <summary>
    /// Main page showing repository information, document selection, and analysis options
    /// </summary>
    public IActionResult Index()
    {
        try
        {
            // Git repository detection
            var (branchInfo, isError) = _repositoryService.DetectRepository(_environment.ContentRootPath);
            ViewBag.BranchInfo = branchInfo;
            ViewBag.IsError = isError;

            // Repository path management for Git diff extraction
            var defaultRepositoryPath = Path.Combine(_environment.ContentRootPath, "..");
            var repositoryPath = HttpContext.Session.GetString("RepositoryPath") ?? defaultRepositoryPath;
            ViewBag.RepositoryPath = repositoryPath;
            
            _logger.LogInformation("Index method - Default path: {DefaultPath}", defaultRepositoryPath);
            _logger.LogInformation("Index method - Session path: {SessionPath}", HttpContext.Session.GetString("RepositoryPath"));
            _logger.LogInformation("Index method - Final path: {FinalPath}", repositoryPath);

            // Extract Git diff if repository path is set
            var (gitDiff, gitError) = _repositoryService.ExtractDiff(repositoryPath);
            ViewBag.GitDiff = gitDiff;
            ViewBag.GitDiffError = gitError;

            // Document management
            var documentsFolder = HttpContext.Session.GetString("DocumentsFolder") ?? Path.Combine(_environment.ContentRootPath, "..", "Documents");
            ViewBag.DocumentsFolder = documentsFolder;

            var (files, scanError) = _documentService.ScanDocumentsFolder(documentsFolder);
            if (!scanError)
            {
                ViewBag.AvailableDocuments = files;
                ViewBag.DocumentScanError = null;
            }
            else
            {
                ViewBag.AvailableDocuments = new List<string>();
                ViewBag.DocumentScanError = "Unable to scan documents folder";
            }

            var selectedDocuments = HttpContext.Session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
            ViewBag.SelectedDocuments = selectedDocuments;

            // Load analysis results from cache if available
            var analysisId = HttpContext.Session.GetString("AnalysisId");
            if (!string.IsNullOrEmpty(analysisId) && _cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? cachedResult))
            {
                // Store the result in session for the view to display
                if (cachedResult != null)
                {
                    HttpContext.Session.SetString("AnalysisResult", cachedResult.Result ?? "");
                    HttpContext.Session.SetString("AnalysisError", cachedResult.Error ?? "");
                    // AnalysisId is already in session
                }
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Index page");
            ViewBag.Error = "Error loading page data";
            return View();
        }
    }

    /// <summary>
    /// Set repository path for analysis
    /// </summary>
    [HttpPost]
    public IActionResult SetRepositoryPath(string repositoryPath)
    {
        try
        {
            // Validate and normalize path
            string normalizedPath = string.IsNullOrWhiteSpace(repositoryPath)
                ? _environment.ContentRootPath
                : Path.IsPathRooted(repositoryPath)
                    ? repositoryPath
                    : Path.Combine(_environment.ContentRootPath, repositoryPath);

            HttpContext.Session.SetString("RepositoryPath", normalizedPath);
            _logger.LogInformation("Repository path set to: {Path}", normalizedPath);
            return Json(new { success = true, repositoryPath = normalizedPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting repository path");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Set documents folder path
    /// </summary>
    [HttpPost]
    public IActionResult SetDocumentsFolder(string folderPath)
    {
        try
        {
            // Validate and normalize path
            string normalizedPath = string.IsNullOrWhiteSpace(folderPath)
                ? Path.Combine(_environment.ContentRootPath, "..", "Documents")
                : Path.IsPathRooted(folderPath)
                    ? folderPath
                    : Path.Combine(_environment.ContentRootPath, folderPath);

            HttpContext.Session.SetString("DocumentsFolder", normalizedPath);
            _logger.LogInformation("Documents folder set to: {Path}", normalizedPath);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting documents folder");
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Select documents for analysis
    /// </summary>
    [HttpPost]
    public IActionResult SelectDocuments(string[] selectedDocuments)
    {
        try
        {
            var selections = selectedDocuments?.ToList() ?? new List<string>();
            HttpContext.Session.SetObject("SelectedDocuments", selections);
            
            var displayNames = selections.Select(doc => _documentService.GetDocumentDisplayName(doc)).ToList();
            _logger.LogInformation("Selected {Count} documents for analysis", selections.Count);
            
            return Json(new {
                success = true,
                selectedDocuments = selections,
                displayNames = displayNames,
                count = selections.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting documents");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Start code analysis with the selected parameters
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RunAnalysis([FromBody] RunAnalysisRequest request)
    {
        try
        {
            var (analysisId, success, error) = await _analysisService.StartAnalysisAsync(
                request, HttpContext.Session, _environment, _configuration);

            if (success)
            {
                _logger.LogInformation("Analysis started successfully with ID: {AnalysisId}", analysisId);
                return Json(new { success = true, analysisId = analysisId });
            }
            else
            {
                _logger.LogError("Failed to start analysis: {Error}", error);
                return Json(new { success = false, error = error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting analysis");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get current analysis status
    /// </summary>
    [HttpGet]
    public IActionResult GetAnalysisStatus(string analysisId)
    {
        try
        {
            var result = _analysisService.GetAnalysisStatus(analysisId);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis status for ID: {AnalysisId}", analysisId);
            return Json(new { status = "Error", result = (string?)null, error = ex.Message, isComplete = true });
        }
    }

    /// <summary>
    /// Store analysis ID in session for tracking
    /// </summary>
    [HttpPost]
    public IActionResult StoreAnalysisId([FromBody] StoreAnalysisIdRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.AnalysisId))
            {
                return BadRequest(new { error = "Analysis ID is required" });
            }
            
            _analysisService.StoreAnalysisId(request.AnalysisId, HttpContext.Session);
            _logger.LogInformation("Stored analysis ID: {AnalysisId}", request.AnalysisId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing analysis ID");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Validate a git commit exists
    /// </summary>
    [HttpPost]
    public IActionResult ValidateCommit([FromBody] ValidateCommitRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.CommitId))
            {
                return Json(new { success = false, error = "Commit ID is required" });
            }

            var defaultRepositoryPath = Path.Combine(_environment.ContentRootPath, "..");
            var repositoryPath = request.RepositoryPath ?? HttpContext.Session.GetString("RepositoryPath") ?? defaultRepositoryPath;
            
            _logger.LogInformation("Validating commit {CommitId} in repository: {RepositoryPath}", request.CommitId, repositoryPath);
            
            var (isValid, message, error) = _repositoryService.ValidateCommit(repositoryPath, request.CommitId);
            
            if (isValid)
            {
                return Json(new { success = true, message = message });
            }
            else
            {
                return Json(new { success = false, error = error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating commit {CommitId}", request.CommitId);
            return Json(new { success = false, error = $"Validation error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Browse directory contents for file selection
    /// </summary>
    [HttpPost]
    public IActionResult BrowseDirectory([FromBody] DirectoryBrowseRequest request)
    {
        try
        {
            string currentPath;
            
            // If no path provided, start with drives on Windows or root on Unix
            if (string.IsNullOrWhiteSpace(request?.CurrentPath))
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // On Windows, start with drive selection
                    var drives = _directoryService.GetRootDrives();
                    return Json(new DirectoryBrowseResponse
                    {
                        Directories = drives,
                        Files = new List<DirectoryItem>(),
                        CurrentPath = "Computer",
                        ParentPath = null,
                        IsGitRepository = false
                    });
                }
                else
                {
                    // On Unix systems, start with root
                    currentPath = "/";
                }
            }
            else
            {
                currentPath = request.CurrentPath;
            }

            // Validate and browse directory
            var (isValid, error) = _directoryService.ValidateDirectoryPath(currentPath);
            if (!isValid)
            {
                return Json(new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = currentPath,
                    ParentPath = null,
                    IsGitRepository = false,
                    Error = error
                });
            }

            var response = _directoryService.BrowseDirectory(currentPath);
            _logger.LogInformation("Browsed directory: {Path}, found {DirectoryCount} directories and {FileCount} files", 
                currentPath, response.Directories.Count, response.Files.Count);
            
            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing directory");
            return Json(new DirectoryBrowseResponse
            {
                Directories = new List<DirectoryItem>(),
                Files = new List<DirectoryItem>(),
                CurrentPath = request?.CurrentPath ?? string.Empty,
                ParentPath = null,
                IsGitRepository = false,
                Error = $"Unexpected error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Error page
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}