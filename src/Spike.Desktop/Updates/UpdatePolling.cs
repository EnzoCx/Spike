namespace Spike.Desktop.Updates;

internal static class UpdatePolling
{
    internal static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    internal static async Task RunAsync(Func<Task> check, CancellationToken cancellation, TimeSpan? interval = null)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                await check();
                // Wait after completion: slow downloads never overlap or trigger catch-up bursts.
                await Task.Delay(interval ?? Interval, cancellation);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }
}
