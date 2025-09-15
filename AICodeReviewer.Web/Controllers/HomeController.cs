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

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment, IConfiguration configuration, IMemoryCache cache, IHubContext<ProgressHub> hubContext)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        // Documents folder is in the root of the application, not in the Web project
        _defaultDocumentsPath = Path.Combine(_environment.ContentRootPath, "..", "Documents");
        _cache = cache;
        _hubContext = hubContext;
    }

    public IActionResult Index()
    {
        // Git repository detection (existing)
        var (branchInfo, isError) = GitHelper.DetectRepository(_environment.ContentRootPath);
        ViewBag.BranchInfo = branchInfo;
        ViewBag.IsError = isError;

        // Repository path management for Git diff extraction
        // Default to parent directory (AICodeReviewer) instead of AICodeReviewer.Web
        var defaultRepositoryPath = Path.Combine(_environment.ContentRootPath, "..");
        var repositoryPath = HttpContext.Session.GetString("RepositoryPath") ?? defaultRepositoryPath;
        ViewBag.RepositoryPath = repositoryPath;
        
        _logger.LogInformation($"Index method - Default path: {defaultRepositoryPath}");
        _logger.LogInformation($"Index method - Session path: {HttpContext.Session.GetString("RepositoryPath")}");
        _logger.LogInformation($"Index method - Final path: {repositoryPath}");

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
    public IActionResult SelectDocuments(string[] selectedDocuments)
    {
        var selections = selectedDocuments?.ToList() ?? new List<string>();
        HttpContext.Session.SetObject("SelectedDocuments", selections);
        return RedirectToAction("Index");
    }


    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult RunAnalysis([FromBody] RunAnalysisRequest request)
    {
        try
        {
            // Use request data or fall back to session data
            // Default to parent directory (AICodeReviewer) instead of AICodeReviewer.Web
            var defaultRepositoryPath = Path.Combine(_environment.ContentRootPath, "..");
            var repositoryPath = request.RepositoryPath ?? HttpContext.Session.GetString("RepositoryPath") ?? defaultRepositoryPath;
            var selectedDocuments = request.SelectedDocuments ?? HttpContext.Session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
            var documentsFolder = !string.IsNullOrEmpty(request.DocumentsFolder) ? request.DocumentsFolder : HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            var language = request.Language ?? HttpContext.Session.GetString("Language") ?? "NET";
            var analysisType = request.AnalysisType ?? "uncommitted";
            var commitId = request.CommitId;
            
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

            // Validate git repository
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
                
                // Additional validation can be added here if needed
            }

            // Generate unique analysis ID
            var analysisId = Guid.NewGuid().ToString();

            // Create initial analysis result
            var analysisResult = new AnalysisResult
            {
                Status = "Starting",
                CreatedAt = DateTime.UtcNow
            };

            // Store in memory cache with expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30)) // Reset on access
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(60)) // Max 1 hour
                .SetSize(1); // Size = 1 unit for size-limited cache

            _cache.Set($"analysis_{analysisId}", analysisResult, cacheOptions);

            // Start background analysis with captured references
            var docsFolder = request.DocumentsFolder ?? HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            _ = Task.Run(async () =>
            {
                try
                {
                    await RunBackgroundAnalysisWithCache(analysisId, repositoryPath, selectedDocuments, docsFolder, apiKey, model, language, analysisType, commitId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background analysis failed for analysis {AnalysisId}", analysisId);
                    await BroadcastError(analysisId, $"Background analysis error: {ex.Message}");
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    _logger.LogError(t.Exception, "Unobserved exception in background task for analysis {AnalysisId}", analysisId);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            return Json(new { success = true, analysisId = analysisId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetAnalysisStatus(string analysisId)
    {
        if (string.IsNullOrEmpty(analysisId))
        {
            return Json(new { status = "NotStarted", result = (string?)null, error = (string?)null, isComplete = false });
        }

        if (_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result))
        {
            _logger.LogInformation($"[Analysis {analysisId}] Serving result: Status={result.Status}, ResultLength={result.Result?.Length ?? 0}");
            var progressDto = new ProgressDto(result.Status, result.Result, result.Error, result.IsComplete);
            return Json(progressDto);
        }

        return Json(new { status = "NotFound", result = (string?)null, error = "Analysis not found or expired", isComplete = true });
    }

    private async Task RunBackgroundAnalysis(string repositoryPath, List<string> selectedDocuments, string apiKey, string model)
    {
        // This method is no longer used - replaced by RunBackgroundAnalysisWithCache
        // Kept for backward compatibility but will not be called
        await Task.CompletedTask;
    }

    private async Task RunBackgroundAnalysisWithCache(string analysisId, string repositoryPath, List<string> selectedDocuments, string documentsFolder, string apiKey, string model, string language, string analysisType = "uncommitted", string? commitId = null)
    {
        _logger.LogInformation($"[Analysis {analysisId}] Starting background analysis");
        _logger.LogInformation($"[Analysis {analysisId}] Repository path: {repositoryPath}");
        _logger.LogInformation($"[Analysis {analysisId}] Selected documents: {string.Join(", ", selectedDocuments)}");
        _logger.LogInformation($"[Analysis {analysisId}] Documents folder: {documentsFolder}");
        _logger.LogInformation($"[Analysis {analysisId}] Language: {language}");
        _logger.LogInformation($"[Analysis {analysisId}] API key configured: {!string.IsNullOrEmpty(apiKey)}");
        _logger.LogInformation($"[Analysis {analysisId}] Model: {model}");

        try
        {
            // Get current result from cache
            _logger.LogInformation($"[Analysis {analysisId}] Checking cache for existing result");
            AnalysisResult? result;
            try
            {
                if (!_cache.TryGetValue($"analysis_{analysisId}", out result))
                {
                    _logger.LogError($"[Analysis {analysisId}] Analysis not found in cache - aborting");
                    return; // Analysis not found in cache
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Error accessing cache - aborting");
                return;
            }
            _logger.LogInformation($"[Analysis {analysisId}] Found existing result in cache");

            // Update status via SignalR
            if (result != null)
            {
                result.Status = "Reading git changes...";
                await BroadcastProgress(analysisId, "Reading git changes...");
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Reading git changes...'");
            }

            // Get git diff based on analysis type
            _logger.LogInformation($"[Analysis {analysisId}] Analysis type: {analysisType}");
            string gitDiff;
            bool gitError;
            
            if (analysisType == "commit" && !string.IsNullOrEmpty(commitId))
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting commit diff for commit: {commitId}");
                (gitDiff, gitError) = GitService.GetCommitDiff(repositoryPath, commitId);
            }
            else
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting uncommitted changes");
                (gitDiff, gitError) = GitService.ExtractDiff(repositoryPath);
            }
            
            _logger.LogInformation($"[Analysis {analysisId}] Git diff extraction complete - Error: {gitError}, Diff length: {gitDiff?.Length ?? 0}");
            
            // Store git diff in cache for results display
            if (!gitError && !string.IsNullOrEmpty(gitDiff))
            {
                _cache.Set($"diff_{analysisId}", gitDiff, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1));
                _logger.LogInformation($"[Analysis {analysisId}] Stored git diff in cache ({gitDiff.Length} bytes)");
            }
            
            if (gitError)
            {
                _logger.LogError($"[Analysis {analysisId}] Git diff extraction failed: {gitDiff}");
                if (result != null)
                {
                    result.Status = "Error";
                    result.Error = $"Git diff error: {gitDiff}";
                    result.CompletedAt = DateTime.UtcNow;
                    
                    var cacheOptions2 = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                        .SetSize(1);
                    _cache.Set($"analysis_{analysisId}", result, cacheOptions2);
                    _logger.LogInformation($"[Analysis {analysisId}] Set error status and completed");
                }
                return;
            }
            _logger.LogInformation($"[Analysis {analysisId}] Git diff extracted successfully");

            // Update status via SignalR
            _logger.LogInformation($"[Analysis {analysisId}] Starting document loading phase");
            if (result != null)
            {
                result.Status = "Loading documents...";
                await BroadcastProgress(analysisId, "Loading documents...");
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Loading documents...'");
            }
            
            // Load selected documents
            _logger.LogInformation($"[Analysis {analysisId}] Loading {selectedDocuments.Count} selected documents");
            var codingStandards = new List<string>();
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder path: {documentsFolder}");
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder exists: {Directory.Exists(documentsFolder)}");
            
            foreach (var docName in selectedDocuments)
            {
                _logger.LogInformation($"[Analysis {analysisId}] Loading document: {docName} from folder: {documentsFolder}");
                var (content, docError) = DocumentService.LoadDocument(docName, documentsFolder);
                _logger.LogInformation($"[Analysis {analysisId}] Document {docName} - Error: {docError}, Content length: {content?.Length ?? 0}");
                if (!docError)
                {
                    codingStandards.Add(content);
                    _logger.LogInformation($"[Analysis {analysisId}] Document {docName} loaded successfully");
                }
                else
                {
                    _logger.LogWarning($"[Analysis {analysisId}] Failed to load document {docName}: {content}");
                }
            }
            _logger.LogInformation($"[Analysis {analysisId}] Document loading complete - loaded {codingStandards.Count} documents");
            _logger.LogInformation($"[Analysis {analysisId}] Final codingStandards count: {codingStandards.Count}");

            // Update status via SignalR
            _logger.LogInformation($"[Analysis {analysisId}] Starting AI analysis phase");
            if (result != null)
            {
                result.Status = "AI analysis...";
                await BroadcastProgress(analysisId, "AI analysis...");
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'AI analysis...'");
            }
            
            // Get requirements
            string requirements = "Follow .NET best practices and coding standards";
            _logger.LogInformation($"[Analysis {analysisId}] Requirements: {requirements}");

            // Call AI service with timeout protection
            _logger.LogInformation($"[Analysis {analysisId}] Calling AI service with:");
            _logger.LogInformation($"[Analysis {analysisId}] - Git diff length: {gitDiff?.Length ?? 0}");
            _logger.LogInformation($"[Analysis {analysisId}] - Coding standards count: {codingStandards.Count}");
            _logger.LogInformation($"[Analysis {analysisId}] - API key configured: {!string.IsNullOrEmpty(apiKey)}");
            _logger.LogInformation($"[Analysis {analysisId}] - Model: {model}");
            
            // Add timeout protection (60 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            
            string analysis;
            bool aiError;
            string? errorMessage;
            
            try
            {
                _logger.LogInformation($"[Analysis {analysisId}] Starting AI service call with 60-second timeout");
                
                // Call AI service with timeout
                var (analysisResult, isError, errorMsg) = await Task.Run(async () =>
                    await AIService.AnalyzeCodeAsync(gitDiff, codingStandards, requirements, apiKey, model, language),
                    cts.Token);
                
                analysis = analysisResult;
                aiError = isError;
                errorMessage = errorMsg;
                
                _logger.LogInformation($"[Analysis {analysisId}] AI service call complete - Error: {aiError}, Analysis length: {analysis?.Length ?? 0}");
                if (aiError)
                {
                    _logger.LogError($"[Analysis {analysisId}] AI analysis failed with detailed error: {errorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError($"[Analysis {analysisId}] AI analysis timed out after 60 seconds");
                analysis = "";
                aiError = true;
                errorMessage = "AI analysis timed out after 60 seconds";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Analysis {analysisId}] Unexpected exception during AI service call");
                analysis = "";
                aiError = true;
                errorMessage = $"Unexpected error calling AI service: {ex.Message}";
            }

            // Always create a NEW instance to ensure we're writing fresh data
            var finalResult = new AnalysisResult
            {
                Status = aiError ? "Error" : "Complete",
                Result = aiError ? null : analysis,
                Error = aiError ? $"AI analysis failed: {errorMessage}" : null,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = result.CreatedAt, // preserve original creation time
                RequestId = result.RequestId  // preserve if you have it
            };

            // Broadcast completion via SignalR
            if (aiError)
            {
                await BroadcastError(analysisId, $"AI analysis failed: {errorMessage}");
            }
            else
            {
                await BroadcastComplete(analysisId, analysis);
            }

            // Still update cache for fallback
            _cache.Set($"analysis_{analysisId}", finalResult, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetSize(1));

            _logger.LogInformation($"[Analysis {analysisId}] Cache updated with final result: {finalResult.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Analysis {analysisId}] Unhandled exception in background analysis");
            try
            {
                if (_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? errorResult))
                {
                    var finalErrorResult = new AnalysisResult {
                        Status = "Error",
                        Error = $"Analysis error: {ex.Message}",
                        CompletedAt = DateTime.UtcNow
                    };
                    _cache.Set($"analysis_{analysisId}", finalErrorResult, new MemoryCacheEntryOptions().SetSize(1));
                    await BroadcastError(analysisId, $"Analysis error: {ex.Message}");
                    _logger.LogInformation($"[Analysis {analysisId}] Set error status due to exception: {ex.Message}");
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogError(cacheEx, $"[Analysis {analysisId}] Failed to update cache with error status");
            }
        }
        finally
        {
            _logger.LogInformation($"[Analysis {analysisId}] Background analysis task completed");
        }
    }

    private async Task BroadcastProgress(string analysisId, string status)
    {
        try
        {
            var progressDto = new ProgressDto(status, null, null, false);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Keep cache for fallback - create AnalysisResult from ProgressDto
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow
            };
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
    }

    private async Task BroadcastComplete(string analysisId, string result)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis complete", result, null, true);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Store analysis ID in session for view switching
            HttpContext.Session.SetString("AnalysisId", analysisId);
            
            // Cache as AnalysisResult - create AnalysisResult from ProgressDto
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
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

    private async Task BroadcastError(string analysisId, string error)
    {
        try
        {
            var progressDto = new ProgressDto("Analysis failed", null, error, true);
            await _hubContext.Clients.Group(analysisId).SendAsync("UpdateProgress", progressDto);
            
            // Cache as AnalysisResult - create AnalysisResult from ProgressDto
            var cacheResult = new AnalysisResult
            {
                Status = progressDto.Status,
                Result = progressDto.Result,
                Error = progressDto.Error,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _cache.Set($"analysis_{analysisId}", cacheResult, new MemoryCacheEntryOptions().SetSize(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR broadcast failed for analysis {AnalysisId}", analysisId);
        }
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
