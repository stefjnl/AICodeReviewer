using AICodeReviewer.Web.Hubs;
using AICodeReviewer.Web.Services;
using AICodeReviewer.Web.Domain.Interfaces;
using AICodeReviewer.Web.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Max 1000 entries (prevents memory bloat)
});

// Register custom services
builder.Services.AddScoped<AIPromptResponseService>();

// Register domain services
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<IRepositoryManagementService, RepositoryManagementService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();
builder.Services.AddScoped<IPathValidationService, PathValidationService>();
builder.Services.AddScoped<ISignalRBroadcastService, SignalRBroadcastService>();
builder.Services.AddScoped<IDirectoryBrowsingService, DirectoryBrowsingService>();

// In service registration section
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

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// After app.UseRouting()
app.MapHub<ProgressHub>("/hubs/progress");

app.Run();
