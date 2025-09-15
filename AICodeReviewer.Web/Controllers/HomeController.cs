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

                _logger.LogInformation($"[RunAnalysis] Validating file existence: {filePath}");
                _logger.LogInformation($"[RunAnalysis] File exists check: {System.IO.File.Exists(filePath)}");
                _logger.LogInformation($"[RunAnalysis] Current working directory: {Environment.CurrentDirectory}");
                _logger.LogInformation($"[RunAnalysis] Content root path: {_environment.ContentRootPath}");
                _logger.LogInformation($"[RunAnalysis] Repository path: {repositoryPath}");
                
                var originalFilePath = filePath;
                
                // Check if it's just a filename (no path)
                if (!filePath.Contains(Path.DirectorySeparatorChar) && !filePath.Contains(Path.AltDirectorySeparatorChar))
                {
                    _logger.LogWarning($"[RunAnalysis] File path appears to be just a filename: {filePath}");
                    
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
                            _logger.LogWarning($"[RunAnalysis] Could not enumerate subdirectories: {ex.Message}");
                        }
                    }
                    
                    // Add other common paths
                    searchPaths.Add(_environment.ContentRootPath);
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
                                    _logger.LogInformation($"[RunAnalysis] File found at: {filePath}");
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
                                            _logger.LogInformation($"[RunAnalysis] File found with extension at: {filePath}");
                                            break;
                                        }
                                    }
                                    if (fileFound) break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug($"[RunAnalysis] Error searching in {searchPath}: {ex.Message}");
                            }
                        }
                    }
                    
                    if (!fileFound)
                    {
                        _logger.LogWarning($"[RunAnalysis] File not found in search paths. Suggesting user enters full path.");
                        var errorMsg = $"File '{originalFilePath}' not found. ";
                        errorMsg += "Please provide the full file path (e.g., C:\\path\\to\\file.py or /path/to/file.py) or ";
                        errorMsg += "ensure the file is in the repository directory: " + repositoryPath;
                        return Json(new { success = false, error = errorMsg });
                    }
                }
                else
                {
                    // User provided a path, check if it's relative and resolve it
                    if (!Path.IsPathRooted(filePath))
                    {
                        _logger.LogInformation($"[RunAnalysis] Relative path detected, resolving: {filePath}");
                        
                        // Try resolving relative to repository path first
                        var relativePath = Path.Combine(repositoryPath, filePath);
                        if (System.IO.File.Exists(relativePath))
                        {
                            filePath = relativePath;
                            _logger.LogInformation($"[RunAnalysis] Resolved relative path to: {filePath}");
                        }
                        else
                        {
                            // Try relative to content root
                            var contentRelativePath = Path.Combine(_environment.ContentRootPath, filePath);
                            if (System.IO.File.Exists(contentRelativePath))
                            {
                                filePath = contentRelativePath;
                                _logger.LogInformation($"[RunAnalysis] Resolved relative to content root: {filePath}");
                            }
                        }
                    }
                }

                // Final validation - check if file exists and is readable
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError($"[RunAnalysis] File not found after all resolution attempts: {filePath}");
                    var finalError = $"File not found: {originalFilePath}. ";
                    finalError += "Please verify the file path is correct and the file exists. ";
                    finalError += "If using a relative path, ensure it's relative to the repository root: " + repositoryPath;
                    return Json(new { success = false, error = finalError });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".cs", ".js", ".py" };
                var extension = Path.GetExtension(filePath).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, error = $"Unsupported file type '{extension}'. Allowed extensions: {string.Join(", ", allowedExtensions)}" });
                }
                
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
                    await RunBackgroundAnalysisWithCache(analysisId, repositoryPath, selectedDocuments, docsFolder, apiKey, model, language, analysisType, commitId, filePath);
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

    private async Task RunBackgroundAnalysisWithCache(string analysisId, string repositoryPath, List<string> selectedDocuments, string documentsFolder, string apiKey, string model, string language, string analysisType = "uncommitted", string? commitId = null, string? filePath = null)
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

            // Get content based on analysis type
            _logger.LogInformation($"[Analysis {analysisId}] Analysis type: {analysisType}");
            string content;
            bool contentError;
            bool isFileContent = false;
            
            if (analysisType == "singlefile" && !string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation($"[Analysis {analysisId}] Reading single file: {filePath}");
                try
                {
                    content = await System.IO.File.ReadAllTextAsync(filePath);
                    contentError = false;
                    isFileContent = true;
                    _logger.LogInformation($"[Analysis {analysisId}] File read successfully, length: {content.Length}");
                }
                catch (Exception ex)
                {
                    content = $"Error reading file: {ex.Message}";
                    contentError = true;
                    _logger.LogError(ex, $"[Analysis {analysisId}] Failed to read file: {filePath}");
                }
            }
            else if (analysisType == "commit" && !string.IsNullOrEmpty(commitId))
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting commit diff for commit: {commitId}");
                (content, contentError) = GitService.GetCommitDiff(repositoryPath, commitId);
            }
            else
            {
                _logger.LogInformation($"[Analysis {analysisId}] Extracting uncommitted changes");
                (content, contentError) = GitService.ExtractDiff(repositoryPath);
            }
            
            _logger.LogInformation($"[Analysis {analysisId}] Content extraction complete - Error: {contentError}, Content length: {content?.Length ?? 0}");
            
            // Store content in cache for results display
            if (!contentError && !string.IsNullOrEmpty(content))
            {
                _cache.Set($"content_{analysisId}", content, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                    .SetSize(1));
                _logger.LogInformation($"[Analysis {analysisId}] Stored content in cache ({content.Length} bytes)");
            }
            
            if (contentError)
            {
                _logger.LogError($"[Analysis {analysisId}] Content extraction failed: {content}");
                if (result != null)
                {
                    result.Status = "Error";
                    result.Error = isFileContent ? $"File reading error: {content}" : $"Git diff error: {content}";
                    result.CompletedAt = DateTime.UtcNow;
                    
                    var cacheOptions2 = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                        .SetSize(1);
                    _cache.Set($"analysis_{analysisId}", result, cacheOptions2);
                    _logger.LogInformation($"[Analysis {analysisId}] Set error status and completed");
                }
                return;
            }
            _logger.LogInformation($"[Analysis {analysisId}] Content extracted successfully");

            // Update status via SignalR
            _logger.LogInformation($"[Analysis {analysisId}] Starting document loading phase");
            if (result != null)
            {
                result.Status = "Loading documents...";
                await BroadcastProgress(analysisId, "Loading documents...");
                _logger.LogInformation($"[Analysis {analysisId}] Updated status to 'Loading documents...'");
            }
            
            // Load selected documents asynchronously
            _logger.LogInformation($"[Analysis {analysisId}] Loading {selectedDocuments.Count} selected documents");
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder path: {documentsFolder}");
            _logger.LogInformation($"[Analysis {analysisId}] Documents folder exists: {Directory.Exists(documentsFolder)}");
            
            // Create tasks for parallel document loading
            var documentTasks = selectedDocuments.Select(async docName =>
            {
                _logger.LogInformation($"[Analysis {analysisId}] Loading document: {docName} from folder: {documentsFolder}");
                var (content, docError) = await DocumentService.LoadDocumentAsync(docName, documentsFolder);
                _logger.LogInformation($"[Analysis {analysisId}] Document {docName} - Error: {docError}, Content length: {content?.Length ?? 0}");
                
                if (!docError)
                {
                    _logger.LogInformation($"[Analysis {analysisId}] Document {docName} loaded successfully");
                    return (content, docError, docName);
                }
                else
                {
                    _logger.LogWarning($"[Analysis {analysisId}] Failed to load document {docName}: {content}");
                    return (content, docError, docName);
                }
            }).ToList();
            
            // Wait for all documents to load in parallel
            var documentResults = await Task.WhenAll(documentTasks);
            
            // Collect successful documents
            var codingStandards = new List<string>();
            foreach (var (docContent, docError, docName) in documentResults)
            {
                if (!docError)
                {
                    codingStandards.Add(docContent);
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
            _logger.LogInformation($"[Analysis {analysisId}] - Content length: {content?.Length ?? 0}");
            _logger.LogInformation($"[Analysis {analysisId}] - Coding standards count: {codingStandards.Count}");
            _logger.LogInformation($"[Analysis {analysisId}] - API key configured: {!string.IsNullOrEmpty(apiKey)}");
            _logger.LogInformation($"[Analysis {analysisId}] - Model: {model}");
            _logger.LogInformation($"[Analysis {analysisId}] - Analysis type: {(isFileContent ? "Single File" : "Git Diff")}");
            
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
                    await AIService.AnalyzeCodeAsync(content, codingStandards, requirements, apiKey, model, language, isFileContent),
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

            // Validate the path exists
            if (!Directory.Exists(currentPath))
            {
                return Json(new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = currentPath,
                    ParentPath = null,
                    IsGitRepository = false,
                    Error = "Directory not found"
                });
            }

            // Get parent directory
            string? parentPath = null;
            try
            {
                var parent = Directory.GetParent(currentPath);
                parentPath = parent?.FullName;
                
                // Special case for root directory on Unix
                if (Environment.OSVersion.Platform != PlatformID.Win32NT && currentPath == "/")
                {
                    parentPath = null;
                }
                
                // Special case for drive root on Windows
                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    (currentPath.Length == 3 && currentPath.EndsWith(":\\")))
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
                return Json(new DirectoryBrowseResponse
                {
                    Directories = new List<DirectoryItem>(),
                    Files = new List<DirectoryItem>(),
                    CurrentPath = currentPath,
                    ParentPath = parentPath,
                    IsGitRepository = false,
                    Error = $"Error accessing directory: {ex.Message}"
                });
            }

            // Check if current directory is a git repository
            bool isCurrentGitRepo = Directory.Exists(Path.Combine(currentPath, ".git"));

            return Json(new DirectoryBrowseResponse
            {
                Directories = directories.OrderBy(d => d.Name).ToList(),
                Files = files.OrderBy(f => f.Name).ToList(),
                CurrentPath = currentPath,
                ParentPath = parentPath,
                IsGitRepository = isCurrentGitRepo
            });
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
