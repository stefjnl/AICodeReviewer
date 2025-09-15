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
        string gitDiff, List<string> codingStandards, string requirements, string apiKey, string model)
    {
        try
        {
            if (string.IsNullOrEmpty(apiKey))
                return ("", true, "OpenRouter API key not configured");

            if (string.IsNullOrEmpty(gitDiff))
                return ("", true, "No code changes to analyze");

            var prompt = BuildPrompt(gitDiff, codingStandards, requirements);

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

    private static string BuildPrompt(string gitDiff, List<string> standards, string requirements)
    {
        var standardsText = standards?.Any() == true
            ? string.Join("\n- ", standards)
            : "No specific standards selected";

        return $@"Please review this .NET code diff and provide feedback.

SELECTED CODING STANDARDS:
- {standardsText}

REQUIREMENTS:
{requirements ?? "No specific requirements provided"}

CODE CHANGES:
{gitDiff}

Please provide:
1. Overall code quality assessment
2. Any violations of the selected coding standards
3. Suggestions for improvement
4. Severity level (Critical/Warning/Info) for each issue";
    }
}