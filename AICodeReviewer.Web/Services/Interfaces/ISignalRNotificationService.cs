namespace AICodeReviewer.Web.Services;

public interface ISignalRNotificationService
{
    Task BroadcastProgress(string analysisId, string status);
    Task BroadcastComplete(string analysisId, string result);
    Task BroadcastError(string analysisId, string error);
}