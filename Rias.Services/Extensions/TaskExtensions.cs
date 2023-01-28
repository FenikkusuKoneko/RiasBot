using Microsoft.Extensions.Logging;

namespace Rias.Services.Extensions;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, ILogger logger)
    {
        task.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                logger.LogError(t.Exception, "Exception thrown in a background task");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}