using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using AICodeReviewer.Web.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Controllers
{
    /// <summary>
    /// API controller for AI model management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModelApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ModelApiController> _logger;

        public ModelApiController(IConfiguration configuration, ILogger<ModelApiController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get available AI models with display information
        /// </summary>
        /// <returns>List of available models with user-friendly display names</returns>
        [HttpGet("available")]
        public IActionResult GetAvailableModels()
        {
            try
            {
                _logger.LogInformation("🎯 ModelApiController.GetAvailableModels() called");
                
                // Get available models from configuration
                var availableModels = _configuration.GetSection("AvailableModels").Get<string[]>() ?? new string[0];
                _logger.LogInformation($"📋 Found {availableModels.Length} models in configuration");
                
                // Transform to display-friendly models
                var models = availableModels.Select(modelId => MapToDisplayModel(modelId)).ToList();
                
                _logger.LogInformation($"✅ Returning {models.Count} models");
                return Ok(new { success = true, models });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetAvailableModels");
                return StatusCode(500, new { success = false, error = $"Error loading models: {ex.Message}" });
            }
        }

        /// <summary>
        /// Map backend model ID to display-friendly model information
        /// </summary>
        private ModelInfo MapToDisplayModel(string modelId)
        {
            // Model display mapping based on the provided model IDs
            var displayMap = new Dictionary<string, ModelInfo>
            {
                ["qwen/qwen3-coder"] = new ModelInfo
                {
                    Id = "qwen/qwen3-coder",
                    Name = "Qwen3 Coder",
                    Provider = "Qwen",
                    Description = "Specialized for code analysis and review",
                    Icon = "🔍"
                },
                ["moonshotai/kimi-k2-0905"] = new ModelInfo
                {
                    Id = "moonshotai/kimi-k2-0905",
                    Name = "Kimi K2",
                    Provider = "Moonshot AI",
                    Description = "Advanced reasoning for complex code patterns",
                    Icon = "🌙"
                },
                ["qwen/qwen3-next-80b-a3b-instruct"] = new ModelInfo
                {
                    Id = "qwen/qwen3-next-80b-a3b-instruct",
                    Name = "Qwen3 Next 80B",
                    Provider = "Qwen",
                    Description = "Large model for comprehensive analysis",
                    Icon = "🚀"
                }
            };

            return displayMap.TryGetValue(modelId, out var modelInfo) 
                ? modelInfo 
                : new ModelInfo 
                { 
                    Id = modelId,
                    Name = modelId.Split('/').Last(),
                    Provider = "Unknown",
                    Description = "AI model for code analysis",
                    Icon = "🤖"
                };
        }
    }
}