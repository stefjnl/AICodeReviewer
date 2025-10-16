using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Web.Domain.Interfaces;

namespace AICodeReviewer.Web.Controllers;

/// <summary>
/// API controller for git repository operations
/// Provides endpoints for validating and inspecting git repositories
/// </summary>
[ApiController]
[Route("api/git")]
public class GitApiController : ControllerBase
{
    private readonly ILogger<GitApiController> _logger;
    private readonly IRepositoryManagementService _repositoryService;

    public GitApiController(ILogger<GitApiController> logger, IRepositoryManagementService repositoryService)
    {
        _logger = logger;
        _repositoryService = repositoryService;
    }

    /// <summary>
    /// Validates a git repository path and returns repository information
    /// </summary>
    /// <param name="request">Repository path validation request</param>
    /// <returns>Git repository validation result</returns>
    [HttpPost("validate")]
    public IActionResult ValidateRepository([FromBody] RepositoryValidationRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RepositoryPath))
            {
                return BadRequest(new
                {
                    success = false,
                    isValidRepo = false,
                    error = "Repository path is required"
                });
            }

            var repositoryPath = request.RepositoryPath.Trim();
            _logger.LogInformation("Validating repository path: {RepositoryPath}", repositoryPath);

            // Use repository service to validate repository
            var (branchInfo, isError) = _repositoryService.DetectRepository(repositoryPath);

            if (isError || string.IsNullOrEmpty(branchInfo))
            {
                _logger.LogWarning("Repository validation failed for: {RepositoryPath}", repositoryPath);
                return Ok(new
                {
                    success = true,
                    isValidRepo = false,
                    error = "Not a valid git repository or access denied"
                });
            }

            // Repository is valid - simplified response
            var result = new
            {
                success = true,
                isValidRepo = true,
                repositoryPath = repositoryPath,
                currentBranch = branchInfo,
                hasChanges = true, // Simplified for now
                unstagedFiles = 0, // Simplified for now
                stagedFiles = 0, // Simplified for now
                aheadBy = 0, // Simplified for now
                behindBy = 0, // Simplified for now
                lastCommit = DateTime.Now.ToString(), // Simplified for now
                error = (string?)null
            };

            _logger.LogInformation("Repository validation successful: {RepositoryPath}", repositoryPath);
            return Ok(result);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating repository");
            return StatusCode(500, new
            {
                success = false,
                isValidRepo = false,
                error = "An error occurred while validating the repository"
            });
        }
    }

    /// <summary>
    /// Gets basic repository information including branches
    /// </summary>
    /// <param name="request">Repository path request</param>
    /// <returns>Repository information</returns>
    [HttpPost("info")]
    public IActionResult GetRepositoryInfo([FromBody] RepositoryValidationRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RepositoryPath))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Repository path is required"
                });
            }

            var repositoryPath = request.RepositoryPath.Trim();
            _logger.LogInformation("Getting repository info for: {RepositoryPath}", repositoryPath);

            // Use repository service to detect repository
            var (branchInfo, isError) = _repositoryService.DetectRepository(repositoryPath);

            if (isError || string.IsNullOrEmpty(branchInfo))
            {
                _logger.LogWarning("Repository not found or invalid: {RepositoryPath}", repositoryPath);
                return Ok(new
                {
                    success = true,
                    isValidRepo = false,
                    error = "Not a valid git repository"
                });
            }

            // Format response with repository info
            var result = new
            {
                success = true,
                isValidRepo = true,
                repositoryPath,
                currentBranch = branchInfo,
                branches = new[] { new { name = branchInfo, isCurrent = true } }, // Simplified for now
                remoteUrl = "unknown", // Simplified for now
                error = (string?)null
            };

            return Ok(result);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository info");
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while getting repository information"
            });
        }
    }

    /// <summary>
    /// Clones a Git repository from a URL
    /// </summary>
    /// <param name="request">Clone repository request with Git URL and optional access token</param>
    /// <returns>Clone operation result</returns>
    [HttpPost("clone")]
    public IActionResult CloneRepository([FromBody] CloneRepositoryRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.GitUrl))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Git URL is required"
                });
            }

            var gitUrl = request.GitUrl.Trim();
            _logger.LogInformation("Cloning repository from URL: {GitUrl}", gitUrl);

            // Basic URL validation
            if (!gitUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !gitUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Git URL must start with https:// or http://"
                });
            }

            // Validate URL contains known Git hosting patterns
            var validPatterns = new[] { "github.com", "gitlab.com", "bitbucket.org", "dev.azure.com" };
            var isValidHost = validPatterns.Any(pattern => gitUrl.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (!isValidHost)
            {
                _logger.LogWarning("Git URL does not contain recognized hosting pattern: {GitUrl}", gitUrl);
            }

            // Clone repository
            var (success, localPath, error) = _repositoryService.CloneRepository(gitUrl, request.AccessToken);

            if (!success)
            {
                _logger.LogWarning("Repository clone failed: {Error}", error);
                return Ok(new
                {
                    success = false,
                    repositoryPath = (string?)null,
                    error
                });
            }

            _logger.LogInformation("Repository cloned successfully to: {LocalPath}", localPath);
            return Ok(new
            {
                success = true,
                repositoryPath = localPath,
                error = (string?)null
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning repository");
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while cloning the repository"
            });
        }
    }

    /// <summary>
    /// Cleans up a cloned repository directory
    /// </summary>
    /// <param name="request">Cleanup request with repository path</param>
    /// <returns>Cleanup operation result</returns>
    [HttpPost("cleanup")]
    public IActionResult CleanupRepository([FromBody] CleanupRepositoryRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RepositoryPath))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Repository path is required"
                });
            }

            var repositoryPath = request.RepositoryPath.Trim();
            _logger.LogInformation("Cleaning up repository at: {RepositoryPath}", repositoryPath);

            var (success, error) = _repositoryService.CleanupRepository(repositoryPath);

            if (!success)
            {
                _logger.LogWarning("Repository cleanup failed: {Error}", error);
                return Ok(new
                {
                    success = false,
                    error
                });
            }

            _logger.LogInformation("Repository cleaned up successfully: {RepositoryPath}", repositoryPath);
            return Ok(new
            {
                success = true,
                error = (string?)null
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up repository");
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while cleaning up the repository"
            });
        }
    }
}

/// <summary>
/// Request model for repository validation
/// </summary>
public class RepositoryValidationRequest
{
    public string RepositoryPath { get; set; } = string.Empty;
}

/// <summary>
/// Request model for cloning a repository
/// </summary>
public class CloneRepositoryRequest
{
    public string GitUrl { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}

/// <summary>
/// Request model for cleaning up a repository
/// </summary>
public class CleanupRepositoryRequest
{
    public string RepositoryPath { get; set; } = string.Empty;
}