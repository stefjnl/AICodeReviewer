namespace AICodeReviewer.Web.Models;

public class RunAnalysisRequest
{
    public string? RepositoryPath { get; set; }
    public List<string>? SelectedDocuments { get; set; }
    public string? DocumentsFolder { get; set; }
}