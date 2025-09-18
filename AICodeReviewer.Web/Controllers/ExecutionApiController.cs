using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AICodeReviewer.Web.Controllers
{
    [ApiController]
    [Route("api/execution")]
    public class ExecutionApiController : ControllerBase
    {
        private readonly IAnalysisService _analysisService;
        private readonly ILogger<ExecutionApiController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ExecutionApiController(
            IAnalysisService analysisService,
            ILogger<ExecutionApiController> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _analysisService = analysisService;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        /// <summary>
        /// Start code analysis with the selected parameters from workflow steps 1-5
        /// </summary>
        /// <param name="request">Analysis configuration from frontend</param>
        /// <returns>Analysis ID and success status</returns>
        [HttpPost("start")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StartAnalysis([FromBody] RunAnalysisRequest request)
        {
            try
            {
                _logger.LogInformation("Starting analysis with parameters: {Request}", 
                    JsonSerializer.Serialize(request));

                // Validate required parameters
                if (string.IsNullOrWhiteSpace(request.RepositoryPath))
                {
                    return BadRequest(new { success = false, error = "Repository path is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    return BadRequest(new { success = false, error = "AI model selection is required" });
                }

                // Start the analysis
                var (analysisId, success, error) = await _analysisService.StartAnalysisAsync(
                    request, HttpContext.Session, _environment, _configuration);

                if (success)
                {
                    _logger.LogInformation("Analysis started successfully with ID: {AnalysisId}", analysisId);
                    return Ok(new { success = true, analysisId });
                }
                else
                {
                    _logger.LogError("Failed to start analysis: {Error}", error);
                    return BadRequest(new { success = false, error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting analysis execution");
                return StatusCode(500, new { success = false, error = "Internal server error occurred" });
            }
        }
    }
}