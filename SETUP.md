# AI Code Reviewer - Setup Instructions

## Configuration Setup

### Step 1: Configure your OpenRouter API key (local development)

Use [ASP.NET Core user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) so the key never touches disk:

```powershell
cd AICodeReviewer.Web
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-v1-your-actual-api-key"
```

You can verify the value later with:

```powershell
dotnet user-secrets list
```

> The project now includes a `UserSecretsId`, so no additional initialization is required.

Alternatively, you can export the key via environment variables. The application checks the following names (in order): `OpenRouter:ApiKey`, `OPENROUTER_API_KEY`, `OpenRouterApiKey`, and `OPEN_ROUTER_API_KEY`.

### Step 2: Optional – fallback appsettings

If you prefer a file-based configuration for non-sensitive values, copy the template:

```powershell
Copy-Item AICodeReviewer.Web/appsettings.template.json AICodeReviewer.Web/appsettings.json
```

and update non-secret settings as needed. Leave `OpenRouter:ApiKey` blank so secrets remain outside source control.

### Step 3: Get your OpenRouter API key
1. Visit https://openrouter.ai/
2. Sign up for an account
3. Generate an API key from your dashboard
4. Copy the key and paste it in your appsettings.json file

## Security Notes
- **Never commit your API key** to version control
- **appsettings.json is gitignored** to protect your credentials
- **Use environment variables** in production environments
- **Rotate your API keys** regularly for security

## Troubleshooting
If you see "No auth credentials found" errors, ensure:
1. Your API key is correctly formatted (starts with `sk-or-v1-`)
2. `dotnet user-secrets list` shows an entry for `OpenRouter:ApiKey`
3. Your OpenRouter account has sufficient credits

## Azure Key Vault integration

When running in Azure (or locally with `az login`), the application can pull secrets directly from Key Vault. Set either of these configuration values:

- `KeyVault:Uri` (preferred) – e.g. `https://codeguard-ss-kv.vault.azure.net/`
- `KeyVault:Name` – the vault name; the app infers the URI automatically

> The existing `deploy/azure/main.bicep` template already provisions a secret named `OpenRouterApiKey`. The application maps that secret to `OpenRouter:ApiKey` automatically.

## Running the Application
```bash
cd AICodeReviewer.Web
dotnet run
```

Visit http://localhost:8097 to use the application.