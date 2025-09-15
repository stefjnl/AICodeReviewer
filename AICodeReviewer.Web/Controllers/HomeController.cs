using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Web.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AICodeReviewer.Web; // Add this to access DocumentService and AIService
using AICodeReviewer.Web.Services; // Add this to access GitService

namespace AICodeReviewer.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly string _defaultDocumentsPath;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        // Documents folder is in the root of the application, not in the Web project
        _defaultDocumentsPath = Path.Combine(_environment.ContentRootPath, "..", "Documents");
    }

    public IActionResult Index()
    {
        // Git repository detection (existing)
        var (branchInfo, isError) = GitHelper.DetectRepository(_environment.ContentRootPath);
        ViewBag.BranchInfo = branchInfo;
        ViewBag.IsError = isError;
        
        // Repository path management for Git diff extraction
        var repositoryPath = HttpContext.Session.GetString("RepositoryPath") ?? _environment.ContentRootPath;
        ViewBag.RepositoryPath = repositoryPath;
        
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
            var repositoryPath = request.RepositoryPath ?? HttpContext.Session.GetString("RepositoryPath") ?? _environment.ContentRootPath;
            var selectedDocuments = request.SelectedDocuments ?? HttpContext.Session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
            var documentsFolder = request.DocumentsFolder ?? HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"];

            // Validate required fields
            if (string.IsNullOrEmpty(apiKey))
            {
                return Json(new { success = false, error = "API key not configured" });
            }

            if (selectedDocuments.Count == 0)
            {
                return Json(new { success = false, error = "No coding standards selected" });
            }

            // Set initial status and clear previous results
            HttpContext.Session.SetString("AnalysisStatus", "Starting");
            HttpContext.Session.Remove("AnalysisResult");
            HttpContext.Session.Remove("AnalysisError");

            // Update session with request data
            if (!string.IsNullOrEmpty(request.RepositoryPath))
                HttpContext.Session.SetString("RepositoryPath", request.RepositoryPath);
            if (request.SelectedDocuments != null && request.SelectedDocuments.Count > 0)
                HttpContext.Session.SetObject("SelectedDocuments", request.SelectedDocuments);
            if (!string.IsNullOrEmpty(request.DocumentsFolder))
                HttpContext.Session.SetString("DocumentsFolder", request.DocumentsFolder);

            // Start background analysis
            _ = Task.Run(async () => await RunBackgroundAnalysis(repositoryPath, selectedDocuments, apiKey, model));

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetAnalysisStatus()
    {
        var status = HttpContext.Session.GetString("AnalysisStatus") ?? "NotStarted";
        var result = HttpContext.Session.GetString("AnalysisResult");
        var error = HttpContext.Session.GetString("AnalysisError");
        var isComplete = status == "Complete" || status == "Error";

        return Json(new { status, result, error, isComplete });
    }

    private async Task RunBackgroundAnalysis(string repositoryPath, List<string> selectedDocuments, string apiKey, string model)
    {
        try
        {
            // Update status
            HttpContext.Session.SetString("AnalysisStatus", "Reading git changes...");
            
            // Get git diff
            var (gitDiff, gitError) = GitService.ExtractDiff(repositoryPath);
            if (gitError)
            {
                HttpContext.Session.SetString("AnalysisStatus", "Error");
                HttpContext.Session.SetString("AnalysisError", $"Git diff error: {gitDiff}");
                return;
            }

            // Update status
            HttpContext.Session.SetString("AnalysisStatus", "Loading documents...");
            
            // Load selected documents
            var documentsFolder = HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            var codingStandards = new List<string>();
            foreach (var docName in selectedDocuments)
            {
                var (content, docError) = DocumentService.LoadDocument(docName, documentsFolder);
                if (!docError)
                    codingStandards.Add(content);
            }

            // Update status
            HttpContext.Session.SetString("AnalysisStatus", "AI analysis...");
            
            // Get requirements
            string requirements = "Follow .NET best practices and coding standards";

            // Call AI service
            var (analysis, aiError) = await AIService.AnalyzeCodeAsync(gitDiff, codingStandards, requirements, apiKey, model);
            
            if (aiError)
            {
                HttpContext.Session.SetString("AnalysisStatus", "Error");
                HttpContext.Session.SetString("AnalysisError", "AI analysis failed");
            }
            else
            {
                // Store final result
                HttpContext.Session.SetString("AnalysisResult", analysis);
                HttpContext.Session.SetString("AnalysisStatus", "Complete");
            }
        }
        catch (Exception ex)
        {
            HttpContext.Session.SetString("AnalysisStatus", "Error");
            HttpContext.Session.SetString("AnalysisError", $"Analysis error: {ex.Message}");
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
