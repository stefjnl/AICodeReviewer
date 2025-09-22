using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using LibGit2Sharp;
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
        private readonly IDiffProviderFactory _diffProviderFactory;

        public ContentExtractionService(
            ILogger<ContentExtractionService> logger,
            IRepositoryManagementService repositoryService,
            IDiffProviderFactory diffProviderFactory)
        {
            _logger = logger;
            _repositoryService = repositoryService;
            _diffProviderFactory = diffProviderFactory;
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
                else
                {
                    // Use the diff provider pattern for git-based analysis
                    string analysisTypeString = analysisType switch
                    {
                        AnalysisType.Commit => "commit",
                        AnalysisType.Staged => "staged",
                        AnalysisType.Uncommitted => "uncommitted",
                        _ => throw new ArgumentException($"Unsupported analysis type: {analysisType}")
                    };

                    // Create the appropriate diff provider
                    var diffProvider = _diffProviderFactory.CreateProvider(analysisTypeString);

                    // Validate inputs
                    if (!diffProvider.ValidateInputs(commitId, null, null))
                    {
                        contentError = true;
                        error = "Invalid input parameters for the selected analysis type";
                        _logger.LogWarning("[ContentExtraction] Invalid input parameters for analysis type {AnalysisType}", analysisTypeString);
                        return (content, contentError, isFileContent, error);
                    }

                    // Get the diff content using the provider
                    using (var repo = new Repository(repositoryPath))
                    {
                        var (diffContent, isError, errorMsg) = diffProvider.GetDiff(repo, commitId, null, null);
                        
                        content = diffContent;
                        contentError = isError;
                        error = errorMsg;
                        isFileContent = false;
                        
                        if (contentError)
                        {
                            _logger.LogError("[ContentExtraction] Content extraction failed: {Error}", error);
                        }
                        else
                        {
                            _logger.LogInformation("[ContentExtraction] Diff content extracted successfully, length: {Length}", content.Length);
                        }
                    }
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