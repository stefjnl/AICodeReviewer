using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace AICodeReviewer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DirectoryBrowserController : ControllerBase
{
    private readonly IDirectoryBrowsingService _directoryService;

    public DirectoryBrowserController(IDirectoryBrowsingService directoryService)
    {
        _directoryService = directoryService;
    }

    [HttpGet("browse")]
    public IActionResult BrowseDirectory([FromQuery] string path = "")
    {
        try
        {
            DirectoryBrowseResponse result;
            
            if (string.IsNullOrEmpty(path))
            {
                // Get root drives on Windows, or root directory on Unix
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var drives = _directoryService.GetRootDrives();
                    result = new DirectoryBrowseResponse
                    {
                        Directories = drives,
                        CurrentPath = "",
                        ParentPath = null,
                        IsGitRepository = false
                    };
                }
                else
                {
                    // On Unix systems, start from root
                    result = _directoryService.BrowseDirectory("/");
                }
            }
            else
            {
                result = _directoryService.BrowseDirectory(path);
            }
            
            // Format the response for the frontend
            var response = new
            {
                currentPath = result.CurrentPath,
                directories = result.Directories
                    .Where(d => d.IsDirectory)
                    .Select(d => new
                    {
                        name = d.Name,
                        fullPath = d.FullPath,
                        isGitRepository = d.IsGitRepository
                    })
                    .ToList(),
                parentPath = result.ParentPath,
                error = result.Error
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Log the detailed error for debugging
            Console.WriteLine($"Directory browsing error: {ex}");
            return StatusCode(500, new { error = "An error occurred while browsing the directory. Please try again." });
        }
    }
}