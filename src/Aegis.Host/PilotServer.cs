using System.IO.Pipes;
using System.Threading.Channels;

namespace Aegis.Host;

public sealed class PilotServer : IDisposable
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    private readonly string _pipeName;
    private readonly ChannelWriter<HostMessage> _writer;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listener;

    public PilotServer(string session, ChannelWriter<HostMessage> writer)
    {
        _pipeName = PilotWire.PipeName(session);
        _writer = writer;
    }

    public void Start() => _listener ??= Task.Run(() => ListenAsync(_cts.Token));

    public void Stop() => _cts.Cancel();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                    1 << 16,
                    1 << 16);
                await pipe.WaitForConnectionAsync(ct);

                using var reader = new StreamReader(pipe, PilotWire.Utf8NoBom, leaveOpen: true);
                await using var writer = new StreamWriter(pipe, PilotWire.Utf8NoBom, leaveOpen: true)
                {
                    AutoFlush = true,
                };

                while (pipe.IsConnected && !ct.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync(ct);
                    if (line is null) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    await writer.WriteLineAsync(await DispatchAsync(line, ct));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                // The client vanished. The next loop accepts another connection.
            }
        }
    }

    private async Task<string> DispatchAsync(string line, CancellationToken ct)
    {
        PilotRequest? request;
        try
        {
            request = PilotWire.ParseRequest(line);
        }
        catch (Exception ex)
        {
            return PilotWire.Serialize(new PilotResponse { Ok = false, Error = $"bad request: {ex.Message}" });
        }

        if (request is null)
            return PilotWire.Serialize(new PilotResponse { Ok = false, Error = "empty request" });

        var completion = new TaskCompletionSource<PilotResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_writer.TryWrite(new HostMessage.Pilot(request, completion)))
            return PilotWire.Serialize(new PilotResponse { Ok = false, Error = "game loop is not accepting input" });

        try
        {
            PilotResponse response = await completion.Task.WaitAsync(CompletionTimeout, ct);
            return PilotWire.Serialize(response);
        }
        catch (TimeoutException)
        {
            return PilotWire.Serialize(new PilotResponse { Ok = false, Error = "game loop response timed out" });
        }
    }
}
