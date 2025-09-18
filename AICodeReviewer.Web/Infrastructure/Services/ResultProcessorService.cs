using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Infrastructure.Services
{
    /// <summary>
    /// Service for processing and broadcasting AI analysis results.
    /// </summary>
    public class ResultProcessorService : IResultProcessorService
    {
        private readonly ILogger<ResultProcessorService> _logger;
        private readonly IAIPromptResponseService _aiPromptResponseService;
        private readonly ISignalRBroadcastService _signalRService;
        private readonly IMemoryCache _cache;

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
            .SetSize(1);

        public ResultProcessorService(
            ILogger<ResultProcessorService> logger,
            IAIPromptResponseService aiPromptResponseService,
            ISignalRBroadcastService signalRService,
            IMemoryCache cache)
        {
            _logger = logger;
            _aiPromptResponseService = aiPromptResponseService;
            _signalRService = signalRService;
            _cache = cache;
        }

        public async Task ProcessAndBroadcastAsync(
            string analysisId,
            string analysis,
            string? errorMessage,
            bool aiError,
            string usedModel,
            string fallbackModel,
            ISession session)
        {
            _logger.LogInformation("[ResultProcessor] Processing results for analysis {AnalysisId}, AI error: {AiError}", analysisId, aiError);

            if (!_cache.TryGetValue($"analysis_{analysisId}", out AnalysisResult? result))
            {
                _logger.LogError("[ResultProcessor] Analysis result not found in cache for {AnalysisId}", analysisId);
                return;
            }

            if (aiError)
            {
                result.Status = "Error";
                result.Error = $"AI analysis failed: {errorMessage}";
                result.CompletedAt = DateTime.UtcNow;

                await _signalRService.BroadcastErrorAsync(analysisId, $"AI analysis failed: {errorMessage}");
                _logger.LogError("[ResultProcessor] Broadcasted AI error for {AnalysisId}: {Error}", analysisId, errorMessage);
            }
            else
            {
                result.Status = "Complete";
                result.Result = analysis;
                result.CompletedAt = DateTime.UtcNow;

                // Parse the AI response into structured feedback items
                var feedback = _aiPromptResponseService.ParseAIResponse(analysis);
                _logger.LogInformation("[ResultProcessor] Parsed {Count} feedback items from AI response for {AnalysisId}", feedback.Count, analysisId);

                // Get the raw content from cache
                var rawContent = _cache.Get<string>($"content_{analysisId}") ?? string.Empty;

                // Create structured results object
                var analysisResults = new AnalysisResults
                {
                    AnalysisId = analysisId,
                    Feedback = feedback,
                    RawDiff = rawContent,
                    RawResponse = analysis,
                    CreatedAt = DateTime.UtcNow,
                    IsComplete = true,
                    Error = null
                };

                // Serialize the structured results to JSON for SignalR transmission
                var structuredResultsJson = JsonSerializer.Serialize(analysisResults, JsonSerializerOptions);

                await _signalRService.BroadcastCompleteWithModelAsync(analysisId, structuredResultsJson, session, usedModel, fallbackModel);

                _logger.LogInformation("[ResultProcessor] Broadcasted complete results for {AnalysisId} using model {UsedModel}", analysisId, usedModel);
            }

            // Store final result in cache
            _cache.Set($"analysis_{analysisId}", result, CacheEntryOptions);
            _logger.LogInformation("[ResultProcessor] Final result stored in cache for {AnalysisId}", analysisId);
        }
    }
}