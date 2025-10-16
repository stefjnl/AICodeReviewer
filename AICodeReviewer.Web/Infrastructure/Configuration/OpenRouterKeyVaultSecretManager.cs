using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace AICodeReviewer.Web.Infrastructure.Configuration;

/// <summary>
/// Maps Azure Key Vault secrets to configuration keys used by the application.
/// </summary>
public class OpenRouterKeyVaultSecretManager : KeyVaultSecretManager
{
    /// <summary>
    /// Only load enabled secrets to avoid pulling soft-deleted entries.
    /// </summary>
    public override bool Load(SecretProperties secret)
    {
        return secret.Enabled.GetValueOrDefault();
    }

    /// <summary>
    /// Translate secret names into hierarchical configuration keys.
    /// </summary>
    public override string? GetKey(KeyVaultSecret secret)
    {
        var normalizedName = secret.Name.Replace("--", ConfigurationPath.KeyDelimiter)
                                        .Replace("__", ConfigurationPath.KeyDelimiter);

        if (string.Equals(secret.Name, "OpenRouterApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return "OpenRouter:ApiKey";
        }

        return normalizedName;
    }
}
