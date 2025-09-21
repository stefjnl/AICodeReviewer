namespace AICodeReviewer.Web.Models;

public class RunAnalysisRequest
{
    public string? RepositoryPath { get; set; }
    public List<string>? SelectedDocuments { get; set; }
    public string? DocumentsFolder { get; set; }
    public string? Language { get; set; }
    public AnalysisType? AnalysisType { get; set; }
    public string? CommitId { get; set; } // Only used when AnalysisType = AnalysisType.Commit
    public string? SourceBranch { get; set; } // Only used when AnalysisType = AnalysisType.PullRequestDifferential
    public string? TargetBranch { get; set; } // Only used when AnalysisType = AnalysisType.PullRequestDifferential
    public string? FilePath { get; set; } // Only used when AnalysisType = AnalysisType.SingleFile
    public string? FileContent { get; set; } // Optional: file content for single file analysis
    public string? Model { get; set; } // AI model to use for analysis
}