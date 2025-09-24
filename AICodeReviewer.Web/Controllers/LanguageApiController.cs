using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguageApiController : ControllerBase
    {
        private static readonly Dictionary<string, LanguageInfo> SupportedLanguages = new()
        {
            ["dotnet"] = new LanguageInfo { Id = "dotnet", Name = ".NET/C#", Icon = "🔷", Extensions = new[] { ".cs", ".csproj", ".sln" } },
            ["python"] = new LanguageInfo { Id = "python", Name = "Python", Icon = "🐍", Extensions = new[] { ".py", ".pyx", ".pxd", ".pyi" } },
            ["javascript"] = new LanguageInfo { Id = "javascript", Name = "JavaScript/Node.js", Icon = "⚡", Extensions = new[] { ".js", ".ts", ".jsx", ".tsx", ".json", ".mjs" } },
            ["html"] = new LanguageInfo { Id = "html", Name = "Web (HTML/CSS)", Icon = "🌐", Extensions = new[] { ".html", ".htm", ".css", ".scss", ".sass", ".less" } },
            ["multi"] = new LanguageInfo { Id = "multi", Name = "Multi-language", Icon = "🔀", Extensions = new[] { "*" } }
        };

        [HttpGet("supported")]
        public IActionResult GetSupportedLanguages()
        {
            return Ok(new { languages = SupportedLanguages.Values });
        }

        [HttpPost("detect")]
        public IActionResult DetectLanguage([FromBody] DetectLanguageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RepositoryPath))
            {
                return BadRequest(new { error = "Repository path is required" });
            }

            try
            {
                if (!Directory.Exists(request.RepositoryPath))
                {
                    return NotFound(new { error = "Repository directory not found" });
                }

                var detectedLanguages = DetectLanguagesFromRepository(request.RepositoryPath);
                var primaryLanguage = detectedLanguages.FirstOrDefault() ?? "multi";

                return Ok(new
                {
                    detectedLanguages,
                    primaryLanguage,
                    fileCounts = GetFileCounts(request.RepositoryPath)
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while detecting the programming language. Please try again." });
            }
        }

        private List<string> DetectLanguagesFromRepository(string repositoryPath)
        {
            var fileCounts = new Dictionary<string, int>();
            var extensions = new HashSet<string>();

            // Scan all files in repository
            foreach (var file in Directory.EnumerateFiles(repositoryPath, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (!string.IsNullOrEmpty(extension))
                {
                    extensions.Add(extension);
                }
            }

            // Map extensions to languages
            var detectedLanguages = new List<string>();
            foreach (var language in SupportedLanguages.Values)
            {
                if (language.Id == "multi") continue; // Skip multi-language for detection

                if (language.Extensions.Any(ext => extensions.Contains(ext)))
                {
                    detectedLanguages.Add(language.Id);
                }
            }

            return detectedLanguages.Any() ? detectedLanguages : new List<string> { "multi" };
        }

        private Dictionary<string, int> GetFileCounts(string repositoryPath)
        {
            var fileCounts = new Dictionary<string, int>();

            foreach (var language in SupportedLanguages.Values)
            {
                if (language.Id == "multi") continue;

                var count = Directory.EnumerateFiles(repositoryPath, "*.*", SearchOption.AllDirectories)
                    .Count(file => language.Extensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

                if (count > 0)
                {
                    fileCounts[language.Id] = count;
                }
            }

            return fileCounts;
        }

        public class LanguageInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string[] Extensions { get; set; } = Array.Empty<string>();
        }

        public class DetectLanguageRequest
        {
            public string? RepositoryPath { get; set; }
        }
    }
}