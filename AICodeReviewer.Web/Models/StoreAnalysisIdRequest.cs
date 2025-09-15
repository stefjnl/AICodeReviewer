namespace AICodeReviewer.Web.Models;

/// <summary>
/// Request model for storing analysis ID in session
/// </summary>
public class StoreAnalysisIdRequest
{
    public string AnalysisId { get; set; } = string.Empty;
}