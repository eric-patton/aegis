using System.Text;
using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

public sealed partial class Main : Control, IFrameSink
{
    private static readonly Color DarkCoal = Html("#10161A");
    private static readonly Color DarkPanel = Html("#182126");
    private static readonly Color DarkRaised = Html("#223038");
    private static readonly Color DarkText = Html("#E7E1D2");
    private static readonly Color DarkMuted = Html("#9BA7A8");
    private static readonly Color DarkAccent = Html("#72C7CC");
    private static readonly Color DarkWarm = Html("#D69A48");
    private static readonly Color DarkDanger = Html("#A9534F");

    private static readonly Color LightPaper = Html("#E9E2D3");
    private static readonly Color LightPanel = Html("#D8D0C1");
    private static readonly Color LightRaised = Html("#F4EFE5");
    private static readonly Color LightText = Html("#20282B");
    private static readonly Color LightMuted = Html("#687274");
    private static readonly Color LightAccent = Html("#2D7278");
    private static readonly Color LightWarm = Html("#9C6828");
    private static readonly Color LightDanger = Html("#8F403E");

    private SpikeRuntime? _runtime;
    private GameSession? _session;
    private PilotServer? _pilot;
    private Frame? _frame;
    private ClientInteractionContext _interaction = new(ClientSurface.World, "", [], []);

    private Font? _mapFont;
    private Font? _proseFont;
    private bool _lightTheme;
    private bool _compassOpen;
    private bool _stressText;
    private double _shutdownGrace;

    private ColorRect _background = null!;
    private VBoxContainer _shell = null!;
    private HBoxContainer _header = null!;
    private Label _wordmark = null!;
    private Label _place = null!;
    private Button _themeButton = null!;
    private Button _moveButton = null!;
    private PanelContainer _surfacePanel = null!;
    private Control _surface = null!;
    private PanelContainer _compass = null!;

    public (int Width, int Height) CurrentSize
    {
        get
        {
            Vector2 viewport = GetViewportRect().Size;
            int width = Math.Clamp((int)(viewport.X / 13.5f), 80, 140);
            int height = Math.Clamp((int)((viewport.Y - 150) / 21f), 24, 46);
            return (width, height);
        }
    }

    public override void _Ready()
    {
        GetWindow().Title = "Aegis, Godot presentation spike";
        GetWindow().MinSize = new Vector2I(900, 600);
        _mapFont = GD.Load<Font>("res://assets/fonts/AzeretMono.ttf");
        _proseFont = GD.Load<Font>("res://assets/fonts/Literata.ttf");

        BuildChrome();

        SpikeOptions options;
        try
        {
            options = SpikeOptions.Parse([.. OS.GetCmdlineUserArgs()]);
            _lightTheme = options.Theme == "light";
            _runtime = new SpikeRuntime(options);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or IOException)
        {
            ShowStartupError(ex.Message);
            return;
        }

        ApplyTheme();
        _session = new GameSession(_runtime.Game, this);
        if (options.Pilot)
        {
            _pilot = new PilotServer(options.Session, _session.Writer);
            _pilot.Start();
        }
        _session.Start();
    }

    public override void _Process(double delta)
    {
        if (_session is null)
            return;

        _session.Drain();
        if (!_session.Running)
        {
            _shutdownGrace += delta;
            if (_shutdownGrace >= 0.25)
                GetTree().Quit();
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key || _session is null)
            return;

        char? canonical = MapKey(key);
        if (canonical is null)
            return;

        if (_session.Writer.TryWrite(new HostMessage.Key(canonical.Value)))
            GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        _pilot?.Stop();
        _pilot?.Dispose();
        _runtime?.Dispose();
    }

    void IFrameSink.Draw(Frame frame, ClientInteractionContext interaction)
    {
        _frame = frame;
        ClientSurface previous = _interaction.Surface;
        _interaction = interaction;

        if (interaction.Surface == ClientSurface.World && previous != ClientSurface.World)
            _compass.Visible = _compassOpen;
        else if (interaction.Surface != ClientSurface.World)
            _compass.Visible = false;

        RenderSurface();
    }

