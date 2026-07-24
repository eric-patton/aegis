using Aegis.Core;
using Aegis.Host;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace Aegis.Client;

public sealed class AegisScreen : ScreenSurface, IFrameSink
{
    private readonly GameSession _session;
    private readonly PilotServer? _pilot;
    private readonly PresentationSettings _presentation;
    private bool _showClientHelp;
    private TimeSpan _shutdownGrace;

    public AegisScreen(
        ClientOptions options,
        ClientRuntime runtime,
        PresentationSettings presentation)
        : base(GameSession.ObservationWidth, GameSession.ObservationHeight)
    {
        UseKeyboard = true;
        UseMouse = false;
        Surface.DefaultBackground = ToColor(AegisPalette.Clear);
        Surface.Clear();
        _presentation = presentation;
        _showClientHelp = !presentation.HelpSeen;

        _session = new GameSession(runtime.Game, this);
        if (options.Pilot)
        {
            _pilot = new PilotServer(options.Session, _session.Writer);
            _pilot.Start();
        }

        _session.Start();
    }

    (int Width, int Height) IFrameSink.CurrentSize =>
        (GameSession.ObservationWidth, GameSession.ObservationHeight);

    public override void Update(TimeSpan delta)
    {
        _session.Drain();
        if (!_session.Running)
        {
            _shutdownGrace += delta;
            if (_shutdownGrace >= TimeSpan.FromMilliseconds(250))
                SadConsole.GameHost.Instance.Stop();
        }
        base.Update(delta);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (_showClientHelp && keyboard.KeysPressed.Count > 0)
        {
            _showClientHelp = false;
            _presentation.HelpSeen = true;
            _presentation.Save();
            _session.Writer.TryWrite(HostMessage.Redraw.Instance);
            return true;
        }

        bool handled = false;
        foreach (AsciiKey pressed in keyboard.KeysPressed)
        {
            char? canonical = SadConsoleInputMapper.Map(pressed.Key, pressed.Character);
            if (canonical is null) continue;
            handled |= _session.Writer.TryWrite(new HostMessage.Key(canonical.Value));
        }

        return handled;
    }

    public void Draw(Frame frame)
    {
        for (int y = 0; y < frame.Height; y++)
        {
            for (int x = 0; x < frame.Width; x++)
            {
                Cell cell = frame[x, y];
                int glyph = cell.Ch;
                if (glyph >= Font.TotalGlyphs)
                    glyph = '?';
                Surface.SetCellAppearance(
                    x,
                    y,
                    new ColoredGlyph(
                        ToColor(AegisPalette.Resolve(cell.Fg)),
                        ToColor(AegisPalette.Resolve(cell.Bg)),
                        glyph));
            }
        }

        if (_showClientHelp)
            DrawClientHelp();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pilot?.Stop();
            _pilot?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Color ToColor(Rgb24 color) => new(color.R, color.G, color.B);

    private void DrawClientHelp()
    {
        const int left = 18;
        const int top = 10;
        const int width = 84;
        const int height = 18;
        Color background = new(22, 29, 39);
        Color border = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color text = ToColor(AegisPalette.Resolve(Hue.White));
        Color muted = ToColor(AegisPalette.Resolve(Hue.Gray));

        Surface.Fill(new Rectangle(left, top, width, height), text, background, ' ');
        Surface.DrawBox(new Rectangle(left, top, width, height), ShapeParameters.CreateStyledBox(
            ICellSurface.ConnectedLineThin,
            new ColoredGlyph(border, background)));
        Surface.Print(left + 3, top + 2, "AEGIS OWNS THIS WINDOW", border, background);
        Surface.Print(left + 3, top + 5, "Move: arrows or h j k l. Diagonals: y u b n.", text, background);
        Surface.Print(left + 3, top + 7, "Wait: .    Enter/leave: > and <    Help: ?    Quit: q", text, background);
        Surface.Print(left + 3, top + 10, "Resize or maximize freely. The full 120 by 40 frame stays visible.", muted, background);
        Surface.Print(left + 3, top + 12, "Font scale 1 or 2 lives in %LOCALAPPDATA%\\Aegis\\presentation.json.", muted, background);
        Surface.Print(left + 3, top + 15, "Press any key to begin.", border, background);
    }
}
