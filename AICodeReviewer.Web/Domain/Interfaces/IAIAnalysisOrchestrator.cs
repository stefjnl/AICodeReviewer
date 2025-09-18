using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Domain.Interfaces
{
    /// <summary>
    /// Interface for orchestrating AI analysis calls with timeout and fallback handling.
    /// </summary>
    public interface IAIAnalysisOrchestrator
    {
        /// <summary>
        /// Performs AI code analysis with timeout protection and fallback model support.
        /// </summary>
        /// <param name="content">The code content to analyze.</param>
        /// <param name="codingStandards">List of coding standards documents.</param>
        /// <param name="requirements">The analysis requirements.</param>
        /// <param name="apiKey">The API key for AI service.</param>
        /// <param name="primaryModel">The primary AI model to use.</param>
        /// <param name="fallbackModel">The fallback AI model if primary fails.</param>
        /// <param name="language">The programming language.</param>
        /// <param name="isFileContent">Indicates if content is from a single file.</param>
        /// <returns>A tuple with analysis result, error flag, error message, and used model.</returns>
        Task<(string analysis, bool error, string? errorMsg, string usedModel)> AnalyzeAsync(
            string content,
            List<string> codingStandards,
            string requirements,
            string apiKey,
            string primaryModel,
            string? fallbackModel,
            string language,
            bool isFileContent);
    }
}