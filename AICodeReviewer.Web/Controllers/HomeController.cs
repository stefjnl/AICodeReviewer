using AICodeReviewer.Web.Models;
using AICodeReviewer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using AICodeReviewer.Web.Hubs;
using LibGit2Sharp;

namespace AICodeReviewer.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly string _defaultDocumentsPath;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly IPathValidationService _pathValidationService;
    private readonly IAnalysisOrchestrationService _analysisOrchestrationService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IDirectoryBrowserService _directoryBrowserService;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment,
        IConfiguration configuration, IMemoryCache cache, IHubContext<ProgressHub> hubContext,
        IPathValidationService pathValidationService, IAnalysisOrchestrationService analysisOrchestrationService,
        ISignalRNotificationService signalRNotificationService, IDirectoryBrowserService directoryBrowserService)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        // Documents folder is in the root of the application, not in the Web project
        _defaultDocumentsPath = Path.Combine(_environment.ContentRootPath, "..", "Documents");
        _cache = cache;
        _hubContext = hubContext;
        _pathValidationService = pathValidationService;
        _analysisOrchestrationService = analysisOrchestrationService;
        _signalRNotificationService = signalRNotificationService;
        _directoryBrowserService = directoryBrowserService;
    }

    public IActionResult Index()
    {
        // Git repository detection (existing)
        var (branchInfo, isError) = GitHelper.DetectRepository(_environment.ContentRootPath);
        ViewBag.BranchInfo = branchInfo;
        ViewBag.IsError = isError;

        // Repository path management for Git diff extraction
        // Default to root directory (AICodeReviewer) instead of AICodeReviewer.Web
        var defaultRepositoryPath = Path.Combine(_environment.ContentRootPath, "..");
        
        // If no session path is set, use the default (root) directory
        var sessionPath = HttpContext.Session.GetString("RepositoryPath");
        var repositoryPath = sessionPath ?? defaultRepositoryPath;
        
        // Ensure the path is set in session for consistency
        if (string.IsNullOrEmpty(sessionPath))
        {
            HttpContext.Session.SetString("RepositoryPath", repositoryPath);
        }
        
        ViewBag.RepositoryPath = repositoryPath;
        
        _logger.LogInformation($"Index method - Default path: {defaultRepositoryPath}");
        _logger.LogInformation($"Index method - Session path: {sessionPath}");
        _logger.LogInformation($"Index method - Final path: {repositoryPath}");
        
        // Also log if this is a valid git repository and get branch info
        string currentBranchInfo = "Unknown";
        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                _logger.LogInformation($"Index method - Git repository detected at: {repositoryPath}");
                _logger.LogInformation($"Index method - Current branch: {repo.Head.FriendlyName}");
                currentBranchInfo = repo.Head.FriendlyName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Index method - Not a valid git repository at: {repositoryPath}, Error: {ex.Message}");
            currentBranchInfo = "Not a git repository";
        }
        
        ViewBag.BranchInfo = currentBranchInfo;

        // Extract Git diff if repository path is set
        var (gitDiff, gitError) = GitService.ExtractDiff(repositoryPath);
        ViewBag.GitDiff = gitDiff;
        ViewBag.GitDiffError = gitError;

        // Document management
        var documentsFolder = HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
        ViewBag.DocumentsFolder = documentsFolder;

        var (files, scanError) = DocumentService.ScanDocumentsFolder(documentsFolder);
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

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SetRepositoryPath(string repositoryPath)
    {
        // Validate and normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(repositoryPath)
            ? _environment.ContentRootPath
            : Path.IsPathRooted(repositoryPath)
                ? repositoryPath
                : Path.Combine(_environment.ContentRootPath, repositoryPath);

        HttpContext.Session.SetString("RepositoryPath", normalizedPath);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult SetDocumentsFolder(string folderPath)
    {
        // Validate and normalize path
        string normalizedPath = string.IsNullOrWhiteSpace(folderPath)
            ? _defaultDocumentsPath
            : Path.IsPathRooted(folderPath)
                ? folderPath
                : Path.Combine(_environment.ContentRootPath, folderPath);

        HttpContext.Session.SetString("DocumentsFolder", normalizedPath);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult ScanDocumentsFolder([FromBody] ScanDocumentsRequest request)
    {
        try
        {
            string folderPath = request.FolderPath ?? HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            
            // Validate and normalize path
            string normalizedPath = string.IsNullOrWhiteSpace(folderPath)
                ? _defaultDocumentsPath
                : Path.IsPathRooted(folderPath)
                    ? folderPath
                    : Path.Combine(_environment.ContentRootPath, folderPath);

            var (files, isError) = DocumentService.ScanDocumentsFolder(normalizedPath);
            
            return Json(new
            {
                success = !isError,
                documents = files,
                folderPath = normalizedPath,
                error = isError ? "Unable to scan documents folder" : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning documents folder");
            return Json(new
            {
                success = false,
                documents = new List<string>(),
                error = $"Error scanning folder: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public IActionResult SelectDocuments(string[] selectedDocuments)
    {
        var selections = selectedDocuments?.ToList() ?? new List<string>();
        HttpContext.Session.SetObject("SelectedDocuments", selections);
        return RedirectToAction("Index");
    }


    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RunAnalysis([FromBody] RunAnalysisRequest request)
    {
        try
        {
            // Null check for request parameter
            if (request == null)
            {
                return BadRequest("Invalid request");
            }

            // Use request data or fall back to session data
            // Default to parent directory (AICodeReviewer) instead of AICodeReviewer.Web
            var defaultRepositoryPath = Path.Combine(_environment.ContentRootPath, "..");
            var repositoryPath = request.RepositoryPath ?? HttpContext.Session.GetString("RepositoryPath") ?? defaultRepositoryPath;
            var selectedDocuments = request.SelectedDocuments ?? HttpContext.Session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
            var documentsFolder = !string.IsNullOrEmpty(request.DocumentsFolder) ? request.DocumentsFolder : HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            var language = request.Language ?? HttpContext.Session.GetString("Language") ?? "NET";
            var analysisType = request.AnalysisType ?? "uncommitted";
            var commitId = request.CommitId;
            var filePath = request.FilePath;
            
            // DEBUG: Log document selection details
            _logger.LogInformation($"[RunAnalysis] Request SelectedDocuments: {(request.SelectedDocuments != null ? string.Join(", ", request.SelectedDocuments) : "null")}");
            _logger.LogInformation($"[RunAnalysis] Session SelectedDocuments: {string.Join(", ", HttpContext.Session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>())}");
            _logger.LogInformation($"[RunAnalysis] Final selectedDocuments count: {selectedDocuments.Count}");
            
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"];

            // Store language in session for consistency
            HttpContext.Session.SetString("Language", language);

            // Validate required fields
            if (string.IsNullOrEmpty(apiKey))
            {
                return Json(new { success = false, error = "API key not configured" });
            }

            if (selectedDocuments.Count == 0)
            {
                return Json(new { success = false, error = "No coding standards selected" });
            }

            // Validate based on analysis type
            if (analysisType == "singlefile")
            {
                _logger.LogInformation($"[RunAnalysis] Single file analysis requested with filePath: {filePath}");
                
                // Validate file path for single file analysis
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning($"[RunAnalysis] File path is missing for single file analysis");
                    return Json(new { success = false, error = "File path is required for single file analysis" });
                }

                // Use PathValidationService to resolve and validate file path
                var (resolvedPath, validationError) = _pathValidationService.ResolveAndValidateFilePath(
                    filePath, repositoryPath, _environment.ContentRootPath);
                
                if (!string.IsNullOrEmpty(validationError))
                {
                    return Json(new { success = false, error = validationError });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".cs", ".js", ".py" };
                if (!_pathValidationService.IsValidFileExtension(resolvedPath, allowedExtensions))
                {
                    var extension = Path.GetExtension(resolvedPath).ToLower();
                    return Json(new { success = false, error = $"Unsupported file type '{extension}'. Allowed extensions: {string.Join(", ", allowedExtensions)}" });
                }
                
                filePath = resolvedPath; // Use the resolved path
                _logger.LogInformation($"[RunAnalysis] File validation passed for: {filePath}");
            }
            else
            {
                // Validate git repository for git-based analysis
                var (branchInfo, isGitError) = GitHelper.DetectRepository(repositoryPath);
                if (isGitError || branchInfo == "No git repository found")
                {
                    return Json(new { success = false, error = "No valid git repository found at the specified path. Please select a valid git repository." });
                }

                // Validate commit ID if commit analysis requested
                if (analysisType == "commit")
                {
                    if (string.IsNullOrEmpty(commitId))
                    {
                        return Json(new { success = false, error = "Commit ID is required for commit analysis" });
                    }
                }
            }

            // Use AnalysisOrchestrationService to start the analysis
            var docsFolder = request.DocumentsFolder ?? HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            var analysisId = await _analysisOrchestrationService.StartAnalysisAsync(
                repositoryPath, selectedDocuments, docsFolder, apiKey, model, language,
                analysisType, commitId, filePath);

            return Json(new { success = true, analysisId = analysisId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAnalysisStatus(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId))
        {
            return Json(new { status = "NotStarted", result = (string?)null, error = (string?)null, isComplete = false });
        }

        var result = await _analysisOrchestrationService.GetAnalysisStatusAsync(analysisId);
        if (result != null)
        {
            _logger.LogInformation($"[Analysis {analysisId}] Serving result: Status={result.Status}, ResultLength={result.Result?.Length ?? 0}");
            var progressDto = new ProgressDto(result.Status, result.Result, result.Error, result.IsComplete);
            return Json(progressDto);
        }

        return Json(new { status = "NotFound", result = (string?)null, error = "Analysis not found or expired", isComplete = true });
    }




    [HttpPost]
    public IActionResult StoreAnalysisId([FromBody] StoreAnalysisIdRequest request)
    {
        if (string.IsNullOrEmpty(request?.AnalysisId))
        {
            return BadRequest(new { error = "Analysis ID is required" });
        }
        
        HttpContext.Session.SetString("AnalysisId", request.AnalysisId);
        return Json(new { success = true });
    }


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
            
            _logger.LogInformation($"Validating commit {request.CommitId} in repository: {repositoryPath}");
            
            using var repo = new Repository(repositoryPath);
            var commit = repo.Lookup<Commit>(request.CommitId);
            
            if (commit == null)
            {
                return Json(new { success = false, error = $"Commit '{request.CommitId}' not found" });
            }
            
            var message = $"{commit.Sha.Substring(0, 7)} - {commit.MessageShort}";
            return Json(new { success = true, message = message });
        }
        catch (RepositoryNotFoundException)
        {
            _logger.LogError($"Repository not found at path: {request.RepositoryPath ?? HttpContext.Session.GetString("RepositoryPath") ?? Path.Combine(_environment.ContentRootPath, "..")}");
            return Json(new { success = false, error = "Not a git repository" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating commit {request.CommitId}");
            return Json(new { success = false, error = $"Validation error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> BrowseDirectory([FromBody] DirectoryBrowseRequest request)
    {
        try
        {
            var response = await _directoryBrowserService.BrowseDirectoryAsync(request?.CurrentPath);
            return Json(response);
        }
        catch (Exception ex)
        {
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
