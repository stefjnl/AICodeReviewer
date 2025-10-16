using AICodeReviewer.Web.Domain;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Models;
using System.Text;
using System.Text.Json;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for AI-powered code analysis operations
/// </summary>
public class AIService : IAIService
{
    private readonly ILogger<AIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IResourceService _resourceService;
    private readonly Dictionary<SupportedLanguage, PromptTemplate> _languageTemplates;

    public AIService(ILogger<AIService> logger, HttpClient httpClient, IResourceService resourceService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _resourceService = resourceService;

        _languageTemplates = new Dictionary<SupportedLanguage, PromptTemplate>
        {
            [SupportedLanguage.NET] = new PromptTemplate
            {
                Role = "senior .NET developer",
                LanguageName = ".NET",
                FileExtensions = new[] { ".cs", ".vb" },
                NamingConvention = "PascalCase for public members, camelCase for local variables",
                AsyncPattern = "async/await",
                BestPractices = "SOLID principles, dependency injection, proper exception handling"
            },
            [SupportedLanguage.Python] = new PromptTemplate
            {
                Role = "senior Python developer",
                LanguageName = "Python",
                FileExtensions = new[] { ".py" },
                NamingConvention = "snake_case for variables and functions",
                AsyncPattern = "async/await with asyncio",
                BestPractices = "PEP 8 standards, type hints, proper exception handling"
            },
            [SupportedLanguage.JavaScript] = new PromptTemplate
            {
                Role = "senior JavaScript/TypeScript developer",
                LanguageName = "JavaScript/TypeScript",
                FileExtensions = new[] { ".js", ".ts", ".jsx", ".tsx" },
                NamingConvention = "camelCase for variables/functions, PascalCase for classes/components",
                AsyncPattern = "async/await with Promises",
                BestPractices = "ES6+ features, type safety with TypeScript, proper error handling, module patterns"
            },
            [SupportedLanguage.HTML] = new PromptTemplate
            {
                Role = "senior frontend developer",
                LanguageName = "HTML/CSS",
                FileExtensions = new[] { ".html", ".htm", ".css", ".scss", ".sass", ".less" },
                NamingConvention = "kebab-case for CSS classes, semantic HTML elements",
                AsyncPattern = "N/A (markup/styles are not async languages)",
                BestPractices = "Semantic HTML, accessibility (a11y), responsive design, CSS specificity management, browser compatibility, performance optimization"
            }
        };
    }

    public async Task<(string analysis, bool isError, string? errorMessage)> AnalyzeCodeAsync(
        string content, List<string> codingStandards, string requirements, string apiKey, string model, string language, bool isFileContent = false)
    {
        try
        {
            var apiKeyExists = !string.IsNullOrWhiteSpace(apiKey);
            var maskedPrefix = apiKey?.Length > 0 ? $"{apiKey.Substring(0, Math.Min(6, apiKey.Length))}..." : "";
            _logger.LogDebug("[OpenRouter] Per-call check - API key exists: {Exists}; length: {Len}; startsWith(masked): {Prefix}",
                apiKeyExists, apiKey?.Length ?? 0, maskedPrefix);
            if (string.IsNullOrEmpty(apiKey))
                return ("", true, "OpenRouter API key not configured");

            if (string.IsNullOrEmpty(content))
                return ("", true, isFileContent ? "No file content to analyze" : "No code changes to analyze");

            // Convert string language to enum with proper mapping
            var languageEnum = MapLanguageStringToEnum(language);

            var prompt = BuildPrompt(content, codingStandards, requirements, languageEnum, isFileContent);

            var template = _languageTemplates.TryGetValue(languageEnum, out var langTemplate)
                ? langTemplate
                : _languageTemplates[SupportedLanguage.NET];

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = $"You are a {template.Role} reviewing code changes. Provide concise, actionable feedback." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 1500
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost:8097");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "AI Code Reviewer");

            var response = await _httpClient.PostAsync("chat/completions", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return ("", true, $"HTTP {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);

            var analysis = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No analysis returned";

            return (analysis, false, null);
        }
        catch (TaskCanceledException tce)
        {
            _logger.LogError(tce, "Network timeout during AI analysis");
            return ("", true, "Network timeout during AI analysis. Please try again.");
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, "Network error during AI analysis");
            return ("", true, "Network error during AI analysis. Please check your connection and try again.");
        }
        catch (JsonException je)
        {
            _logger.LogError(je, "JSON parsing error during AI analysis");
            return ("", true, "Error processing AI response. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI analysis");
            return ("", true, "An unexpected error occurred during analysis. Please try again.");
        }
    }

    private SupportedLanguage MapLanguageStringToEnum(string language)
    {
        // Map frontend language IDs to backend enum values
        return language?.ToLowerInvariant() switch
        {
            "dotnet" => SupportedLanguage.NET,
            "python" => SupportedLanguage.Python,
            "javascript" => SupportedLanguage.JavaScript,
            "html" => SupportedLanguage.HTML,
            _ => SupportedLanguage.NET // Default fallback
        };
    }

    private string BuildPrompt(string content, List<string> standards, string requirements, SupportedLanguage language, bool isFileContent = false)
    {
        if (!_languageTemplates.TryGetValue(language, out var template))
        {
            template = _languageTemplates[SupportedLanguage.NET]; // Default fallback
        }

        var standardsText = standards?.Any() == true
            ? string.Join("\n", standards)
            : $"Follow general {template.LanguageName} best practices";

        // Choose the appropriate template based on content type
        var promptTemplate = isFileContent ? _resourceService.GetSingleFilePromptTemplate() : _resourceService.GetPromptTemplate();
        
        return promptTemplate
            .Replace("{LanguageName}", template.LanguageName)
            .Replace("{StandardsText}", standardsText)
            .Replace("{Requirements}", requirements ?? "No requirements provided")
            .Replace("{GitDiff}", content);
    }
}