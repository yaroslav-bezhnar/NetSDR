namespace NetSDR.Core.Extensions;

public static class TaskExtensions
{
    public static async Task<T?> WithTimeoutAsync<T>(this Task<T> task,
                                                     TimeSpan timeout,
                                                     CancellationTokenSource? linkedCts = null) where T : class
    {
        using var timeoutCts = new CancellationTokenSource();
        using var combined = linkedCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, linkedCts.Token)
            : timeoutCts;

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var completed = await Task.WhenAny(task, timeoutTask);

        if (completed == task)
        {
            timeoutCts.Cancel();
            return await task;
        }

        combined.Cancel();
        return null;
    }

    public static async Task<int?> WithTimeoutAsync(this Task<int> task,
                                                    TimeSpan timeout,
                                                    CancellationTokenSource? linkedCts = null)
    {
        using var timeoutCts = new CancellationTokenSource();
        using var combined = linkedCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, linkedCts.Token)
            : timeoutCts;

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var completed = await Task.WhenAny(task, timeoutTask);

        if (completed == task)
        {
            timeoutCts.Cancel();
            return await task;
        }

        combined.Cancel();
        return null;
    }

    public static async Task<bool> WithTimeoutAsync(this Task task,
                                                    TimeSpan timeout,
                                                    CancellationTokenSource? linkedCts = null)
    {
        using var timeoutCts = new CancellationTokenSource();
        using var combined = linkedCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, linkedCts.Token)
            : timeoutCts;

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var completed = await Task.WhenAny(task, timeoutTask);

        if (completed == task)
        {
            timeoutCts.Cancel();
            await task;
            return true;
        }

        combined.Cancel();
        return false;
    }
}