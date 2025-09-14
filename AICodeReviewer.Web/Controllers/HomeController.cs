using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Web.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AICodeReviewer.Web; // Add this to access DocumentService and AIService

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

            // Get git diff (enhanced mock data to demonstrate AI capabilities)
            var (branchInfo, isError) = GitHelper.DetectRepository(_environment.ContentRootPath);
            string gitDiff = isError ? @"// No git changes detected" : @"diff --git a/UserService.cs b/UserService.cs
index 1234567..abcdefg 100644
--- a/UserService.cs
+++ b/UserService.cs
@@ -10,8 +10,15 @@ namespace MyApp.Services
     public class UserService
     {
-        public void ProcessUserData(string userInput)
+        public async Task<string> ProcessUserDataAsync(string userInput, ILogger logger)
         {
-            // TODO: Implement processing
+            if (string.IsNullOrWhiteSpace(userInput))
+                throw new ArgumentNullException(nameof(userInput));
+
+            if (logger == null)
+                throw new ArgumentNullException(nameof(logger));
+
+            logger.LogInformation($""Processing user data: {userInput}"");
+
+            return await Task.FromResult($""Processed: {userInput}"");
         }
     }
 }";

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
