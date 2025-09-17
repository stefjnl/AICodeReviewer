namespace AICodeReviewer.Web.Models;

public class RunAnalysisRequest
{
    public string? RepositoryPath { get; set; }
    public List<string>? SelectedDocuments { get; set; }
    public string? DocumentsFolder { get; set; }
    public string? Language { get; set; }
    public string? AnalysisType { get; set; } // "uncommitted", "commit", or "singlefile"
    public string? CommitId { get; set; } // Only used when AnalysisType = "commit"
    public string? FilePath { get; set; } // Only used when AnalysisType = "singlefile"
    public string? FileContent { get; set; } // Optional: file content for single file analysis
    public string? Model { get; set; } // AI model to use for analysis
}