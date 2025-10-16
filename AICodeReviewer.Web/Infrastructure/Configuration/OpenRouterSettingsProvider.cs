using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Configuration;

public interface IOpenRouterSettingsProvider
{
    string? GetApiKey();
    string? GetModel();
    string? GetFallbackModel();
}

/// <summary>
/// Resolves OpenRouter configuration values from multiple sources (configuration, environment, Key Vault, user secrets).
/// </summary>
public class OpenRouterSettingsProvider : IOpenRouterSettingsProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterSettingsProvider> _logger;

    private static readonly string[] ApiKeyEnvironmentVariables =
    {
        "OPENROUTER_API_KEY",
        "OpenRouterApiKey",
        "OPEN_ROUTER_API_KEY",
        "OpenRouter:ApiKey"
    };

    public OpenRouterSettingsProvider(IConfiguration configuration, ILogger<OpenRouterSettingsProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetApiKey()
    {
        // Primary: configuration (appsettings, user secrets, Azure Key Vault, env with __)
        var apiKey = _configuration["OpenRouter:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            LogSource("configuration");
            return apiKey;
        }

        // Secondary: environment variables that might not use __
        foreach (var variable in ApiKeyEnvironmentVariables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value))
            {
                LogSource($"environment ({variable})");
                return value;
            }
        }

        _logger.LogWarning("[OpenRouter] API key not found in configuration or environment variables");
        return null;
    }

    public string? GetModel() => _configuration["OpenRouter:Model"];

    public string? GetFallbackModel() => _configuration["OpenRouter:FallbackModel"];

    private void LogSource(string source)
    {
        _logger.LogInformation("[OpenRouter] API key resolved via {Source}", source);
    }
}
