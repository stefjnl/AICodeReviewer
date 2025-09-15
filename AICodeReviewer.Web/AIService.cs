    using System.Text;
    using System.Text.Json;
    using AICodeReviewer.Web.Models;
    
    namespace AICodeReviewer.Web;

public class PromptTemplate
{
    public string Role { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string[] FileExtensions { get; set; } = Array.Empty<string>();
    public string NamingConvention { get; set; } = string.Empty;
    public string AsyncPattern { get; set; } = string.Empty;
    public string BestPractices { get; set; } = string.Empty;
}

public static class AIService
{
    private static readonly Dictionary<SupportedLanguage, PromptTemplate> _languageTemplates = new()
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
        }
    };

    private static readonly HttpClient _httpClient = new HttpClient()
    {
        BaseAddress = new Uri("https://openrouter.ai/api/v1/"),
        Timeout = TimeSpan.FromSeconds(120)
    };

    public static async Task<(string analysis, bool isError, string? errorMessage)> AnalyzeCodeAsync(
        string gitDiff, List<string> codingStandards, string requirements, string apiKey, string model, string language)
    {
        try
        {
            if (string.IsNullOrEmpty(apiKey))
                return ("", true, "OpenRouter API key not configured");

            if (string.IsNullOrEmpty(gitDiff))
                return ("", true, "No code changes to analyze");

            // Convert string language to enum
            var languageEnum = Enum.TryParse<SupportedLanguage>(language, true, out var parsedLanguage)
                ? parsedLanguage
                : SupportedLanguage.NET; // Default fallback

            var prompt = BuildPrompt(gitDiff, codingStandards, requirements, languageEnum);

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
            return ("", true, $"Network timeout after 30 seconds: {tce.Message}");
        }
        catch (HttpRequestException hre)
        {
            return ("", true, $"Network error: {hre.Message}");
        }
        catch (JsonException je)
        {
            return ("", true, $"JSON parsing error: {je.Message}");
        }
        catch (Exception ex)
        {
            return ("", true, $"Unexpected error: {ex.Message}");
        }
    }

    private static string BuildPrompt(string gitDiff, List<string> standards, string requirements, SupportedLanguage language)
    {
        if (!_languageTemplates.TryGetValue(language, out var template))
        {
            template = _languageTemplates[SupportedLanguage.NET]; // Default fallback
        }

        var standardsText = standards?.Any() == true
            ? string.Join("\n", standards)
            : $"Follow general {template.LanguageName} best practices";

        return $@"Review this {template.LanguageName} code diff. Provide feedback using EXACTLY this format:

            **Critical** [Category] - [FileName] Line [X]: [Issue description]
            Suggestion: [Specific fix]

            **Warning** [Category] - [FileName] Line [X]: [Issue description]  
            Suggestion: [Specific fix]

            **Suggestion** [Category] - [FileName] Line [X]: [Issue description]
            Suggestion: [Specific fix]

            **Style** [Category] - [FileName] Line [X]: [Issue description]
            Suggestion: [Specific fix]

            CODING STANDARDS:
            {standardsText}

            REQUIREMENTS:
            {requirements ?? "No requirements provided"}

            DIFF:
            {gitDiff}

            Categories: Security, Performance, Error Handling, Architecture, Style
            Order: Critical issues first, then Warning, Suggestion, Style
            Include filename before line number for every issue.";
    }
}