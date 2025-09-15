namespace AICodeReviewer.Web.Models;

public class RunAnalysisRequest
{
    public string? RepositoryPath { get; set; }
    public List<string>? SelectedDocuments { get; set; }
    public string? DocumentsFolder { get; set; }
    public string? Language { get; set; }
    public string? AnalysisType { get; set; } // "uncommitted" or "commit"
    public string? CommitId { get; set; } // Only used when AnalysisType = "commit"
}