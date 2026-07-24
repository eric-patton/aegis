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
    private Frame? _lastFrame;
    private ClientInteractionContext _interaction = new(ClientSurface.World, "", [], []);
    private bool _showClientHelp;
    private bool _showCompass;
    private bool _showGuide;
    private bool _showLog;
    private int _focusedAction;
    private int _logScroll;
    private string _interactionStamp = "";
    private TimeSpan _shutdownGrace;

    public AegisScreen(
        ClientOptions options,
        ClientRuntime runtime,
        PresentationSettings presentation)
        : base(GameSession.ObservationWidth, GameSession.ObservationHeight)
    {
        UseKeyboard = true;
        UseMouse = true;
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

    PilotResponse? IFrameSink.HandlePresentation(PilotRequest request)
    {
        switch (request.Action)
        {
            case "dismiss-help":
                _showClientHelp = false;
                _presentation.HelpSeen = true;
                _presentation.Save();
                return new PilotResponse { Ok = true };
            case "guide":
                _showGuide = !_showGuide;
                _showLog = false;
                _showCompass = false;
                return new PilotResponse { Ok = true };
            case "compass":
                if (!_interaction.SupportsCompass)
                    return new PilotResponse { Ok = false, Error = "the iron rose is available only on the world view" };
                _showCompass = !_showCompass;
                _showGuide = false;
                _showLog = false;
                return new PilotResponse { Ok = true };
            case "log":
                _showLog = !_showLog;
                _showGuide = false;
                _showCompass = false;
                _logScroll = 0;
                return new PilotResponse { Ok = true };
            case "close":
                _showClientHelp = false;
                _showGuide = false;
                _showLog = false;
                _showCompass = false;
                return new PilotResponse { Ok = true };
            case "next":
                MoveFocus(1);
                return new PilotResponse { Ok = true };
            case "previous":
                MoveFocus(-1);
                return new PilotResponse { Ok = true };
            case "activate":
                return ActivateFocusedAction()
                    ? new PilotResponse { Ok = true }
                    : new PilotResponse { Ok = false, Error = "there is no enabled focused action" };
            default:
                return new PilotResponse { Ok = false, Error = "unknown presentation action" };
        }
    }

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
            if (HandlePresentationKey(pressed.Key))
            {
                handled = true;
                continue;
            }

            if (_interaction.SupportsActionFocus
                && pressed.Key is Keys.Up or Keys.Down or Keys.Tab)
            {
                MoveFocus(pressed.Key == Keys.Up ? -1 : 1);
                handled = true;
                continue;
            }

            if (_interaction.SupportsActionFocus
                && pressed.Key == Keys.Enter
                && _interaction.Surface is not ClientSurface.CreationText
                && _interaction.Surface is not ClientSurface.CreationReview)
            {
                handled |= ActivateFocusedAction();
                continue;
            }

            char? canonical = SadConsoleInputMapper.Map(
                pressed.Key,
                pressed.Character,
                _interaction.Surface);
            if (canonical is null) continue;
            handled |= _session.Writer.TryWrite(new HostMessage.Key(canonical.Value));
        }

        return handled;
    }

    public override bool ProcessMouse(MouseScreenObjectState state)
    {
        if (!state.IsOnScreenObject) return false;

        if (state.Mouse.ScrollWheelValueChange != 0
            && (_showLog || _interaction.Surface == ClientSurface.Conversation))
        {
            _logScroll = Math.Max(0, _logScroll + Math.Sign(state.Mouse.ScrollWheelValueChange) * 3);
            RenderPresentation();
            return true;
        }

        if (!state.Mouse.LeftClicked) return base.ProcessMouse(state);
        Point point = state.SurfaceCellPosition;

        if (Hit(point, ToolbarMove))
        {
            ToggleCompass();
            return true;
        }
        if (_interaction.Surface == ClientSurface.World && Hit(point, ToolbarPack))
            return _session.Writer.TryWrite(new HostMessage.Key('i'));
        if (_interaction.Surface == ClientSurface.World && Hit(point, ToolbarCharacter))
            return _session.Writer.TryWrite(new HostMessage.Key('c'));
        if (Hit(point, ToolbarGuide))
        {
            _showGuide = !_showGuide;
            _showLog = false;
            RenderPresentation();
            return true;
        }
        if (Hit(point, ToolbarLog))
        {
            _showLog = !_showLog;
            _showGuide = false;
            _logScroll = 0;
            RenderPresentation();
            return true;
        }

        if (_showGuide)
        {
            _showGuide = false;
            RenderPresentation();
            return true;
        }

        if (_showLog)
            return true;

        if (_showCompass && TryCompassKey(point, out char direction))
            return _session.Writer.TryWrite(new HostMessage.Key(direction));

        foreach (var (action, bounds) in VisibleActions())
        {
            if (!action.Enabled || !Hit(point, bounds)) continue;
            return _session.Writer.TryWrite(new HostMessage.Key(action.Key));
        }

        return base.ProcessMouse(state);
    }

    public void Draw(Frame frame, ClientInteractionContext interaction)
    {
        _lastFrame = frame;
        _interaction = interaction;
        string stamp = $"{interaction.Surface}:{string.Join('|', interaction.Actions.Select(a => $"{a.Key}:{a.Label}"))}";
        if (!string.Equals(stamp, _interactionStamp, StringComparison.Ordinal))
        {
            _interactionStamp = stamp;
            _focusedAction = FirstEnabledAction();
            _logScroll = 0;
        }

        if (interaction.Surface != ClientSurface.World)
            _showCompass = false;

        if (interaction.Surface == ClientSurface.World && !_presentation.GuideSeen)
        {
            _presentation.GuideSeen = true;
            _presentation.Save();
            _showGuide = true;
        }

        RenderPresentation();
    }

    private void RenderPresentation()
    {
        if (_lastFrame is null) return;
        Frame frame = _lastFrame;
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
        else
        {
            if (_interaction.Surface == ClientSurface.Conversation)
                DrawConversation();
            DrawActionFocus();
            DrawToolbar();
            if (_showCompass) DrawCompass();
            if (_showGuide) DrawGuide();
            if (_showLog) DrawLogOverlay();
        }
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

    private static readonly Rectangle ToolbarMove = new(72, 0, 9, 1);
    private static readonly Rectangle ToolbarPack = new(82, 0, 9, 1);
    private static readonly Rectangle ToolbarCharacter = new(92, 0, 9, 1);
    private static readonly Rectangle ToolbarGuide = new(102, 0, 8, 1);
    private static readonly Rectangle ToolbarLog = new(111, 0, 8, 1);

    private bool HandlePresentationKey(Keys key)
    {
        if (key == Keys.F1)
        {
            _showGuide = !_showGuide;
            _showLog = false;
            RenderPresentation();
            return true;
        }
        if (key == Keys.PageUp)
        {
            _showLog = true;
            _showGuide = false;
            _logScroll += 5;
            RenderPresentation();
            return true;
        }
        if (key == Keys.PageDown && _showLog)
        {
            _logScroll = Math.Max(0, _logScroll - 5);
            RenderPresentation();
            return true;
        }
        if (key == Keys.OemTilde && _interaction.SupportsCompass)
        {
            ToggleCompass();
            return true;
        }
        if (key == Keys.Escape)
        {
            if (_showGuide)
            {
                _showGuide = false;
                RenderPresentation();
                return true;
            }
            if (_showLog)
            {
                _showLog = false;
                RenderPresentation();
                return true;
            }
            if (_showCompass)
            {
                _showCompass = false;
                RenderPresentation();
                return true;
            }
        }
        return false;
    }

    private void ToggleCompass()
    {
        if (!_interaction.SupportsCompass) return;
        _showCompass = !_showCompass;
        _showGuide = false;
        _showLog = false;
        RenderPresentation();
    }

    private int FirstEnabledAction()
    {
        for (int i = 0; i < _interaction.Actions.Length; i++)
            if (_interaction.Actions[i].Enabled) return i;
        return 0;
    }

    private void MoveFocus(int delta)
    {
        if (_interaction.Actions.Length == 0) return;
        int index = _focusedAction;
        for (int i = 0; i < _interaction.Actions.Length; i++)
        {
            index = (index + delta + _interaction.Actions.Length) % _interaction.Actions.Length;
            if (_interaction.Actions[index].Enabled)
            {
                _focusedAction = index;
                RenderPresentation();
                return;
            }
        }
    }

    private bool ActivateFocusedAction()
    {
        if (_interaction.Actions.Length == 0) return false;
        ClientAction action = _interaction.Actions[Math.Clamp(
            _focusedAction,
            0,
            _interaction.Actions.Length - 1)];
        return action.Enabled && _session.Writer.TryWrite(new HostMessage.Key(action.Key));
    }

    private IEnumerable<(ClientAction Action, Rectangle Bounds)> VisibleActions()
    {
        if (_interaction.Surface == ClientSurface.Conversation)
        {
            for (int i = 0; i < _interaction.Actions.Length; i++)
                yield return (_interaction.Actions[i], new Rectangle(12, 9 + i * 2, 38, 1));
            yield break;
        }

        foreach (ClientAction action in _interaction.Actions)
            yield return (action, new Rectangle(action.X, action.Y, action.Width, 1));
    }

    private void DrawActionFocus()
    {
        if (!_interaction.SupportsActionFocus || _interaction.Actions.Length == 0) return;
        int index = Math.Clamp(_focusedAction, 0, _interaction.Actions.Length - 1);
        var visible = VisibleActions().ToArray();
        if (index >= visible.Length) return;
        var (_, bounds) = visible[index];
        Color cyan = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color panel = new(22, 29, 39);
        if (bounds.X > 0)
            Surface.Print(bounds.X - 1, bounds.Y, ">", cyan, panel);
    }

    private void DrawToolbar()
    {
        Color background = new(22, 29, 39);
        Color text = ToColor(AegisPalette.Resolve(Hue.White));
        Color muted = ToColor(AegisPalette.Resolve(Hue.DarkGray));
        Color cyan = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color gameAction = _interaction.Surface == ClientSurface.World ? text : muted;
        Surface.Print(ToolbarMove.X, 0, _showCompass ? "[Move *]" : "[Move ~]", cyan, background);
        Surface.Print(ToolbarPack.X, 0, "[Pack i]", gameAction, background);
        Surface.Print(ToolbarCharacter.X, 0, "[You c]  ", gameAction, background);
        Surface.Print(ToolbarGuide.X, 0, "[Guide]", text, background);
        Surface.Print(ToolbarLog.X, 0, "[Log]   ", text, background);
    }

    private void DrawCompass()
    {
        const int left = 62;
        const int top = 20;
        const int width = 23;
        const int height = 14;
        Color background = new(22, 29, 39);
        Color border = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color text = ToColor(AegisPalette.Resolve(Hue.White));
        Color brass = ToColor(AegisPalette.Resolve(Hue.Yellow));

        Surface.Fill(new Rectangle(left, top, width, height), text, background, ' ');
        Surface.DrawBox(new Rectangle(left, top, width, height), ShapeParameters.CreateStyledBox(
            ICellSurface.ConnectedLineThin,
            new ColoredGlyph(border, background)));
        Surface.Print(left + 6, top + 1, "IRON ROSE", brass, background);
        Surface.Print(left + 9, top + 3, "[N]", text, background);
        Surface.Print(left + 4, top + 5, "[NW]", text, background);
        Surface.Print(left + 14, top + 5, "[NE]", text, background);
        Surface.Print(left + 1, top + 7, "[W]", text, background);
        Surface.Print(left + 8, top + 7, "[wait]", brass, background);
        Surface.Print(left + 18, top + 7, "[E]", text, background);
        Surface.Print(left + 4, top + 9, "[SW]", text, background);
        Surface.Print(left + 14, top + 9, "[SE]", text, background);
        Surface.Print(left + 9, top + 11, "[S]", text, background);
    }

    private static bool TryCompassKey(Point point, out char key)
    {
        key = point switch
        {
            { X: >= 71 and <= 73, Y: 23 } => 'k',
            { X: >= 66 and <= 69, Y: 25 } => 'y',
            { X: >= 76 and <= 79, Y: 25 } => 'u',
            { X: >= 63 and <= 65, Y: 27 } => 'h',
            { X: >= 70 and <= 75, Y: 27 } => '.',
            { X: >= 80 and <= 82, Y: 27 } => 'l',
            { X: >= 66 and <= 69, Y: 29 } => 'b',
            { X: >= 76 and <= 79, Y: 29 } => 'n',
            { X: >= 71 and <= 73, Y: 31 } => 'j',
            _ => '\0',
        };
        return key != '\0';
    }

    private void DrawConversation()
    {
        const int left = 8;
        const int top = 4;
        const int width = 104;
        const int height = 32;
        const int split = 53;
        Color background = new(22, 29, 39);
        Color border = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color text = ToColor(AegisPalette.Resolve(Hue.White));
        Color muted = ToColor(AegisPalette.Resolve(Hue.Gray));
        Color brass = ToColor(AegisPalette.Resolve(Hue.Yellow));

        Surface.Fill(new Rectangle(left, top, width, height), text, background, ' ');
        Surface.DrawBox(new Rectangle(left, top, width, height), ShapeParameters.CreateStyledBox(
            ICellSurface.ConnectedLineThin,
            new ColoredGlyph(border, background)));
        Surface.Print(left + 3, top + 2, _interaction.Title, brass, background);
        Surface.Print(left + 3, top + 4, "Topics and actions", muted, background);
        Surface.Print(split, top + 4, "Conversation", muted, background);
        for (int y = top + 5; y < top + height - 2; y++)
            Surface.Print(split - 2, y, "|", border, background);

        for (int i = 0; i < _interaction.Actions.Length; i++)
        {
            ClientAction action = _interaction.Actions[i];
            string label = $"{action.Key}) {action.Label}";
            if (label.Length > 38) label = label[..38];
            Surface.Print(12, 9 + i * 2, label, action.Enabled ? text : muted, background);
        }

        DrawTranscript(split, top + 6, 54, height - 9, _interaction.Transcript, background);
        Surface.Print(left + 3, top + height - 2, "Arrows choose, Enter confirms, Escape leaves", muted, background);
    }

    private void DrawGuide()
    {
        const int left = 8;
        const int top = 2;
        const int width = 104;
        const int height = 36;
        Color background = new(22, 29, 39);
        Color border = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color text = ToColor(AegisPalette.Resolve(Hue.White));
        Color muted = ToColor(AegisPalette.Resolve(Hue.Gray));
        Color brass = ToColor(AegisPalette.Resolve(Hue.Yellow));

        Surface.Fill(new Rectangle(left, top, width, height), text, background, ' ');
        Surface.DrawBox(new Rectangle(left, top, width, height), ShapeParameters.CreateStyledBox(
            ICellSurface.ConnectedLineThin,
            new ColoredGlyph(border, background)));
        Surface.Print(left + 3, top + 2, "FIELD GUIDE", brass, background);

        string[] lines =
        [
            "Movement",
            "Arrow keys move north, south, west, and east. Press ~ for the eight-direction iron rose.",
            "Diagonal steps are sometimes needed around corners or people. The iron rose makes all eight clear.",
            "Moving into a hostile creature attacks. Moving into a friendly follower trades places.",
            "",
            "Map",
            "@ you   p person   a companion   m road beast   > entrance   & woods   ^ hills   ~ water",
            ". ground   # wall or building   + shrine   $ cache or goods   ! marked danger",
            "Bump a person to talk. Bump a companion or road beast to trade places instead of being blocked.",
            "Color reinforces identity, but glyphs and the sidebar carry the required meaning.",
            "",
            "Survival and equipment",
            "i opens inventory and equipment. Condition says how many uses remain before an item is worn.",
            "e eats one ration to recover. d drinks a prepared draught. Resources and possessions stay listed.",
            "Use m to camp on open land. At a forge, stillroom, kitchen, or camp, actions name what they make.",
            "",
            "Combat",
            "Bump an adjacent foe to strike. f aims a bow. z opens carried workings.",
            "Marked ground shows where a committed attack will land. Move away before it resolves.",
            "a prepares a parry. A parry costs stamina, lasts for the next incoming committed blow,",
            "and can break the attacker's guard. x changes stance. Uppercase directions rush in local fights.",
            "",
            "Interface",
            "Number keys remain shortcuts. In menus, arrows move focus, Enter chooses, and Escape goes back.",
            "Page Up opens scrollback. Mouse clicks use the same commands as the keyboard.",
            "",
            "Press F1, Escape, or click anywhere to close this guide.",
        ];
        int y = top + 4;
        foreach (string line in lines)
        {
            if (y >= top + height - 2) break;
            Color color = line is "Movement" or "Map" or "Survival and equipment" or "Combat" or "Interface"
                ? border
                : line.Length == 0 ? muted : text;
            foreach (string wrapped in Wrap(line, width - 6))
            {
                if (y >= top + height - 2) break;
                Surface.Print(left + 3, y++, wrapped, color, background);
            }
        }
    }

    private void DrawLogOverlay()
    {
        const int left = 6;
        const int top = 3;
        const int width = 108;
        const int height = 34;
        Color background = new(22, 29, 39);
        Color border = ToColor(AegisPalette.Resolve(Hue.Cyan));
        Color text = ToColor(AegisPalette.Resolve(Hue.White));
        Color muted = ToColor(AegisPalette.Resolve(Hue.Gray));

        Surface.Fill(new Rectangle(left, top, width, height), text, background, ' ');
        Surface.DrawBox(new Rectangle(left, top, width, height), ShapeParameters.CreateStyledBox(
            ICellSurface.ConnectedLineThin,
            new ColoredGlyph(border, background)));
        Surface.Print(left + 3, top + 2, "LOG AND CONVERSATIONS", border, background);
        DrawTranscript(left + 3, top + 4, width - 6, height - 8, _interaction.Transcript, background);
        Surface.Print(left + 3, top + height - 2, "Page Up and Page Down scroll, Escape closes", muted, background);
    }

    private void DrawTranscript(
        int left,
        int top,
        int width,
        int height,
        IReadOnlyList<LogEntry> entries,
        Color background)
    {
        var lines = new List<(string Text, Hue Hue)>();
        foreach (LogEntry entry in entries)
        {
            Hue hue = entry.Tone switch
            {
                LogTone.Aegis => Hue.Cyan,
                LogTone.Danger => Hue.Red,
                LogTone.Reward => Hue.Yellow,
                LogTone.Combat => Hue.Gray,
                _ => Hue.White,
            };
            foreach (string wrapped in Wrap(entry.Text, width))
                lines.Add((wrapped, hue));
        }

        int maxScroll = Math.Max(0, lines.Count - height);
        _logScroll = Math.Clamp(_logScroll, 0, maxScroll);
        int end = Math.Max(0, lines.Count - _logScroll);
        int start = Math.Max(0, end - height);
        int y = top;
        for (int i = start; i < end && y < top + height; i++)
            Surface.Print(left, y++, lines[i].Text, ToColor(AegisPalette.Resolve(lines[i].Hue)), background);
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (text.Length == 0)
        {
            yield return "";
            yield break;
        }

        string remaining = text;
        while (remaining.Length > width)
        {
            int split = remaining.LastIndexOf(' ', width);
            if (split <= 0) split = width;
            yield return remaining[..split].TrimEnd();
            remaining = remaining[split..].TrimStart();
        }
        yield return remaining;
    }

    private static bool Hit(Point point, Rectangle rectangle) =>
        point.X >= rectangle.X
        && point.X < rectangle.X + rectangle.Width
        && point.Y >= rectangle.Y
        && point.Y < rectangle.Y + rectangle.Height;

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
        Surface.Print(left + 3, top + 5, "Move: arrows or h j k l. Press ~ for the eight-direction iron rose.", text, background);
        Surface.Print(left + 3, top + 7, "Menus: arrows choose, Enter confirms, Escape returns. Field guide: F1.", text, background);
        Surface.Print(left + 3, top + 10, "Resize or maximize freely. The full 120 by 40 frame stays visible.", muted, background);
        Surface.Print(left + 3, top + 12, "Font scale 1 or 2 lives in %LOCALAPPDATA%\\Aegis\\presentation.json.", muted, background);
        Surface.Print(left + 3, top + 15, "Press any key to begin.", border, background);
    }
}
