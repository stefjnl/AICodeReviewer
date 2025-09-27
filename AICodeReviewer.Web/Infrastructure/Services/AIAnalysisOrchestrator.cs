using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Infrastructure.Services
{
    /// <summary>
    /// Service for orchestrating AI analysis calls with timeout and fallback handling.
    /// </summary>
    public class AIAnalysisOrchestrator : IAIAnalysisOrchestrator
    {
        private readonly ILogger<AIAnalysisOrchestrator> _logger;
        private readonly IAIService _aiService;

        public AIAnalysisOrchestrator(
            ILogger<AIAnalysisOrchestrator> logger,
            IAIService aiService)
        {
            _logger = logger;
            _aiService = aiService;
        }

        public async Task<(string analysis, bool error, string? errorMsg, string usedModel)> AnalyzeAsync(
            string content,
            List<string> codingStandards,
            string requirements,
            string apiKey,
            string primaryModel,
            string? fallbackModel,
            string language,
            bool isFileContent)
        {
            _logger.LogInformation("[AIAnalysis] Starting AI analysis with content length {Length}, standards count {Count}, model {Model}", 
                content.Length, codingStandards.Count, primaryModel);

            string analysis = string.Empty;
            bool error = false;
            string? errorMsg = null;
            string usedModel = primaryModel;
            bool fallbackWasUsed = false;

            // Add timeout protection (60 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                _logger.LogInformation("[AIAnalysis] Calling AI service with timeout");

                // Call AI service with timeout
                (analysis, error, errorMsg) = await ExecuteAnalysisWithModelAsync(content, codingStandards, requirements, apiKey, primaryModel, language, isFileContent, cts.Token);
                _logger.LogInformation("[AIAnalysis] Primary model call complete - Error: {Error}, Result length: {Length}", error, analysis.Length);

                if (error && IsRateLimitError(errorMsg) && !string.IsNullOrEmpty(fallbackModel))
                {
                    fallbackWasUsed = true;
                    usedModel = fallbackModel;
                    _logger.LogInformation("[AIAnalysis] Rate limit detected, falling back to {FallbackModel}", fallbackModel);

                    // Retry with fallback model
                    (analysis, error, errorMsg) = await ExecuteAnalysisWithModelAsync(content, codingStandards, requirements, apiKey, fallbackModel, language, isFileContent, cts.Token);
                    _logger.LogInformation("[AIAnalysis] Fallback model call complete - Error: {Error}, Result length: {Length}", error, analysis.Length);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("[AIAnalysis] AI analysis timed out after 60 seconds");
                analysis = "";
                error = true;
                errorMsg = "AI analysis timed out after 60 seconds";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AIAnalysis] Unexpected exception during AI service call");
                analysis = "";
                error = true;
                errorMsg = $"Unexpected error calling AI service: {ex.Message}";
            }

            if (fallbackWasUsed)
            {
                usedModel = fallbackModel ?? primaryModel;
            }

            _logger.LogInformation("[AIAnalysis] AI analysis completed - Used model: {UsedModel}, Error: {Error}", usedModel, error);
            return (analysis, error, errorMsg, usedModel);
        }

        private async Task<(string analysis, bool error, string? errorMsg)> ExecuteAnalysisWithModelAsync(
            string content, List<string> codingStandards, string requirements,
            string apiKey, string model, string language, bool isFileContent, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
                await _aiService.AnalyzeCodeAsync(content, codingStandards, requirements, apiKey, model, language, isFileContent),
                cancellationToken);
        }

        /// <summary>
        /// Checks if an error message indicates a rate-limiting issue.
        /// </summary>
        private static bool IsRateLimitError(string? errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;

            return errorMessage.Contains("429") ||
                   errorMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                   errorMessage.Contains("too many requests", StringComparison.OrdinalIgnoreCase);
        }
    }
}