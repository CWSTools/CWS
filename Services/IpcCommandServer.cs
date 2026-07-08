using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gallery.Services;

public sealed class IpcCommandServer : IDisposable
{
    public const string PipeName = "CWSTool.Ipc.v1";

    private readonly Func<string, Task> _handleCommandAsync;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _serverTask;

    public IpcCommandServer(Func<string, Task> handleCommandAsync)
    {
        _handleCommandAsync = handleCommandAsync;
    }

    public void Start()
    {
        if (_serverTask is not null)
        {
            return;
        }

        _serverTask = Task.Run(() => RunAsync(_cancellation.Token));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await pipe.WaitForConnectionAsync(cancellationToken);

                using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
                await using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true)
                {
                    AutoFlush = true
                };

                var command = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(command))
                {
                    await _handleCommandAsync(command);
                }

                await writer.WriteLineAsync("OK");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLoggerService.Error("ipc", ex, "IPC command server failed.");
                await Task.Delay(250, cancellationToken).ContinueWith(_ => { }, TaskScheduler.Default);
            }
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }
}
