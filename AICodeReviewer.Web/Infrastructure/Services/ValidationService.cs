using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Extensions;
using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Infrastructure.Services
{
    /// <summary>
    /// Service for validating code analysis requests.
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly IRepositoryManagementService _repositoryService;
        private readonly IPathValidationService _pathService;
        private readonly IConfiguration _configuration;

        public ValidationService(
            ILogger<ValidationService> logger,
            IRepositoryManagementService repositoryService,
            IPathValidationService pathService,
            IConfiguration configuration)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _pathService = pathService;
            _configuration = configuration;
        }

        public Task<(bool isValid, string? error, string? resolvedFilePath)> ValidateAnalysisRequestAsync(
            RunAnalysisRequest request,
            ISession session,
            IWebHostEnvironment environment)
        {
            try
            {
                // Use request data or fall back to session data
                var defaultRepositoryPath = Path.Combine(environment.ContentRootPath, "..");
                var repositoryPath = request.RepositoryPath ?? session.GetString("RepositoryPath") ?? defaultRepositoryPath;
                var selectedDocuments = request.SelectedDocuments ?? session.GetObject<List<string>>("SelectedDocuments") ?? new List<string>();
                var language = request.Language ?? session.GetString("Language") ?? "NET";
                var analysisType = request.AnalysisType ?? AnalysisType.Uncommitted;
                var commitId = request.CommitId;
                var filePath = request.FilePath;
                var fileContent = request.FileContent;

                _logger.LogInformation($"[Validation] Request SelectedDocuments count: {selectedDocuments.Count}");

                // Store language in session for consistency
                session.SetString("Language", language);

                // Validate API key
                var apiKey = _configuration["OpenRouter:ApiKey"] ?? "";
                var apiKeyExists = !string.IsNullOrWhiteSpace(apiKey);
                var maskedPrefix = apiKey?.Length > 0 ? $"{apiKey.Substring(0, Math.Min(6, apiKey.Length))}..." : "";
                _logger.LogDebug("[OpenRouter] API key exists: {Exists}; length: {Len}; startsWith(masked): {Prefix}",
                    apiKeyExists, apiKey?.Length ?? 0, maskedPrefix);
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("API key not configured");
                    return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, "API key not configured", null));
                }

                // Validate selected documents
                if (selectedDocuments.Count == 0)
                {
                    _logger.LogError("No coding standards selected");
                    return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, "No coding standards selected", null));
                }

                string? resolvedFilePath = null;

                // Validate based on analysis type
                if (analysisType == AnalysisType.SingleFile)
                {
                    _logger.LogInformation("[Validation] Single file analysis requested with filePath: {FilePath}", filePath);

                    if (string.IsNullOrEmpty(filePath))
                    {
                        _logger.LogWarning("[Validation] File path is missing for single file analysis");
                        return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, "File path is required for single file analysis", null));
                    }

                    // If file content is provided, we don't need to validate the file path on the file system
                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        _logger.LogInformation("[Validation] File content provided, skipping file system validation");
                        resolvedFilePath = filePath;
                    }
                    else
                    {
                        // Validate the file path
                        var (resolvedPath, isValid, validationError) = _pathService.ValidateSingleFilePath(filePath, repositoryPath, environment.ContentRootPath);
                        if (!isValid)
                        {
                            _logger.LogWarning("[Validation] File path validation failed: {Error}", validationError);
                            return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, validationError, null));
                        }

                        resolvedFilePath = resolvedPath;
                        _logger.LogInformation("[Validation] File validation passed for: {FilePath}", resolvedPath);
                    }
                }
                else
                {
                    // Validate git repository for git-based analysis
                    var (isValid, repoError) = _repositoryService.ValidateRepositoryForAnalysis(repositoryPath);
                    if (!isValid)
                    {
                        _logger.LogWarning("[Validation] Repository validation failed: {Error}", repoError);
                        return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, repoError, null));
                    }

                    // Validate commit ID if commit analysis requested
                    if (analysisType == AnalysisType.Commit)
                    {
                        if (string.IsNullOrEmpty(commitId))
                        {
                            _logger.LogWarning("[Validation] Commit ID is missing for commit analysis");
                            return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, "Commit ID is required for commit analysis", null));
                        }
                    }

                    // Validate staged changes if staged analysis requested
                    if (analysisType == AnalysisType.Staged)
                    {
                        var (hasStaged, stagedError) = _repositoryService.HasStagedChanges(repositoryPath);
                        if (!hasStaged)
                        {
                            _logger.LogWarning("[Validation] No staged changes found: {Error}", stagedError);
                            return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, "No staged changes found. Use 'git add' to stage files for analysis.", null));
                        }
                    }
                }

                _logger.LogInformation("[Validation] All validations passed for analysis type {AnalysisType}", analysisType);
                return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((true, null, resolvedFilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Validation] Unexpected error during request validation");
                return Task.FromResult<(bool isValid, string? error, string? resolvedFilePath)>((false, $"Validation error: {ex.Message}", null));
            }
        }
    }
}