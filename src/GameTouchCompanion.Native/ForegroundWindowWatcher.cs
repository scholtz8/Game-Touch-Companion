namespace GameTouchCompanion.Native;

public sealed class ForegroundWindowWatcher(IForegroundWindowService service) : IDisposable
{
    private CancellationTokenSource? cancellation;
    public event EventHandler<nint>? ForegroundChanged;

    public void Start(TimeSpan interval)
    {
        if (cancellation is not null) return;
        cancellation = new CancellationTokenSource();
        _ = WatchAsync(interval, cancellation.Token);
    }

    private async Task WatchAsync(TimeSpan interval, CancellationToken token)
    {
        var previous = nint.Zero;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var current = service.GetForegroundWindow();
                if (current != previous)
                {
                    previous = current;
                    ForegroundChanged?.Invoke(this, current);
                }
                await Task.Delay(interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }
}
