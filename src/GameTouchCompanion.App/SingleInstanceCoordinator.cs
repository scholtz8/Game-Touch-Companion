using System.IO;
using System.IO.Pipes;
using System.Text;
using Serilog;

namespace GameTouchCompanion.App;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private const string DefaultInstanceName = "GameTouchCompanion.App";
    private readonly Mutex mutex;
    private readonly string pipeName;
    private readonly CancellationTokenSource cancellation = new();
    private Task? listenerTask;
    private bool ownsMutex;
    private bool disposed;

    public SingleInstanceCoordinator(string? instanceName = null)
    {
        var suffix = string.IsNullOrWhiteSpace(instanceName) ? DefaultInstanceName : instanceName.Trim();
        var safe = string.Concat(suffix.Select(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_'));
        pipeName = $"{safe}.Activation";
        mutex = new Mutex(
            initiallyOwned: false,
            name: $"Local\\{safe}.Singleton",
            createdNew: out var createdNew);
        ownsMutex = createdNew;
    }

    public bool IsPrimary => ownsMutex;

    public void StartListening(Func<string, Task> commandHandler)
    {
        ArgumentNullException.ThrowIfNull(commandHandler);
        if (!IsPrimary) throw new InvalidOperationException("Only the primary instance can listen for activation requests.");
        if (listenerTask is not null) return;
        listenerTask = Task.Run(() => ListenLoopAsync(commandHandler, cancellation.Token));
    }

    public async Task<bool> SignalPrimaryAsync(string command, CancellationToken cancellationToken = default)
    {
        if (IsPrimary || string.IsNullOrWhiteSpace(command)) return false;

        for (var attempt = 0; attempt < 12; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromMilliseconds(250));
                await client.ConnectAsync(timeout.Token).ConfigureAwait(false);
                await using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: false) { AutoFlush = true };
                await writer.WriteLineAsync(command.AsMemory(), cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The primary process may still be starting. Retry briefly without ever launching a duplicate UI.
            }
            catch (IOException)
            {
                // Same startup race as above.
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task ListenLoopAsync(Func<string, Task> commandHandler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
                var command = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(command)) await commandHandler(command).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Single-instance activation pipe failed; listener will continue");
                try { await Task.Delay(250, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        cancellation.Cancel();
        cancellation.Dispose();
        ownsMutex = false;
        mutex.Dispose();
    }
}
