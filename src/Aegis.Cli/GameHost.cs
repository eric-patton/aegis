using System.Threading.Channels;
using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// Single-threaded owner of the <see cref="Game"/>. Console keys and pilot requests
/// both arrive as messages on one channel, so the engine only ever runs on one thread
/// and determinism survives concurrent input sources.
/// </summary>
public sealed class GameHost
{
    private readonly Game _game;
    private readonly ConsoleRenderer? _renderer;
    private readonly Channel<HostMessage> _channel = Channel.CreateUnbounded<HostMessage>();

    public GameHost(ulong seed, ConsoleRenderer? renderer)
    {
        _game = new Game(seed);
        _renderer = renderer;
    }

    public ChannelWriter<HostMessage> Writer => _channel.Writer;

    public async Task RunAsync()
    {
        Render();
        while (_game.Running)
        {
            HostMessage message;
            try
            {
                message = await _channel.Reader.ReadAsync();
            }
            catch (ChannelClosedException)
            {
                break;
            }

            switch (message)
            {
                case HostMessage.Key(var key):
                    _game.ApplyKey(key);
                    Render();
                    break;

                case HostMessage.Pilot(var request, var completion):
                    completion.TrySetResult(HandlePilot(request));
                    Render();
                    break;
            }
        }
        _channel.Writer.TryComplete();
    }

    private void Render() => _renderer?.Draw(Presenter.Render(_game));

    private PilotResponse HandlePilot(PilotRequest request)
    {
        try
        {
            switch (request.Cmd)
            {
                case "ping":
                    return new PilotResponse { Ok = true };

                case "screen":
                    return new PilotResponse { Ok = true, Screen = PilotScreen() };

                case "state":
                    return new PilotResponse { Ok = true, State = _game.TakeSnapshot() };

                case "keys":
                    foreach (char key in request.Keys ?? "")
                        _game.ApplyKey(key);
                    return new PilotResponse
                    {
                        Ok = true,
                        Screen = PilotScreen(),
                        State = _game.TakeSnapshot(),
                    };

                case "quit":
                    _game.Apply(Aegis.Core.Command.Quit);
                    return new PilotResponse { Ok = true };

                default:
                    return new PilotResponse { Ok = false, Error = $"unknown cmd '{request.Cmd}'" };
            }
        }
        catch (Exception ex)
        {
            return new PilotResponse { Ok = false, Error = ex.Message };
        }
    }

    /// <summary>Pilot screens always render at the 80x24 baseline, independent of the console window.</summary>
    private string[] PilotScreen() => Presenter.Render(_game).ToTextLines();
}

public abstract record HostMessage
{
    public sealed record Key(char Char) : HostMessage;
    public sealed record Pilot(PilotRequest Request, TaskCompletionSource<PilotResponse> Completion) : HostMessage;
}
