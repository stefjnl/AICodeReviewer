using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Web.Models;
using System.Text.Json;

namespace AICodeReviewer.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _defaultDocumentsPath;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
