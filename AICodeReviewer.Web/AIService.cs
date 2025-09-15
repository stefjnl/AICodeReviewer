    using System.Text;
using System.Text.Json;

namespace AICodeReviewer.Web;

public static class AIService
{
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

            var prompt = language == "Python" ? BuildPromptPython(gitDiff, codingStandards, requirements) : BuildPromptNet(gitDiff, codingStandards, requirements);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are a senior .NET developer reviewing code changes. Provide concise, actionable feedback." },
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

    private static string BuildPromptNet(string gitDiff, List<string> standards, string requirements)
    {
        var standardsText = standards?.Any() == true
            ? string.Join("\n", standards)
            : "Follow general .NET best practices";

        return $@"You are a senior .NET code reviewer. Analyze this code diff and provide specific, actionable feedback.

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
            **Warning** Performance - Line 45: Synchronous database call blocks thread
            Suggestion: Replace .GetString() with .GetStringAsync() and add await

            **Style** Naming - Line 12: Variable name 'repositoryPath' doesn't follow conventions  
            Suggestion: Rename to 'repositoryPath' using camelCase for local variables

            **Critical** Security - Line 67: User input not validated before database query
            Suggestion: Add input validation and use parameterized queries

            FOCUS ON:
            - Specific line numbers and file paths when possible
            - Concrete, actionable suggestions
            - Violations of the provided coding standards
            - Security, performance, and maintainability issues

            AVOID:
            - General assessments or summaries
            - Theoretical explanations
            - Repetitive feedback about the same pattern";
    }

    private static string BuildPromptPython(string gitDiff, List<string> standards, string requirements)
    {
        var standardsText = standards?.Any() == true
            ? string.Join("\n", standards)
            : "Follow general Python best practices";

        return $@"You are a senior Python code reviewer. Analyze this code diff and provide specific, actionable feedback.

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
            **Warning** Performance - Line 45: Synchronous database call blocks thread
            Suggestion: Replace .get() with async version and add await

            **Style** Naming - Line 12: Variable name 'userName' doesn't follow snake_case conventions
            Suggestion: Rename to 'user_name' using snake_case for local variables

            **Critical** Security - Line 67: User input not validated before database query
            Suggestion: Add input validation and use parameterized queries

            FOCUS ON:
            - Specific line numbers and file paths when possible
            - Concrete, actionable suggestions
            - Violations of the provided coding standards
            - Security, performance, and maintainability issues
            - Python-specific best practices (PEP 8, type hints, async/await patterns)

            AVOID:
            - General assessments or summaries
            - Theoretical explanations
            - Repetitive feedback about the same pattern";
    }
}