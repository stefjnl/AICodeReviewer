using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Infrastructure.Services
{
    /// <summary>
    /// Service for extracting code content for analysis based on type.
    /// </summary>
    public class ContentExtractionService : IContentExtractionService
    {
        private readonly ILogger<ContentExtractionService> _logger;
        private readonly IRepositoryManagementService _repositoryService;

        public ContentExtractionService(
            ILogger<ContentExtractionService> logger,
            IRepositoryManagementService repositoryService)
        {
            _logger = logger;
            _repositoryService = repositoryService;
        }

        public async Task<(string content, bool contentError, bool isFileContent, string? error)> ExtractContentAsync(
            string repositoryPath,
            AnalysisType analysisType,
            string? commitId = null,
            string? filePath = null)
        {
            _logger.LogInformation("[ContentExtraction] Starting extraction for type {AnalysisType}, repo {RepositoryPath}", analysisType, repositoryPath);

            string content = string.Empty;
            bool contentError = false;
            bool isFileContent = false;
            string? error = null;

            try
            {
                if (analysisType == AnalysisType.SingleFile && !string.IsNullOrEmpty(filePath))
                {
                    _logger.LogInformation("[ContentExtraction] Extracting single file content: {FilePath}", filePath);
                    content = await File.ReadAllTextAsync(filePath);
                    contentError = false;
                    isFileContent = true;
                    _logger.LogInformation("[ContentExtraction] File read successfully, length: {Length}", content.Length);
                }
                else if (analysisType == AnalysisType.Commit && !string.IsNullOrEmpty(commitId))
                {
                    _logger.LogInformation("[ContentExtraction] Extracting commit diff for {CommitId}", commitId);
                    (content, contentError) = _repositoryService.GetCommitDiff(repositoryPath, commitId);
                    isFileContent = false;
                }
                else if (analysisType == AnalysisType.Staged)
                {
                    _logger.LogInformation("[ContentExtraction] Extracting staged changes");
                    (content, contentError) = _repositoryService.ExtractStagedDiff(repositoryPath);
                    isFileContent = false;
                }
                else
                {
                    _logger.LogInformation("[ContentExtraction] Extracting uncommitted changes");
                    (content, contentError) = _repositoryService.ExtractDiff(repositoryPath);
                    isFileContent = false;
                }

                if (contentError)
                {
                    error = content; // Original uses content as error message for git errors
                    _logger.LogError("[ContentExtraction] Content extraction failed: {Error}", error);
                }
                else if (string.IsNullOrEmpty(content))
                {
                    contentError = true;
                    error = "No content extracted";
                    _logger.LogWarning("[ContentExtraction] No content was extracted");
                }
                else
                {
                    _logger.LogInformation("[ContentExtraction] Content extracted successfully, length: {Length}", content.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ContentExtraction] Unexpected error during content extraction");
                contentError = true;
                error = ex.Message;
            }

            return (content, contentError, isFileContent, error);
        }
    }
}