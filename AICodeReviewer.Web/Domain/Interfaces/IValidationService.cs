using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AICodeReviewer.Web.Domain.Interfaces
{
    /// <summary>
    /// Interface for validation services related to code analysis requests.
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates the analysis request based on type, paths, and session data.
        /// </summary>
        /// <param name="request">The analysis request containing paths, type, etc.</param>
        /// <param name="session">The user session for fallback data.</param>
        /// <param name="environment">The hosting environment for path resolution.</param>
        /// <returns>A tuple indicating validity, any error message, and resolved file path for single file analysis.</returns>
        Task<(bool isValid, string? error, string? resolvedFilePath)> ValidateAnalysisRequestAsync(
            RunAnalysisRequest request,
            ISession session,
            IWebHostEnvironment environment);
    }
}