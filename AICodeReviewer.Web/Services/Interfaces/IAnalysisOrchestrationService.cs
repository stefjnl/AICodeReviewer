using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Services;

public interface IAnalysisOrchestrationService
{
    Task<string> StartAnalysisAsync(string repositoryPath, List<string> selectedDocuments, string documentsFolder, 
        string apiKey, string model, string language, string analysisType, string? commitId, string? filePath);
    
    Task<AnalysisResult?> GetAnalysisStatusAsync(string analysisId);
    Task RunBackgroundAnalysisAsync(string analysisId, string repositoryPath, List<string> selectedDocuments, 
        string documentsFolder, string apiKey, string model, string language, string analysisType, 
        string? commitId, string? filePath);
}