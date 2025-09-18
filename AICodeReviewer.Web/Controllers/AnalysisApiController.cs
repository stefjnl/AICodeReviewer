using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Web.Infrastructure.Services;
using AICodeReviewer.Web.Models;
using LibGit2Sharp;
using AICodeReviewer.Web.Domain.Interfaces;

namespace AICodeReviewer.Web.Controllers;

/// <summary>
/// API controller for analysis configuration and preview
/// </summary>
[ApiController]
[Route("api/analysis")]
public class AnalysisApiController : ControllerBase
{
    private readonly ILogger<AnalysisApiController> _logger;
    private readonly IRepositoryManagementService _repositoryService;

    public AnalysisApiController(ILogger<AnalysisApiController> logger, IRepositoryManagementService repositoryService)
    {
        _logger = logger;
        _repositoryService = repositoryService;
    }

    /// <summary>
    /// Get analysis options for a repository
    /// </summary>
    [HttpPost("options")]
    public IActionResult GetAnalysisOptions([FromBody] AnalysisOptionsRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RepositoryPath))
            {
                return BadRequest(new { success = false, error = "Repository path is required" });
            }

            if (!Directory.Exists(request.RepositoryPath))
            {
                return BadRequest(new { success = false, error = "Repository directory not found" });
            }

            var options = _repositoryService.GetAnalysisOptions(request.RepositoryPath);
            
            return Ok(new 
            { 
                success = true,
                commits = options.commits,
                branches = options.branches,
                modifiedFiles = options.modifiedFiles,
                stagedFiles = options.stagedFiles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis options for {Path}", request.RepositoryPath);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Preview changes for analysis configuration
    /// </summary>
    [HttpPost("preview")]
    public IActionResult PreviewChanges([FromBody] PreviewRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RepositoryPath))
            {
                return BadRequest(new { success = false, error = "Repository path is required" });
            }

            var preview = _repositoryService.PreviewChanges(request.RepositoryPath, request.AnalysisType, request.TargetCommit);
            
            return Ok(new 
            { 
                success = true,
                changesSummary = preview.changesSummary,
                isValid = preview.isValid,
                error = preview.error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing changes for {Path}", request.RepositoryPath);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for analysis options
/// </summary>
public class AnalysisOptionsRequest
{
    public string RepositoryPath { get; set; } = string.Empty;
}

/// <summary>
/// Request model for previewing changes
/// </summary>
public class PreviewRequest
{
    public string RepositoryPath { get; set; } = string.Empty;
    public string AnalysisType { get; set; } = "uncommitted"; // uncommitted, staged, commit
    public string? TargetCommit { get; set; }
}