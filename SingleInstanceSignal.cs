using System.IO.Pipes;

namespace FF14DiscordRelay;

internal sealed class SingleInstanceSignal : IDisposable
{
    private const string PipeName = "FF14DiscordRelay.SingleInstance.ShowSettings";
    private readonly CancellationTokenSource _cts = new();
    private int _pendingShowRequests;

    public Action? ShowRequested { get; set; }

    public SingleInstanceSignal()
    {
        _ = Task.Run(ListenLoopAsync);
    }

    public static bool NotifyExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(800);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine("show");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ConsumePendingShowRequests()
    {
        return Interlocked.Exchange(ref _pendingShowRequests, 0) > 0;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(_cts.Token);
                using var reader = new StreamReader(server);
                var command = await reader.ReadLineAsync(_cts.Token);
                if (command?.Equals("show", StringComparison.OrdinalIgnoreCase) == true)
                    RequestShow();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                await Task.Delay(250, _cts.Token).ContinueWith(_ => { }, TaskScheduler.Default);
            }
        }
    }

    private void RequestShow()
    {
        var handler = ShowRequested;
        if (handler is null)
        {
            Interlocked.Increment(ref _pendingShowRequests);
            return;
        }

        handler();
    }
}
