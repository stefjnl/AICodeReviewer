using AICodeReviewer.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using AICodeReviewer.Web.Infrastructure.Extensions;

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
    public AnalysisContext Create(RunAnalysisRequest request, ISession session, IWebHostEnvironment environment, IConfiguration configuration)
    {
        var defaultRepositoryPath = Path.Combine(environment.ContentRootPath, "..");
        var repositoryPath = request.RepositoryPath ?? session.GetString(SessionKeys.RepositoryPath) ?? defaultRepositoryPath;
        var selectedDocuments = request.SelectedDocuments ?? session.GetObject<List<string>>(SessionKeys.SelectedDocuments) ?? new List<string>();
        // Look in the solution root (parent of ContentRootPath) for Documents folder
        var solutionRoot = Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath;
        var documentsFolder = !string.IsNullOrEmpty(request.DocumentsFolder) ? request.DocumentsFolder : session.GetString(SessionKeys.DocumentsFolder) ?? Path.Combine(solutionRoot, "Documents");
        var language = request.Language ?? session.GetString(SessionKeys.Language) ?? "NET";
        var analysisType = request.AnalysisType ?? AnalysisType.Uncommitted;
        var apiKey = configuration["OpenRouter:ApiKey"] ?? "";
        var model = request.Model ?? configuration["OpenRouter:Model"] ?? "";
        var fallbackModel = configuration["OpenRouter:FallbackModel"] ?? "";

        return new AnalysisContext(repositoryPath, selectedDocuments, documentsFolder, language, analysisType, apiKey, model, fallbackModel);
    }
}