namespace AICodeReviewer.Web.Models;

public record AnalysisExecutionRequest(
    string AnalysisId,
    string Content,
    List<string> SelectedDocuments,
    string DocumentsFolder,
    string ApiKey,
    string PrimaryModel,
    string? FallbackModel,
    string Language,
    bool IsFileContent,
    ISession Session
);