using AICodeReviewer.Web.Hubs;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Services;
using AICodeReviewer.Web.Application.Factories;
using AICodeReviewer.Web.Infrastructure.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Enrich configuration with local secrets and Azure Key Vault (when available)
builder.Configuration.AddUserSecrets<Program>(optional: true, reloadOnChange: true);
ConfigureAzureKeyVault(builder);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Disable antiforgery validation for AJAX endpoints
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization to use string enum conversion
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Also add explicit API controllers to ensure they're discovered
builder.Services.AddControllers();

builder.Services.AddHealthChecks();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy.WithOrigins("http://localhost:8097", "http://192.168.68.112:8097")
               .AllowAnyMethod()
               .AllowAnyHeader();
        });
});

// set routing to be case-insensitive:
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Add memory cache for session and other services
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Max 1000 entries (prevents memory bloat)
});

// Add distributed cache for session services
builder.Services.AddDistributedMemoryCache();

// Register custom services
builder.Services.AddScoped<IAIPromptResponseService, AICodeReviewer.Web.Infrastructure.Services.AIPromptResponseService>();

// Register new refactored services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IContentExtractionService, ContentExtractionService>();
builder.Services.AddScoped<IDocumentRetrievalService, DocumentRetrievalService>();
builder.Services.AddScoped<IAIAnalysisOrchestrator, AIAnalysisOrchestrator>();
builder.Services.AddScoped<IResultProcessorService, ResultProcessorService>();

// Register new specialized services
builder.Services.AddScoped<IAnalysisCacheService, AnalysisCacheService>();
builder.Services.AddScoped<IBackgroundTaskService, BackgroundTaskService>();
builder.Services.AddScoped<IAnalysisProgressService, AnalysisProgressService>();

// Register new coordinator services
builder.Services.AddScoped<IAnalysisPreparationService, AICodeReviewer.Web.Application.Services.AnalysisPreparationService>();
builder.Services.AddScoped<IAnalysisExecutionService, AICodeReviewer.Web.Application.Services.AnalysisExecutionService>();

// Register Application layer service as the main analysis service
builder.Services.AddScoped<IAnalysisService, AICodeReviewer.Web.Application.Services.AnalysisOrchestrationService>();
builder.Services.AddScoped<IAnalysisContextFactory, AnalysisContextFactory>();
builder.Services.AddScoped<IRepositoryManagementService, RepositoryManagementService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();
builder.Services.AddScoped<IPathValidationService, PathValidationService>();
builder.Services.AddScoped<ISignalRBroadcastService, SignalRBroadcastService>();
builder.Services.AddScoped<IDirectoryBrowsingService, DirectoryBrowsingService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IDiffProviderFactory, DiffProviderFactory>();
builder.Services.AddScoped<IDiffStatisticsParser, DiffStatisticsParser>();
builder.Services.AddSingleton<IOpenRouterSettingsProvider, OpenRouterSettingsProvider>();
builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    client.Timeout = TimeSpan.FromSeconds(120);
});

// Add SignalR
builder.Services.AddSignalR();

builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // For dev
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add session support for document management
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Startup sanity check for OpenRouter API key
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var openRouterSettings = app.Services.GetRequiredService<IOpenRouterSettingsProvider>();
var apiKey = openRouterSettings.GetApiKey();
var apiKeyExists = !string.IsNullOrWhiteSpace(apiKey);
var maskedPrefix = apiKey?.Length > 0 ? $"{apiKey.Substring(0, Math.Min(6, apiKey.Length))}..." : "";
logger.LogInformation("[OpenRouter] Startup check - API key exists: {Exists}; length: {Len}; startsWith(masked): {Prefix}",
    apiKeyExists, apiKey?.Length ?? 0, maskedPrefix);

app.UseCors("AllowLocalhost");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost"); // Add CORS middleware BEFORE routing
app.UseRouting();
app.UseSession(); // Enable session middleware

app.UseAuthorization();

// Configure default files (index.html, etc.)
app.UseDefaultFiles();

// Serve static files
app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // Map API controllers

app.MapHealthChecks("/health");

// After app.UseRouting()
app.MapHub<ProgressHub>("/hubs/progress");

app.Run();

static void ConfigureAzureKeyVault(WebApplicationBuilder builder)
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"];
    var keyVaultName = builder.Configuration["KeyVault:Name"] ?? builder.Configuration["Azure:KeyVaultName"];

    if (string.IsNullOrWhiteSpace(keyVaultUri) && !string.IsNullOrWhiteSpace(keyVaultName))
    {
        keyVaultUri = $"https://{keyVaultName}.vault.azure.net/";
    }

    if (string.IsNullOrWhiteSpace(keyVaultUri))
    {
        Console.WriteLine("[KeyVault] No Key Vault configuration detected. Skipping Key Vault provider.");
        return;
    }

    try
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = builder.Environment.IsProduction()
        });

        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential, new AzureKeyVaultConfigurationOptions
        {
            Manager = new OpenRouterKeyVaultSecretManager(),
            ReloadInterval = TimeSpan.FromMinutes(5)
        });

        Console.WriteLine($"[KeyVault] Loaded configuration from {keyVaultUri}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[KeyVault] Unable to load configuration from {keyVaultUri}: {ex.Message}");
    }
}
