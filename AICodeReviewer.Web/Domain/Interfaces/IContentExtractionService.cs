
using AICodeReviewer.Web.Models;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Domain.Interfaces
{
    /// <summary>
    /// Interface for extracting code content for analysis.
    /// </summary>
    public interface IContentExtractionService
    {
        /// <summary>
        /// Extracts code content based on the analysis type.
        /// </summary>
        /// <param name="repositoryPath">The path to the git repository.</param>
        /// <param name="analysisType">The type of analysis.</param>
        /// <param name="commitId">Commit ID for commit analysis.</param>
        /// <param name="filePath">File path for single file analysis.</param>
        /// <param name="fileContent">File content for single file analysis (optional).</param>
        /// <returns>Tuple with content, error flag, is file content flag, and error message.</returns>
        Task<(string content, bool contentError, bool isFileContent, string? error)> ExtractContentAsync(
            string repositoryPath,
            AnalysisType analysisType,
            string? commitId = null,
            string? filePath = null,
            string? fileContent = null);
    }
}