    public PilotResponse? HandlePresentation(PilotRequest request)
    {
        switch (request.Action)
        {
            case "compass":
                if (_interaction.Surface != ClientSurface.World)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "the iron rose is available only on the world view",
                    };
                ToggleCompass();
                return new PilotResponse { Ok = true };
            case "theme":
                ToggleTheme();
                return new PilotResponse { Ok = true };
            case "stress":
                if (_interaction.Surface != ClientSurface.Conversation)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "the text stress view is available only in conversation",
                    };
                _stressText = !_stressText;
                RenderSurface();
                return new PilotResponse { Ok = true };
            case "close":
                _compassOpen = false;
                _compass.Visible = false;
                return new PilotResponse { Ok = true };
            case "next":
                MoveFocus(1);
                return new PilotResponse { Ok = true };
            case "previous":
                MoveFocus(-1);
                return new PilotResponse { Ok = true };
            case "activate":
                return ActivateFocus()
                    ? new PilotResponse { Ok = true }
                    : new PilotResponse { Ok = false, Error = "there is no enabled focused action" };
            default:
                return new PilotResponse { Ok = false, Error = "unknown presentation action" };
        }
    }

    private void BuildChrome()
    {
        _background = new ColorRect();
        _background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        AddChild(margin);

        _shell = new VBoxContainer();
        _shell.AddThemeConstantOverride("separation", 14);
        margin.AddChild(_shell);

        _header = new HBoxContainer { CustomMinimumSize = new Vector2(0, 54) };
        _header.AddThemeConstantOverride("separation", 18);
        _shell.AddChild(_header);

        _wordmark = NewLabel("AEGIS", 27, true);
        _wordmark.CustomMinimumSize = new Vector2(118, 0);
        _header.AddChild(_wordmark);

        var rule = new VSeparator();
        _header.AddChild(rule);

        _place = NewLabel("The first road", 16);
        _place.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _place.VerticalAlignment = VerticalAlignment.Center;
        _header.AddChild(_place);

        _moveButton = NewButton("Move  ~");
        _moveButton.Pressed += ToggleCompass;
        _header.AddChild(_moveButton);

        _themeButton = NewButton("Light field");
        _themeButton.Pressed += ToggleTheme;
        _header.AddChild(_themeButton);

        _surfacePanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _shell.AddChild(_surfacePanel);

        _compass = BuildCompass();
        _compass.Visible = false;
        _compass.SetAnchorsPreset(LayoutPreset.BottomLeft);
        _compass.Position = new Vector2(56, -252);
        _compass.CustomMinimumSize = new Vector2(220, 220);
        AddChild(_compass);
    }

    private void RenderSurface()
    {
        if (_frame is null)
            return;

        if (_surface is not null && IsInstanceValid(_surface))
            _surface.QueueFree();

        _surface = _interaction.Surface switch
        {
            ClientSurface.CreationChoice or ClientSurface.CreationText or ClientSurface.CreationReview
                => BuildCreation(),
            ClientSurface.Conversation => BuildConversation(),
            _ => BuildWorld(),
        };
        _surfacePanel.AddChild(_surface);
        ApplyTheme();
    }

    private Control BuildCreation()
    {
        _place.Text = "Character creation";
        _moveButton.Visible = false;

        var center = new CenterContainer();
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(720, 0) };
        panel.AddThemeStyleboxOverride("panel", PanelStyle(raised: true, border: true));
        center.AddChild(panel);

        var margin = InnerMargin(38, 34);
        panel.AddChild(margin);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 18);
        margin.AddChild(stack);

        var eyebrow = NewLabel(
            $"BECOMING  {_interaction.ProgressStep ?? 1:00} / {_interaction.ProgressTotal ?? 10:00}",
            13,
            true);
        eyebrow.AddThemeColorOverride("font_color", Accent);
        stack.AddChild(eyebrow);

        var progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = _interaction.ProgressTotal ?? 10,
            Value = _interaction.ProgressStep ?? 1,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 8),
        };
        stack.AddChild(progress);

        var prompt = NewLabel(_interaction.Prompt, 30, true);
        prompt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(prompt);

        if (_interaction.Surface == ClientSurface.CreationText)
        {
            var entry = NewLabel(
                _interaction.Detail.Length > 0 ? $"{_interaction.Detail}│" : "│",
                26,
                true);
            entry.CustomMinimumSize = new Vector2(0, 62);
            entry.AddThemeStyleboxOverride("normal", FieldStyle());
            entry.AddThemeColorOverride("font_color", Accent);
            stack.AddChild(entry);
        }
        else if (_interaction.Surface == ClientSurface.CreationReview && _interaction.Detail.Length > 0)
        {
            var detail = NewLabel(_interaction.Detail, 18);
            detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            detail.AddThemeColorOverride("font_color", MutedText);
            stack.AddChild(detail);
        }

        var actionsScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 250),
        };
        stack.AddChild(actionsScroll);
        var actions = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride("separation", 8);
        actionsScroll.AddChild(actions);
        AddActionButtons(actions, _interaction.Actions);

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 12);
        stack.AddChild(footer);
        var back = NewButton("Back  Esc");
        back.Disabled = _interaction.ProgressStep == 1;
        back.Pressed += () => SendKey('[');
        footer.AddChild(back);
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        footer.AddChild(spacer);
        var hint = NewLabel(CreationHint(), 13);
        hint.AddThemeColorOverride("font_color", MutedText);
        footer.AddChild(hint);

        if (_interaction.Surface is ClientSurface.CreationText or ClientSurface.CreationReview)
        {
            var continueButton = NewButton(
                _interaction.Surface == ClientSurface.CreationReview ? "Begin  Enter" : "Continue  Enter",
                emphasis: true);
            continueButton.Pressed += () => SendKey('.');
            footer.AddChild(continueButton);
        }

        return center;
    }

    private Control BuildWorld()
    {
        _place.Text = WorldTitle();
        _moveButton.Visible = true;

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        var split = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SplitOffsets = [820],
        };
        root.AddChild(split);

        var mapPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        split.AddChild(mapPanel);
        var mapMargin = InnerMargin(16, 14);
        mapPanel.AddChild(mapMargin);
        var map = NewRichText(17, mono: true);
        map.ScrollActive = true;
        map.BbcodeEnabled = true;
        map.Text = FrameBbCode(_frame!);
        mapMargin.AddChild(map);

        var rail = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
        };
        rail.AddThemeConstantOverride("separation", 12);
        split.AddChild(rail);

        var statusPanel = new PanelContainer();
        rail.AddChild(statusPanel);
        var statusMargin = InnerMargin(18, 16);
        statusPanel.AddChild(statusMargin);
        var status = NewRichText(15);
        status.FitContent = true;
        status.Text = StatusText();
        statusMargin.AddChild(status);

        var logPanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        rail.AddChild(logPanel);
        var logMargin = InnerMargin(18, 16);
        logPanel.AddChild(logMargin);
        var logStack = new VBoxContainer();
        logStack.AddThemeConstantOverride("separation", 10);
        logMargin.AddChild(logStack);
        var logTitle = NewLabel("RECENT WORDS", 12, true);
        logTitle.AddThemeColorOverride("font_color", Accent);
        logStack.AddChild(logTitle);
        var log = NewRichText(15);
        log.SizeFlagsVertical = SizeFlags.ExpandFill;
        log.Text = string.Join(
            "\n\n",
            _interaction.Transcript.TakeLast(8).Select(entry => entry.Text));
        logStack.AddChild(log);

        var command = NewLabel(
            "Move with arrows, HJKL, or the iron rose. F1 guide  •  I pack  •  C character",
            13);
        command.AddThemeColorOverride("font_color", MutedText);
        command.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(command);
        return root;
    }

    private Control BuildConversation()
    {
        _place.Text = _interaction.Title.Length > 0 ? _interaction.Title : "Conversation";
        _moveButton.Visible = false;

        var margin = InnerMargin(24, 22);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 16);
        margin.AddChild(stack);

        var heading = NewLabel(_interaction.Title, 28, true);
        heading.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        stack.AddChild(heading);

        var split = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SplitOffsets = [480],
        };
        stack.AddChild(split);

        var actionPanel = new PanelContainer { CustomMinimumSize = new Vector2(390, 0) };
        split.AddChild(actionPanel);
        var actionMargin = InnerMargin(18, 18);
        actionPanel.AddChild(actionMargin);
        var actionStack = new VBoxContainer();
        actionStack.AddThemeConstantOverride("separation", 12);
        actionMargin.AddChild(actionStack);
        var actionTitle = NewLabel("TOPICS AND ACTIONS", 12, true);
        actionTitle.AddThemeColorOverride("font_color", Accent);
        actionStack.AddChild(actionTitle);
        var actionScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        actionStack.AddChild(actionScroll);
        var actions = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride("separation", 8);
        actionScroll.AddChild(actions);
        IEnumerable<ClientAction> shownActions = _interaction.Actions;
        if (_stressText)
        {
            shownActions = new[]
            {
                new ClientAction(
                '0',
                "Inspect a deliberately oversized presentation label that must wrap across several lines without losing its shortcut, words, focus boundary, or disabled state.",
                0,
                0,
                0,
                Enabled: false),
            }.Concat(shownActions);
        }
        AddActionButtons(actions, [.. shownActions]);

        var transcriptPanel = new PanelContainer();
        split.AddChild(transcriptPanel);
        var transcriptMargin = InnerMargin(22, 18);
        transcriptPanel.AddChild(transcriptMargin);
        var transcriptStack = new VBoxContainer();
        transcriptStack.AddThemeConstantOverride("separation", 12);
        transcriptMargin.AddChild(transcriptStack);
        var transcriptTitle = NewLabel("CONVERSATION", 12, true);
        transcriptTitle.AddThemeColorOverride("font_color", Warm);
        transcriptStack.AddChild(transcriptTitle);
        var transcript = NewRichText(17);
        transcript.SizeFlagsVertical = SizeFlags.ExpandFill;
        IEnumerable<string> transcriptLines = _interaction.Transcript
            .TakeLast(28)
            .Select(entry => entry.Text);
        if (_stressText)
        {
            transcriptLines = new[]
            {
                "Text stress proof: this deliberately oversized paragraph continues well beyond the width of the transcript pane so the native layout must wrap every word, preserve comfortable line spacing, and make the complete passage reachable by scrolling.",
            }.Concat(transcriptLines);
        }
        transcript.Text = string.Join(
            "\n\n",
            transcriptLines);
        transcriptStack.AddChild(transcript);

        var footer = new HBoxContainer();
        stack.AddChild(footer);
        var leave = NewButton("Leave  Esc");
        leave.Pressed += () => SendKey('q');
        footer.AddChild(leave);
        var footerHint = NewLabel(
            "Arrow keys move focus  •  Enter confirms  •  Number shortcuts remain available",
            13);
        footerHint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footerHint.HorizontalAlignment = HorizontalAlignment.Right;
        footerHint.AddThemeColorOverride("font_color", MutedText);
        footer.AddChild(footerHint);
        return margin;
    }

    private PanelContainer BuildCompass()
    {
        var panel = new PanelContainer();
        var margin = InnerMargin(18, 16);
        panel.AddChild(margin);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 10);
        margin.AddChild(stack);
        var title = NewLabel("IRON ROSE", 12, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(title);
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        stack.AddChild(grid);
        foreach (var (label, key) in new[]
        {
            ("NW", 'y'), ("N", 'k'), ("NE", 'u'),
            ("W", 'h'), ("WAIT", '.'), ("E", 'l'),
            ("SW", 'b'), ("S", 'j'), ("SE", 'n'),
        })
        {
            Button button = NewButton(label, emphasis: key == '.');
            button.CustomMinimumSize = new Vector2(56, 44);
            char captured = key;
            button.Pressed += () => SendKey(captured);
            grid.AddChild(button);
        }
        return panel;
    }

    private void AddActionButtons(VBoxContainer parent, IReadOnlyList<ClientAction> actions)
    {
        bool focused = false;
        foreach (ClientAction action in actions)
        {
            var button = NewButton($"{action.Key})  {action.Label}");
            button.Disabled = !action.Enabled;
            button.Alignment = HorizontalAlignment.Left;
            button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.CustomMinimumSize = new Vector2(0, 48);
            char key = action.Key;
            button.Pressed += () => SendKey(key);
            parent.AddChild(button);
            if (!focused && action.Enabled)
            {
                button.CallDeferred(Control.MethodName.GrabFocus);
                focused = true;
            }
        }
    }

    private char? MapKey(InputEventKey key)
    {
        if (key.Keycode == Key.F6)
        {
            ToggleTheme();
            return null;
        }
        if (key.Keycode == Key.Quoteleft && _interaction.Surface == ClientSurface.World)
        {
            ToggleCompass();
            return null;
        }
        if (key.Keycode == Key.Backspace && _interaction.IsCreationText)
            return '-';
        if (key.Keycode is Key.Enter or Key.KpEnter
            && _interaction.Surface is ClientSurface.CreationText or ClientSurface.CreationReview)
            return '.';
        if (key.Keycode == Key.Escape)
        {
            if (_interaction.IsCreation)
                return '[';
            return 'q';
        }

        if (_interaction.Surface == ClientSurface.World)
        {
            if (key.Keycode == Key.Up) return 'k';
            if (key.Keycode == Key.Down) return 'j';
            if (key.Keycode == Key.Left) return 'h';
            if (key.Keycode == Key.Right) return 'l';
        }

        if (key.Unicode > 0 && key.Unicode <= char.MaxValue)
            return (char)key.Unicode;
        return null;
    }

    private void SendKey(char key)
    {
        _session?.Writer.TryWrite(new HostMessage.Key(key));
    }

    private void ToggleCompass()
    {
        if (_interaction.Surface != ClientSurface.World)
            return;
        _compassOpen = !_compassOpen;
        _compass.Visible = _compassOpen;
        _moveButton.Text = _compassOpen ? "Close move  ~" : "Move  ~";
    }

    private void ToggleTheme()
    {
        _lightTheme = !_lightTheme;
        ApplyTheme();
        if (_frame is not null)
            RenderSurface();
    }

    private void MoveFocus(int delta)
    {
        var buttons = FindEnabledButtons(_surface).ToArray();
        if (buttons.Length == 0)
            return;
        Control? owner = GetViewport().GuiGetFocusOwner();
        int index = Array.IndexOf(buttons, owner);
        index = (index + delta + buttons.Length) % buttons.Length;
        buttons[index].GrabFocus();
    }

    private bool ActivateFocus()
    {
        if (GetViewport().GuiGetFocusOwner() is not Button { Disabled: false } button)
            return false;
        button.EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    private static IEnumerable<Button> FindEnabledButtons(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Button { Disabled: false } button)
                yield return button;
            foreach (Button nested in FindEnabledButtons(child))
                yield return nested;
        }
    }

    private void ApplyTheme()
    {
        _background.Color = Background;
        _themeButton.Text = _lightTheme ? "Dark iron  F6" : "Light field  F6";
        _wordmark.AddThemeColorOverride("font_color", Accent);
        _place.AddThemeColorOverride("font_color", MainText);
        _surfacePanel.AddThemeStyleboxOverride("panel", PanelStyle());
        _compass.AddThemeStyleboxOverride("panel", CompassStyle());
        ApplyNodeTheme(this);
    }

    private void ApplyNodeTheme(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            switch (child)
            {
                case Label label:
                    if (!label.HasThemeColorOverride("font_color"))
                        label.AddThemeColorOverride("font_color", MainText);
                    break;
                case RichTextLabel rich:
                    rich.AddThemeColorOverride("default_color", MainText);
                    rich.AddThemeColorOverride("font_shadow_color", Colors.Transparent);
                    break;
                case Button button:
                    StyleButton(button);
                    break;
                case PanelContainer panel when panel != _surfacePanel && panel != _compass:
                    if (!panel.HasThemeStyleboxOverride("panel"))
                        panel.AddThemeStyleboxOverride("panel", PanelStyle());
                    break;
                case ProgressBar progress:
                    progress.AddThemeStyleboxOverride("background", ProgressStyle(Panel));
                    progress.AddThemeStyleboxOverride("fill", ProgressStyle(Accent));
                    break;
            }
            ApplyNodeTheme(child);
        }
    }

    private Label NewLabel(string text, int size, bool display = false)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeFontOverride("font", display ? _mapFont : _proseFont);
        return label;
    }

    private RichTextLabel NewRichText(int size, bool mono = false)
    {
        var rich = new RichTextLabel
        {
            FitContent = false,
            ScrollActive = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        rich.AddThemeFontSizeOverride("normal_font_size", size);
        rich.AddThemeFontOverride("normal_font", mono ? _mapFont : _proseFont);
        rich.AddThemeFontOverride("mono_font", _mapFont);
        return rich;
    }

    private Button NewButton(string text, bool emphasis = false)
    {
        var button = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeFontOverride("font", _mapFont);
        button.SetMeta("emphasis", emphasis);
        StyleButton(button);
        return button;
    }

    private void StyleButton(Button button)
    {
        bool emphasis = button.HasMeta("emphasis") && button.GetMeta("emphasis").AsBool();
        button.AddThemeColorOverride("font_color", emphasis ? Background : MainText);
        button.AddThemeColorOverride("font_hover_color", emphasis ? Background : MainText);
        button.AddThemeColorOverride("font_pressed_color", Background);
        button.AddThemeColorOverride("font_focus_color", emphasis ? Background : MainText);
        button.AddThemeColorOverride("font_disabled_color", MutedText);
        button.AddThemeStyleboxOverride("normal", ButtonStyle(emphasis ? Accent : Raised));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(emphasis ? Warm : Hover));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(Warm));
        button.AddThemeStyleboxOverride("focus", FocusStyle());
        button.AddThemeStyleboxOverride("disabled", ButtonStyle(Panel, 0.45f));
    }

    private MarginContainer InnerMargin(int horizontal, int vertical)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", horizontal);
        margin.AddThemeConstantOverride("margin_right", horizontal);
        margin.AddThemeConstantOverride("margin_top", vertical);
        margin.AddThemeConstantOverride("margin_bottom", vertical);
        return margin;
    }

    private StyleBoxFlat PanelStyle(bool raised = false, bool border = false)
    {
        var style = new StyleBoxFlat
        {
            BgColor = raised ? Raised : Panel,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
        };
        if (border)
        {
            style.BorderWidthLeft = 1;
            style.BorderWidthTop = 1;
            style.BorderWidthRight = 1;
            style.BorderWidthBottom = 1;
            style.BorderColor = Accent;
        }
        return style;
    }

    private StyleBoxFlat FieldStyle()
    {
        var style = ButtonStyle(Background);
        style.BorderWidthBottom = 2;
        style.BorderColor = Accent;
        style.ContentMarginLeft = 18;
        style.ContentMarginTop = 14;
        return style;
    }

    private StyleBoxFlat CompassStyle()
    {
        var style = PanelStyle(raised: true, border: true);
        style.ShadowColor = new Color(0, 0, 0, 0.35f);
        style.ShadowSize = 12;
        return style;
    }

    private StyleBoxFlat ButtonStyle(Color color, float opacity = 1f)
    {
        color.A *= opacity;
        return new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 11,
            ContentMarginBottom = 11,
        };
    }

    private StyleBoxFlat FocusStyle()
    {
        var style = ButtonStyle(Colors.Transparent);
        style.BorderWidthLeft = 2;
        style.BorderWidthTop = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthBottom = 2;
        style.BorderColor = Accent;
        return style;
    }

    private static StyleBoxFlat ProgressStyle(Color color) => new()
    {
        BgColor = color,
        CornerRadiusTopLeft = 2,
        CornerRadiusTopRight = 2,
        CornerRadiusBottomLeft = 2,
        CornerRadiusBottomRight = 2,
    };

    private string FrameBbCode(Frame frame)
    {
        int sidebarWidth = frame.Width >= 110 ? 31 : 23;
        int mapWidth = Math.Max(1, frame.Width - sidebarWidth - 2);
        int mapHeight = Math.Max(1, frame.Height - 7);
        var builder = new StringBuilder(mapWidth * mapHeight * 8);
        for (int y = 1; y <= mapHeight; y++)
        {
            Hue? foreground = null;
            Hue? background = null;
            for (int x = 0; x < mapWidth; x++)
            {
                Cell cell = frame[x, y];
                if (cell.Fg != foreground || cell.Bg != background)
                {
                    if (foreground is not null)
                        builder.Append("[/bgcolor][/color]");
                    foreground = cell.Fg;
                    background = cell.Bg;
                    builder.Append("[color=#");
                    builder.Append(Hex(HueColor(cell.Fg)));
                    builder.Append("][bgcolor=#");
                    builder.Append(Hex(HueColor(cell.Bg)));
                    builder.Append(']');
                }
                AppendBbChar(builder, cell.Ch);
            }
            if (foreground is not null)
                builder.Append("[/bgcolor][/color]");
            if (y < mapHeight)
                builder.Append('\n');
        }
        return builder.ToString();
    }

    private string StatusText()
    {
        if (_runtime is null)
            return "";
        Game game = _runtime.Game;
        Player player = game.Player;
        return string.Join(
            "\n",
            new[]
            {
                player.Name.Length > 0 ? player.Name : "The bearer",
                $"Cycle {game.Cycle}  •  Turn {game.Turn}",
                $"{game.Season}  •  {game.WeatherRead(game.LocalClimate)}",
                "",
                $"Health     {player.Hp} / {player.EffectiveMaxHp}",
                $"Stamina    {player.Stamina} / {player.MaxStamina}",
                $"Coin       {player.Coin}",
                $"Essence    {player.Essence}",
                player.Rations > 0 ? $"Rations    {player.Rations}" : "",
                player.WoundedTurns > 0 ? $"Wounded    {player.WoundedTurns}" : "",
            }.Where(line => line.Length > 0));
    }

    private string WorldTitle()
    {
        if (_runtime is null)
            return "The road";
        Game game = _runtime.Game;
        return $"{game.World.Name}  •  {game.World.SettlementName}  •  Cycle {game.Cycle}";
    }

    private string CreationHint() => _interaction.Surface switch
    {
        ClientSurface.CreationText => "Type normally, Backspace erases",
        ClientSurface.CreationReview => "Enter confirms",
        _ => "Number keys or pointer choose",
    };

    private void ShowStartupError(string message)
    {
        _place.Text = "The window could not open";
        _moveButton.Visible = false;
        _themeButton.Visible = false;
        var margin = InnerMargin(32, 28);
        var label = NewLabel(message, 18);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        margin.AddChild(label);
        _surfacePanel.AddChild(margin);
        ApplyTheme();
    }

    private static void AppendBbChar(StringBuilder builder, char value)
    {
        switch (value)
        {
            case '[':
                builder.Append("[lb]");
                break;
            case ']':
                builder.Append("[rb]");
                break;
            default:
                builder.Append(value);
                break;
        }
    }

    private Color HueColor(Hue hue)
    {
        if (!_lightTheme)
        {
            Rgb24 rgb = AegisPalette.Resolve(hue);
            return Color.Color8(rgb.R, rgb.G, rgb.B);
        }

        return hue switch
        {
            Hue.Black => LightPaper,
            Hue.DarkBlue => Html("#45658D"),
            Hue.DarkGreen => Html("#48764F"),
            Hue.DarkCyan => Html("#3E7478"),
            Hue.DarkRed => LightDanger,
            Hue.DarkMagenta => Html("#755C83"),
            Hue.DarkYellow => LightWarm,
            Hue.Gray => Html("#4B5557"),
            Hue.DarkGray => LightMuted,
            Hue.Blue => Html("#315D98"),
            Hue.Green => Html("#326D3E"),
            Hue.Cyan => LightAccent,
            Hue.Red => Html("#A33236"),
            Hue.Magenta => Html("#6B4A78"),
            Hue.Yellow => Html("#825516"),
            Hue.White => LightText,
            _ => LightText,
        };
    }

    private static Color Html(string hex) => Color.FromHtml(hex);

    private static string Hex(Color color) =>
        $"{(byte)Math.Round(color.R * 255):X2}{(byte)Math.Round(color.G * 255):X2}{(byte)Math.Round(color.B * 255):X2}";

    private Color Background => _lightTheme ? LightPaper : DarkCoal;
    private Color Panel => _lightTheme ? LightPanel : DarkPanel;
    private Color Raised => _lightTheme ? LightRaised : DarkRaised;
    private Color Hover => _lightTheme ? Html("#C6D9D7") : Html("#2A4047");
    private Color MainText => _lightTheme ? LightText : DarkText;
    private Color MutedText => _lightTheme ? LightMuted : DarkMuted;
    private Color Accent => _lightTheme ? LightAccent : DarkAccent;
    private Color Warm => _lightTheme ? LightWarm : DarkWarm;
    private Color Danger => _lightTheme ? LightDanger : DarkDanger;
}
