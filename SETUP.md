# AI Code Reviewer - Setup Instructions

## Configuration Setup

### Step 1: Create your appsettings.json file
Copy the template and add your API key:

```bash
cp AICodeReviewer.Web/appsettings.template.json AICodeReviewer.Web/appsettings.json
```

### Step 2: Add your OpenRouter API key
Edit `AICodeReviewer.Web/appsettings.json` and replace `YOUR_API_KEY_HERE` with your actual OpenRouter API key:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "OpenRouter": {
    "ApiKey": "sk-or-v1-your-actual-api-key-here",
    "Model": "moonshotai/kimi-k2-0905"
  }
}
```

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
2. The appsettings.json file exists in the AICodeReviewer.Web folder
3. The file contains valid JSON syntax
4. Your OpenRouter account has sufficient credits

## Running the Application
```bash
cd AICodeReviewer.Web
dotnet run
```

Visit http://localhost:8097 to use the application.