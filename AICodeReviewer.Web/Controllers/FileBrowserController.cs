using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace AICodeReviewer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileBrowserController : ControllerBase
{
    private readonly IRepositoryManagementService _repositoryService;

    public FileBrowserController(IRepositoryManagementService repositoryService)
    {
        _repositoryService = repositoryService;
    }

    [HttpGet("browse")]
    public async Task<IActionResult> BrowseFiles([FromQuery] string repositoryPath, [FromQuery] string path = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                return BadRequest(new { success = false, error = "Repository path is required" });
            }

            var (files, isError) = await _repositoryService.GetFilesInDirectoryAsync(repositoryPath, path);

            if (isError)
            {
                return StatusCode(500, new { error = "An error occurred while browsing files. Please try again." });
            }

            var response = new
            {
                currentPath = path,
                files = files.Select(f => new
                {
                    name = f.Name,
                    fullPath = f.FullPath,
                    size = f.Size,
                    isDirectory = f.IsDirectory
                }).ToList(),
                error = (string?)null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File browsing error: {ex}");
            return StatusCode(500, new { error = "An error occurred while browsing files. Please try again." });
        }
    }

    [HttpPost("filecontent")]
    public async Task<IActionResult> GetFileContent([FromBody] FileContentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RepositoryPath))
            {
                return BadRequest(new { success = false, error = "Repository path is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return BadRequest(new { success = false, error = "File path is required" });
            }

            var (content, isError) = await _repositoryService.GetFileContentAsync(request.RepositoryPath, request.FilePath);

            if (isError)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while reading the file. Please try again." });
            }

            var fileInfo = new FileInfo(request.FilePath);

            return Ok(new
            {
                success = true,
                content = content,
                fileName = Path.GetFileName(request.FilePath),
                size = fileInfo.Length
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File content error: {ex}");
            return StatusCode(500, new { success = false, error = "An error occurred while reading the file. Please try again." });
        }
    }
}

public class FileContentRequest
{
    public string RepositoryPath { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}