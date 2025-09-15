namespace AICodeReviewer.Web.Models;

public class AnalysisResult
{
    public string Status { get; set; } = "NotStarted";
    public string? Result { get; set; }
    public string? Error { get; set; }
    public bool IsComplete => Status == "Complete" || Status == "Error";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public string? RequestId { get; set; } // From HttpContext.TraceIdentifier if available
}