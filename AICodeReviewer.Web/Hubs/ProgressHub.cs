using Microsoft.AspNetCore.SignalR;

namespace AICodeReviewer.Web.Hubs;

public class ProgressHub : Hub
{
    public async Task JoinAnalysisGroup(string analysisId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, analysisId);
    }
    
    public async Task LeaveAnalysisGroup(string analysisId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, analysisId);
    }
}