using System.IO.Pipes;
using System.Text;
using System.Threading.Channels;

namespace Aegis.Cli;

/// <summary>
/// Named-pipe control server. One connection at a time, any number of request lines
/// per connection. Requests are marshalled onto the game loop's channel, so the pilot
/// never touches game state off-thread.
/// </summary>
public sealed class PilotServer
{
    private readonly string _pipeName;
    private readonly ChannelWriter<HostMessage> _writer;
    private readonly CancellationTokenSource _cts = new();

    public PilotServer(string session, ChannelWriter<HostMessage> writer)
    {
        _pipeName = PilotWire.PipeName(session);
        _writer = writer;
    }

    public void Start() => _ = Task.Run(() => ListenAsync(_cts.Token));

    public void Stop() => _cts.Cancel();

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Trace("creating pipe instance");
                // Non-zero buffers matter: zero-size pipe buffers make every write block
                // until the peer posts a read, which deadlocks two ends that both write first.
                await using var pipe = new NamedPipeServerStream(
                    _pipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                    inBufferSize: 1 << 16, outBufferSize: 1 << 16);
                Trace("waiting for connection");
                await pipe.WaitForConnectionAsync(ct);
                Trace("client connected");

                using var reader = new StreamReader(pipe, PilotWire.Utf8NoBom, leaveOpen: true);
                await using var writer = new StreamWriter(pipe, PilotWire.Utf8NoBom, leaveOpen: true) { AutoFlush = true };

                while (pipe.IsConnected && !ct.IsCancellationRequested)
                {
                    Trace("reading line");
                    string? line = await reader.ReadLineAsync(ct);
                    Trace($"read: {line ?? "<null>"}");
                    if (line is null) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var response = await DispatchAsync(line);
                    Trace($"responding: {response}");
                    await writer.WriteLineAsync(response);
                    Trace("responded");
                }
                Trace("connection loop ended");
            }
            catch (OperationCanceledException)
            {
                Trace("cancelled");
                break;
            }
            catch (IOException ex)
            {
                Trace($"io exception: {ex.Message}");
                // Client vanished mid-conversation; accept the next one.
            }
        }
    }

    private static void Trace(string message)
    {
        if (Environment.GetEnvironmentVariable("AEGIS_PILOT_TRACE") == "1")
            Console.Error.WriteLine($"[pilot] {message}");
    }

    private async Task<string> DispatchAsync(string line)
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

        var completion = new TaskCompletionSource<PilotResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_writer.TryWrite(new HostMessage.Pilot(request, completion)))
            return PilotWire.Serialize(new PilotResponse { Ok = false, Error = "game loop is not accepting input" });

        var response = await completion.Task;
        return PilotWire.Serialize(response);
    }
}
