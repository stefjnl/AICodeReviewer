using System;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Domain.Interfaces;

public interface IBackgroundTaskService
{
    void ExecuteBackgroundTask(
        string taskName,
        string analysisId,
        Func<Task> taskFunc,
        Func<Exception, Task> errorHandler);
}