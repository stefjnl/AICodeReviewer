using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using AICodeReviewer.Web.Infrastructure.Configuration;
using AICodeReviewer.Web.Infrastructure.Extensions;
using AICodeReviewer.Web.Domain.Interfaces;

namespace AICodeReviewer.Web.Application.Factories;

public record AnalysisContext(
    string RepositoryPath,
    List<string> SelectedDocuments,
    string DocumentsFolder,
    string Language,
    AnalysisType AnalysisType,
    string ApiKey,
    string Model,
    string FallbackModel
);

public interface IAnalysisContextFactory
{
    AnalysisContext Create(RunAnalysisRequest request, ISession session, IWebHostEnvironment environment, IConfiguration configuration);
}

public class AnalysisContextFactory : IAnalysisContextFactory
{
    private readonly IPathValidationService _pathService;
    private readonly IOpenRouterSettingsProvider _openRouterSettingsProvider;

    public AnalysisContextFactory(IPathValidationService pathService, IOpenRouterSettingsProvider openRouterSettingsProvider)
    {
        _pathService = pathService;
        _openRouterSettingsProvider = openRouterSettingsProvider;
    }

    public AnalysisContext Create(RunAnalysisRequest request, ISession session, IWebHostEnvironment environment, IConfiguration configuration)
    {
        var defaultRepositoryPath = Path.Combine(environment.ContentRootPath, "..");
        var repositoryPath = request.RepositoryPath ?? session.GetString(SessionKeys.RepositoryPath) ?? defaultRepositoryPath;
        var selectedDocuments = request.SelectedDocuments ?? session.GetObject<List<string>>(SessionKeys.SelectedDocuments) ?? new List<string>();
        var documentsFolder = ResolveDocumentsFolder(request, session, environment.ContentRootPath);
        var language = request.Language ?? session.GetString(SessionKeys.Language) ?? "NET";
        var analysisType = request.AnalysisType ?? AnalysisType.Uncommitted;
        var apiKey = _openRouterSettingsProvider.GetApiKey() ?? "";
        var model = request.Model ?? _openRouterSettingsProvider.GetModel() ?? configuration["OpenRouter:Model"] ?? "";
        var fallbackModel = _openRouterSettingsProvider.GetFallbackModel() ?? configuration["OpenRouter:FallbackModel"] ?? "";

        return new AnalysisContext(repositoryPath, selectedDocuments, documentsFolder, language, analysisType, apiKey, model, fallbackModel);
    }

    private string ResolveDocumentsFolder(RunAnalysisRequest request, ISession session, string contentRootPath)
    {
        var defaultDocumentsFolder = _pathService.GetDocumentsFolderPath(contentRootPath);

        if (!string.IsNullOrWhiteSpace(request.DocumentsFolder))
        {
            var requestedPath = request.DocumentsFolder;
            if (!Path.IsPathRooted(requestedPath))
            {
                // Ensure relative overrides resolve against the default documents folder
                requestedPath = Path.GetFullPath(Path.Combine(defaultDocumentsFolder, requestedPath));
            }

            return requestedPath;
        }

        var sessionDocuments = session.GetString(SessionKeys.DocumentsFolder);
        if (!string.IsNullOrWhiteSpace(sessionDocuments))
        {
            return sessionDocuments;
        }

        return defaultDocumentsFolder;
    }
}