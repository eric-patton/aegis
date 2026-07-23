using System.Threading.Channels;
using Aegis.Core;

namespace Aegis.Host;

public interface IFrameSink
{
    (int Width, int Height) CurrentSize { get; }
    void Draw(Frame frame);
}

public sealed class GameSession
{
    public const int ObservationWidth = 120;
    public const int ObservationHeight = 40;

    private readonly Game _game;
    private readonly IFrameSink? _renderer;
    private readonly Channel<HostMessage> _channel = Channel.CreateUnbounded<HostMessage>(
        new UnboundedChannelOptions { SingleReader = true });
    private bool _started;

    public GameSession(Game game, IFrameSink? renderer)
    {
        _game = game;
        _renderer = renderer;
    }

    public ChannelWriter<HostMessage> Writer => _channel.Writer;
    public bool Running => _game.Running;

    public async Task RunAsync()
    {
        Start();
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

            Process(message);
        }

        _channel.Writer.TryComplete();
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        Render();
    }

    public void Drain()
    {
        Start();
        while (_game.Running && _channel.Reader.TryRead(out HostMessage? message))
            Process(message);

        if (!_game.Running)
            _channel.Writer.TryComplete();
    }

    private void Process(HostMessage message)
    {
        switch (message)
        {
            case HostMessage.Key(var key):
                _game.ApplyKey(key);
                Render();
                break;

            case HostMessage.Pilot(var request, var completion):
                PilotResponse response = HandlePilot(request);
                Render();
                completion.TrySetResult(response);
                break;

            case HostMessage.Redraw:
                Render();
                break;
        }
    }

    private void Render()
    {
        if (_renderer is null) return;
        var (width, height) = _renderer.CurrentSize;
        _renderer.Draw(Presenter.Render(_game, width, height));
    }

    private PilotResponse HandlePilot(PilotRequest request)
    {
        try
        {
            switch (request.Cmd)
            {
                case "ping":
                    return new PilotResponse { Ok = true };

                case "screen":
                    return new PilotResponse { Ok = true, Screen = Observation().ToTextLines() };

                case "state":
                    return new PilotResponse { Ok = true, State = _game.TakeSnapshot() };

                case "frame":
                    return new PilotResponse { Ok = true, Frame = FrameObservation.From(Observation()) };

                case "keys":
                    string keys = request.Keys ?? "";
                    if (keys.Length > PilotWire.MaxKeyBatchLength)
                        return new PilotResponse { Ok = false, Error = "key batch is too large" };

                    foreach (char key in keys)
                        _game.ApplyKey(key);
                    return new PilotResponse
                    {
                        Ok = true,
                        Screen = Observation().ToTextLines(),
                        State = _game.TakeSnapshot(),
                    };

                case "quit":
                    _game.Apply(Command.Quit);
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

    private Frame Observation() => Presenter.Render(_game, ObservationWidth, ObservationHeight);
}

public abstract record HostMessage
{
    public sealed record Key(char Char) : HostMessage;
    public sealed record Pilot(
        PilotRequest Request,
        TaskCompletionSource<PilotResponse> Completion) : HostMessage;

    public sealed record Redraw : HostMessage
    {
        public static readonly Redraw Instance = new();
    }
}
