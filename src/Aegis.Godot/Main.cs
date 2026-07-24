using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

public sealed partial class Main : Control, IFrameSink
{
    private SpikeRuntime? _runtime;
    private GameSession? _session;
    private PilotServer? _pilot;
    private Frame? _frame;
    private ClientInteractionContext _interaction = new(ClientSurface.World, "", [], []);
    private ClientSurface? _visibleSurface;
    private double _shutdownGrace;

    private readonly ClientFonts _fonts = new();
    private GodotPresentationSettings _settings = null!;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    private ColorRect _background = null!;
    private MarginContainer _outerMargin = null!;
    private VBoxContainer _shell = null!;
    private Label _wordmark = null!;
    private Label _place = null!;
    private Button _moveButton = null!;
    private Button _scaleButton = null!;
    private Button _themeButton = null!;
    private PanelContainer _surfacePanel = null!;
    private Control _screenHost = null!;
    private CreationScreen _creation = null!;
    private WorldScreen _world = null!;
    private ConversationScreen _conversation = null!;
    private LegacyScreen _legacy = null!;
    private PanelContainer _compass = null!;
    private bool _compassOpen;

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
        GetWindow().Title = "Aegis";
        GetWindow().MinSize = new Vector2I(1100, 700);
        _settings = GodotPresentationSettings.Load();

        SpikeOptions options;
        try
        {
            options = SpikeOptions.Parse([.. OS.GetCmdlineUserArgs()]);
            if (options.ThemeSpecified)
                _settings.LightTheme = options.Theme == "light";
            _runtime = new SpikeRuntime(options);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or IOException)
        {
            BuildChrome();
            ShowStartupError(ex.Message);
            return;
        }

        BuildChrome();
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

