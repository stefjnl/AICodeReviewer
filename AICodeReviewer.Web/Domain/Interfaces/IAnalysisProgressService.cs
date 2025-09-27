using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Domain.Interfaces;

public interface IAnalysisProgressService
{
    Task BroadcastProgressAsync(string analysisId, string status, string primaryModel, string fallbackModel);
    Task BroadcastProgressAsync(string analysisId, string message);
    Task BroadcastErrorAsync(string analysisId, string errorMessage);
    Task BroadcastCompletionAsync(string analysisId, string result, string? errorMessage, bool hasError, ISession session);
}