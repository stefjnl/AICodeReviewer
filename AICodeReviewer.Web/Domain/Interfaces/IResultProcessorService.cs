using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Domain.Interfaces
{
    /// <summary>
    /// Interface for processing AI analysis results and broadcasting them.
    /// </summary>
    public interface IResultProcessorService
    {
        /// <summary>
        /// Processes the AI analysis result, structures it, updates cache, and broadcasts via SignalR.
        /// </summary>
        /// <param name="analysisId">The unique analysis ID.</param>
        /// <param name="analysis">The raw AI analysis response.</param>
        /// <param name="errorMessage">Error message if any.</param>
        /// <param name="aiError">Flag indicating if AI processing failed.</param>
        /// <param name="usedModel">The AI model used.</param>
        /// <param name="fallbackModel">The fallback model if used.</param>
        /// <param name="session">The user session for broadcasting.</param>
        /// <returns>Task for async operation.</returns>
        Task ProcessAndBroadcastAsync(
            string analysisId,
            string analysis,
            string? errorMessage,
            bool aiError,
            string usedModel,
            string fallbackModel,
            ISession session);
    }
}