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

        var role = template.Role;
        var languageName = template.LanguageName;
        var namingConvention = template.NamingConvention;
        var asyncPattern = template.AsyncPattern;
        var bestPractices = template.BestPractices;

        var exampleResponse = language switch
        {
            SupportedLanguage.Python => @"**Warning** Performance - Line 45: Synchronous database call blocks thread
            Suggestion: Replace .get() with async version and add await

            **Style** Naming - Line 12: Variable name 'userName' doesn't follow snake_case conventions
            Suggestion: Rename to 'user_name' using snake_case for local variables",

            _ => @"**Warning** Performance - Line 45: Synchronous database call blocks thread
            Suggestion: Replace .GetString() with .GetStringAsync() and add await

            **Style** Naming - Line 12: Variable name 'repositoryPath' doesn't follow conventions
            Suggestion: Rename to 'repositoryPath' using camelCase for local variables"
        };

        return $@"You are a {role} reviewing code changes. Analyze this code diff and provide specific, actionable feedback.

            CODING STANDARDS TO ENFORCE:
            {standardsText}

            REQUIREMENTS CONTEXT:
            {requirements ?? "No specific requirements provided"}

            CODE CHANGES TO REVIEW:
            {gitDiff}

            RESPONSE FORMAT REQUIRED:
            For each issue found, use this exact format:

            **[SEVERITY]** [Category] - Line [number]: [Brief description]
            Suggestion: [Specific actionable fix]

            SEVERITY OPTIONS: Critical, Warning, Suggestion, Style
            CATEGORY OPTIONS: Security, Performance, Error Handling, Style, Architecture

            EXAMPLE RESPONSES:
            {exampleResponse}

            **Critical** Security - Line 67: User input not validated before database query
            Suggestion: Add input validation and use parameterized queries

            FOCUS ON:
            - Specific line numbers and file paths when possible
            - Concrete, actionable suggestions
            - Violations of the provided coding standards
            - Security, performance, and maintainability issues
            - {languageName}-specific best practices ({bestPractices})

            AVOID:
            - General assessments or summaries
            - Theoretical explanations
            - Repetitive feedback about the same pattern";
    }
}