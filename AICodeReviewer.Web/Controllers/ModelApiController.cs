using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using AICodeReviewer.Web.Models;
using System.Collections.Generic;
using System.Linq;

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

        public ModelApiController(IConfiguration configuration)
        {
            _configuration = configuration;
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
                Console.WriteLine("🎯 ModelApiController.GetAvailableModels() called");
                
                // Get available models from configuration
                var availableModels = _configuration.GetSection("AvailableModels").Get<string[]>() ?? new string[0];
                Console.WriteLine($"📋 Found {availableModels.Length} models in configuration");
                
                // Transform to display-friendly models
                var models = availableModels.Select(modelId => MapToDisplayModel(modelId)).ToList();
                
                Console.WriteLine($"✅ Returning {models.Count} models");
                return Ok(new { success = true, models });
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"❌ Error in GetAvailableModels: {ex.Message}");
                return StatusCode(500, new { success = false, error = $"Error loading models: {ex.Message}" });
            }
        }

        /// <summary>
        /// Map backend model ID to display-friendly model information
        /// </summary>
        private ModelInfo MapToDisplayModel(string modelId)
        {
            // Get model configuration from appsettings.json
            var modelsSection = _configuration.GetSection("Models");
            var modelConfig = modelsSection.GetSection(modelId);

            if (modelConfig.Exists())
            {
                return new ModelInfo
                {
                    Id = modelId,
                    Name = modelConfig["name"] ?? modelId.Split('/').Last(),
                    Provider = modelConfig["provider"] ?? "Unknown",
                    Description = modelConfig["description"] ?? "AI model for code analysis",
                    Icon = modelConfig["icon"] ?? "🤖"
                };
            }

            // Fallback for unknown models
            return new ModelInfo
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