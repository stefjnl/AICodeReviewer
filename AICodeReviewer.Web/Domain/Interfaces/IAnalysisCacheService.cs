using AICodeReviewer.Web.Models;

namespace AICodeReviewer.Web.Domain.Interfaces;

public interface IAnalysisCacheService
{
    void StoreAnalysisResult(string analysisId, AnalysisResult result);
    AnalysisResult? GetAnalysisResult(string analysisId);
    void StoreContent(string analysisId, string content);
    void StoreIsFileContent(string analysisId, bool isFileContent);
    string? GetContent(string analysisId);
    bool? GetIsFileContent(string analysisId);
    void UpdateAnalysisStatus(string analysisId, string status);
}