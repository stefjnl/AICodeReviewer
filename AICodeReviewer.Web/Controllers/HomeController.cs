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
    public async Task<IActionResult> RunAnalysis()
    {
        try
        {
            // Get configuration values
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"];

            // Get git diff from the actual repository
            var repositoryPath = HttpContext.Session.GetString("RepositoryPath") ?? _environment.ContentRootPath;
            var (gitDiff, gitError) = GitService.ExtractDiff(repositoryPath);
            
            if (gitError)
            {
                // Store error in session and redirect back to index
                HttpContext.Session.SetString("AnalysisResult", "");
                HttpContext.Session.SetString("AnalysisError", $"Git diff error: {gitDiff}");
                return RedirectToAction("Index");
            }

            // Get selected documents and load their content
            var selectedDocuments = HttpContext.Session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
            var documentsFolder = HttpContext.Session.GetString("DocumentsFolder") ?? _defaultDocumentsPath;
            
            var codingStandards = new List<string>();
            foreach (var docName in selectedDocuments)
            {
                var (content, docError) = DocumentService.LoadDocument(docName, documentsFolder);
                if (!docError)
                    codingStandards.Add(content);
            }

            // Get requirements (can be enhanced later)
            string requirements = "Follow .NET best practices and coding standards";

            // Call AI service
            var (analysis, aiError) = await AIService.AnalyzeCodeAsync(gitDiff, codingStandards, requirements, apiKey, model);
            
            // Store results in session
            HttpContext.Session.SetString("AnalysisResult", analysis);
            HttpContext.Session.SetString("AnalysisError", aiError ? "Analysis failed" : "");
            
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            HttpContext.Session.SetString("AnalysisResult", "");
            HttpContext.Session.SetString("AnalysisError", $"Analysis error: {ex.Message}");
            return RedirectToAction("Index");
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