    public override void _Input(InputEvent @event)
    {
        if (_session is null
            || _interaction.Surface != ClientSurface.World
            || @event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        char? movement = WorldMovement(key);
        if (movement is null)
            return;
        SendKey(movement.Value);
        GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key || _session is null)
            return;

        if (key.Keycode == Key.F6)
        {
            ToggleTheme();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (key.Keycode == Key.F7)
        {
            CycleScale();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (key.Keycode == Key.Quoteleft && _interaction.Surface == ClientSurface.World)
        {
            ToggleCompass();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_interaction.IsCreationText)
        {
            if (key.Keycode == Key.Escape)
            {
                SendKey('[');
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        char? canonical = MapKey(key);
        if (canonical is null)
            return;
        SendKey(canonical.Value);
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
        bool becameVisible = _visibleSurface != interaction.Surface;
        _visibleSurface = interaction.Surface;

        bool isCreation = interaction.IsCreation;
        bool isWorld = interaction.Surface is ClientSurface.World or ClientSurface.DirectionPrompt;
        bool isConversation = interaction.Surface == ClientSurface.Conversation;
        _creation.Visible = isCreation;
        _world.Visible = isWorld;
        _conversation.Visible = isConversation;
        _legacy.Visible = !isCreation && !isWorld && !isConversation;

        if (isCreation)
        {
            _place.Text = "Character creation";
            _creation.UpdateView(interaction, becameVisible || !previous.Equals(interaction.Surface));
        }
        else if (isWorld)
        {
            _place.Text = WorldTitle();
            _world.UpdateView(frame, interaction, StatusText());
        }
        else if (isConversation)
        {
            _place.Text = interaction.Title;
            _conversation.UpdateView(interaction, becameVisible);
        }
        else
        {
            _place.Text = interaction.Title.Length > 0 ? interaction.Title : "The road";
            _legacy.UpdateView(frame);
        }

        _moveButton.Visible = interaction.Surface == ClientSurface.World;
        _compass.Visible = _compassOpen && interaction.Surface == ClientSurface.World;
        ScheduleLayoutPass();
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
            case "scale":
                CycleScale();
                return new PilotResponse { Ok = true };
            case "stress":
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
        _scale = UiScaleTokens.FromIndex(_settings?.ScaleIndex ?? 0);
        _palette = _settings?.LightTheme == true ? UiPalette.Light : UiPalette.Dark;

        _background = new ColorRect();
        _background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        _outerMargin = new MarginContainer();
        _outerMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_outerMargin);
        _shell = new VBoxContainer();
        _outerMargin.AddChild(_shell);

        var header = new HBoxContainer();
        _shell.AddChild(header);
        _wordmark = new Label { Text = "AEGIS", CustomMinimumSize = new Vector2(132, 0) };
        header.AddChild(_wordmark);
        header.AddChild(new VSeparator());
        _place = new Label
        {
            Text = "The first road",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        header.AddChild(_place);

        _moveButton = new Button { Text = "Move  ~" };
        _moveButton.Pressed += ToggleCompass;
        header.AddChild(_moveButton);
        _scaleButton = new Button();
        _scaleButton.Pressed += CycleScale;
        header.AddChild(_scaleButton);
        _themeButton = new Button();
        _themeButton.Pressed += ToggleTheme;
        header.AddChild(_themeButton);

        _surfacePanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _shell.AddChild(_surfacePanel);
        _screenHost = new Control();
        _surfacePanel.AddChild(_screenHost);

        _creation = new CreationScreen(_fonts, _scale, _palette);
        _creation.KeyRequested += SendKey;
        AddScreen(_creation);
        _world = new WorldScreen(_fonts, _scale, _palette, _settings?.LightTheme == true);
        AddScreen(_world);
        _conversation = new ConversationScreen(_fonts, _scale, _palette);
        _conversation.KeyRequested += SendKey;
        AddScreen(_conversation);
        _legacy = new LegacyScreen(_fonts.Mono, _scale, _palette, _settings?.LightTheme == true);
        AddScreen(_legacy);

        _compass = BuildCompass();
        _compass.Visible = false;
        AddChild(_compass);

        GetViewport().SizeChanged += ScheduleLayoutPass;
        RefreshVisuals();
    }

    private void AddScreen(Control screen)
    {
        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        screen.Visible = false;
        _screenHost.AddChild(screen);
    }

    private PanelContainer BuildCompass()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(250, 250),
            MouseFilter = MouseFilterEnum.Stop,
        };
        var margin = new MarginContainer();
        panel.AddChild(margin);
        var stack = new VBoxContainer();
        margin.AddChild(stack);
        var title = new Label
        {
            Text = "IRON ROSE",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        stack.AddChild(title);
        var grid = new GridContainer { Columns = 3 };
        stack.AddChild(grid);
        foreach ((string label, char key) in new[]
        {
            ("NW", 'y'), ("N", 'k'), ("NE", 'u'),
            ("W", 'h'), ("WAIT", '.'), ("E", 'l'),
            ("SW", 'b'), ("S", 'j'), ("SE", 'n'),
        })
        {
            var button = new Button { Text = label, CustomMinimumSize = new Vector2(62, 48) };
            char captured = key;
            button.Pressed += () => SendKey(captured);
            grid.AddChild(button);
        }
        return panel;
    }

    private void RefreshVisuals()
    {
        _scale = UiScaleTokens.FromIndex(_settings?.ScaleIndex ?? 0);
        _palette = _settings?.LightTheme == true ? UiPalette.Light : UiPalette.Dark;
        Theme = UiThemeFactory.Build(_fonts, _scale, _palette);
        _background.Color = _palette.Background;
        _outerMargin.AddThemeConstantOverride("margin_left", _scale.Space4);
        _outerMargin.AddThemeConstantOverride("margin_right", _scale.Space4);
        _outerMargin.AddThemeConstantOverride("margin_top", _scale.Space3);
        _outerMargin.AddThemeConstantOverride("margin_bottom", _scale.Space3);
        _shell.AddThemeConstantOverride("separation", _scale.Space2);
        UiThemeFactory.Mark(_wordmark, "display", _fonts, _scale, _palette);
        UiThemeFactory.Mark(_place, "muted", _fonts, _scale, _palette);
        _surfacePanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(_palette.Panel, _palette.Muted, _scale, 0));
        _compass.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(_palette.Raised, _palette.Accent, _scale, _scale.Space2));
        _creation.ApplyVisuals(_scale, _palette);
        _world.ApplyVisuals(_scale, _palette, _settings?.LightTheme == true);
        _conversation.ApplyVisuals(_scale, _palette);
        _legacy.ApplyVisuals(_scale, _palette, _settings?.LightTheme == true);
        _scaleButton.Text = $"{_scale.Percent}%  F7";
        _themeButton.Text = _settings?.LightTheme == true ? "Dark iron  F6" : "Light field  F6";

        if (_frame is not null)
            ((IFrameSink)this).Draw(_frame, _interaction);
        ScheduleLayoutPass();
    }

    private void ToggleTheme()
    {
        if (_settings is null)
            return;
        _settings.LightTheme = !_settings.LightTheme;
        _settings.Save();
        RefreshVisuals();
    }

    private void CycleScale()
    {
        if (_settings is null)
            return;
        _settings.ScaleIndex = (_settings.ScaleIndex + 1) % 5;
        _settings.Save();
        RefreshVisuals();
    }

    private void ToggleCompass()
    {
        if (_interaction.Surface != ClientSurface.World)
            return;
        _compassOpen = !_compassOpen;
        _compass.Visible = _compassOpen;
        _moveButton.Text = _compassOpen ? "Close move  ~" : "Move  ~";
        ScheduleLayoutPass();
    }

    private void ScheduleLayoutPass()
    {
        Callable.From(ClampFloatingControls).CallDeferred();
    }

    private void ClampFloatingControls()
    {
        if (!_compassOpen || !_compass.Visible)
            return;
        Vector2 viewport = GetViewportRect().Size;
        if (_compass.Position == Vector2.Zero)
            _compass.Position = new Vector2(_scale.Space4, viewport.Y - _compass.Size.Y - _scale.Space4);
        _compass.Position = new Vector2(
            Mathf.Clamp(_compass.Position.X, 0, Math.Max(0, viewport.X - _compass.Size.X)),
            Mathf.Clamp(_compass.Position.Y, 0, Math.Max(0, viewport.Y - _compass.Size.Y)));
    }

    private static char? WorldMovement(InputEventKey key)
    {
        if (key.CtrlPressed && key.Keycode == Key.Left) return 'y';
        if (key.CtrlPressed && key.Keycode == Key.Right) return 'u';
        if (key.AltPressed && key.Keycode == Key.Left) return 'b';
        if (key.AltPressed && key.Keycode == Key.Right) return 'n';
        if (key.Keycode == Key.Up) return 'k';
        if (key.Keycode == Key.Down) return 'j';
        if (key.Keycode == Key.Left) return 'h';
        if (key.Keycode == Key.Right) return 'l';
        return null;
    }

    private char? MapKey(InputEventKey key)
    {
        if (key.Keycode is Key.Enter or Key.KpEnter && _interaction.Surface == ClientSurface.CreationReview)
            return '.';
        if (key.Keycode == Key.Escape)
            return _interaction.IsCreation ? '[' : 'q';
        if (key.Unicode > 0 && key.Unicode <= char.MaxValue)
            return (char)key.Unicode;
        return null;
    }

    private void SendKey(char key)
    {
        _session?.Writer.TryWrite(new HostMessage.Key(key));
    }

    private void MoveFocus(int delta)
    {
        Control active = ActiveScreen();
        Button[] buttons = FindEnabledButtons(active).ToArray();
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

    private Control ActiveScreen() =>
        _creation.Visible ? _creation
        : _conversation.Visible ? _conversation
        : _legacy.Visible ? _legacy
        : _world;

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
                $"Cycle {game.Cycle}  |  Turn {game.Turn}",
                $"{game.Season}  |  {game.WeatherRead(game.LocalClimate)}",
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
        return $"{game.World.Name}  |  {game.World.SettlementName}  |  Cycle {game.Cycle}";
    }

    private void ShowStartupError(string message)
    {
        _place.Text = "The window could not open";
        _moveButton.Visible = false;
        _scaleButton.Visible = false;
        _themeButton.Visible = false;
        var label = new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _screenHost.AddChild(label);
        RefreshVisuals();
    }
}
