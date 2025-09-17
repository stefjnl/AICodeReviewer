using System.Reflection;
using AICodeReviewer.Web.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AICodeReviewer.Web.Infrastructure.Services;

/// <summary>
/// Service for loading embedded resources
/// </summary>
public class ResourceService : IResourceService
{
    private readonly ILogger<ResourceService> _logger;
    private static readonly Lazy<string> _promptTemplate = new(LoadPromptTemplate);
    private static readonly Lazy<string> _singleFilePromptTemplate = new(LoadSingleFilePromptTemplate);

    public ResourceService(ILogger<ResourceService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load the prompt template for git diff analysis
    /// </summary>
    /// <returns>The prompt template content</returns>
    public string GetPromptTemplate()
    {
        return _promptTemplate.Value;
    }

    /// <summary>
    /// Load the prompt template for single file analysis
    /// </summary>
    /// <returns>The single file prompt template content</returns>
    public string GetSingleFilePromptTemplate()
    {
        return _singleFilePromptTemplate.Value;
    }

    private static string LoadPromptTemplate()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "AICodeReviewer.Web.Resources.PromptTemplate.txt";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");
            }
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load prompt template from embedded resource", ex);
        }
    }

    private static string LoadSingleFilePromptTemplate()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "AICodeReviewer.Web.Resources.SingleFilePromptTemplate.txt";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");
            }
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load single file prompt template from embedded resource", ex);
        }
    }
}