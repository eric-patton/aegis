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
    private readonly ActivityFeedState _activityState = new();

    private ColorRect _background = null!;
    private MarginContainer _outerMargin = null!;
    private VBoxContainer _shell = null!;
    private HBoxContainer _header = null!;
    private Label _wordmark = null!;
    private Label _place = null!;
    private Button _characterButton = null!;
    private Button _packButton = null!;
    private Button _journalButton = null!;
    private Button _helpButton = null!;
    private PanelContainer _surfacePanel = null!;
    private Control _screenHost = null!;
    private CreationScreen _creation = null!;
    private WorldScreen _world = null!;
    private ConversationScreen _conversation = null!;
    private ModernTaskScreen _task = null!;
    private CharacterLedgerScreen _character = null!;
    private PackScreen _pack = null!;
    private HistoryOverlay _history = null!;
    private HelpOverlay _help = null!;
    private bool _historyOpen;
    private bool _helpOpen;
    private Control? _overlayReturnFocus;

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
        if (_session is null || @event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        if (_historyOpen)
        {
            if (key.Keycode == Key.Escape)
            {
                CloseHistory();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (_helpOpen)
        {
            if (key.Keycode == Key.Escape)
            {
                CloseHelp();
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (_interaction.IsCreationText
            && key.Keycode is Key.Enter or Key.KpEnter or Key.Escape)
        {
            SendKey(key.Keycode == Key.Escape ? '[' : '.');
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_interaction.Surface == ClientSurface.CreationChoice && _creation.HandleKey(key))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_interaction.Surface == ClientSurface.Conversation
            && key.Keycode is Key.Up or Key.Down
            && _conversation.MoveSelection(key.Keycode == Key.Up ? -1 : 1))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_task.Visible
            && key.Keycode is Key.Up or Key.Down
            && _task.MoveSelection(key.Keycode == Key.Up ? -1 : 1))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_character.Visible
            && key.Keycode is Key.Up or Key.Down
            && _character.MoveSelection(key.Keycode == Key.Up ? -1 : 1))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_pack.Visible
            && key.Keycode is Key.Up or Key.Down
            && _pack.MoveSelection(key.Keycode == Key.Up ? -1 : 1))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!WorldInputChord.AcceptsDirectionalKeys(_interaction.Surface))
            return;

        if (_interaction.Surface == ClientSurface.World
            && key.CtrlPressed
            && key.Keycode is Key.Minus or Key.KpSubtract)
        {
            _world.SetZoom((_settings?.MapZoomIndex ?? 0) - 1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_interaction.Surface == ClientSurface.World
            && key.CtrlPressed
            && key.Keycode is Key.Equal or Key.KpAdd)
        {
            _world.SetZoom((_settings?.MapZoomIndex ?? 0) + 1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_interaction.Surface == ClientSurface.World
            && key.CtrlPressed
            && key.Keycode == Key.Key0)
        {
            _world.SetZoom(0);
            GetViewport().SetInputAsHandled();
            return;
        }

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

        if (_historyOpen)
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
        if (interaction.IsCreation && _historyOpen)
            CloseHistory();
        if (interaction.IsCreation && _helpOpen)
            CloseHelp();

        bool isCreation = interaction.IsCreation;
        bool isWorld = interaction.Surface is
            ClientSurface.World or
            ClientSurface.DirectionPrompt or
            ClientSurface.Menu;
        bool isConversation = interaction.Surface == ClientSurface.Conversation;
        bool isTask = interaction.Surface == ClientSurface.Menu;
        bool isCharacter = interaction.Surface == ClientSurface.Character;
        bool isPack = interaction.Surface == ClientSurface.Equipment;
        _creation.Visible = isCreation;
        _world.Visible = isWorld;
        _conversation.Visible = isConversation;
        _task.Visible = isTask;
        _character.Visible = isCharacter;
        _pack.Visible = isPack;
        _activityState.SetEntries(interaction.Transcript);
        WorldHudPresentation hud = CurrentHud();

        if (isCreation)
        {
            _place.Text = "Character creation";
            _creation.UpdateView(interaction, becameVisible || !previous.Equals(interaction.Surface));
        }
        else if (isWorld)
        {
            _place.Text = WorldTitle();
            _world.UpdateView(frame, interaction, hud, _settings.MapZoomIndex);
            if (isTask)
                _task.UpdateView(interaction, becameVisible);
            if (_historyOpen)
                _history.UpdateEntries(interaction.Transcript);
        }
        else if (isConversation)
        {
            _place.Text = interaction.Title;
            _conversation.UpdateView(interaction, becameVisible, hud);
            if (_historyOpen)
                _history.UpdateEntries(interaction.Transcript);
        }
        else if (isTask)
        {
            _place.Text = interaction.Title.Length > 0 ? interaction.Title : "The road";
            _task.UpdateView(interaction, becameVisible);
        }
        else if (isCharacter)
        {
            _place.Text = "Character";
            _character.UpdateView(interaction, becameVisible);
        }
        else if (isPack)
        {
            _place.Text = "Inventory and equipment";
            _pack.UpdateView(interaction, becameVisible);
        }

        bool launcherVisible = !isCreation;
        _header.Visible = launcherVisible;
        _characterButton.Visible = interaction.Surface == ClientSurface.World;
        _packButton.Visible = interaction.Surface == ClientSurface.World;
        _journalButton.Visible = launcherVisible;
        _helpButton.Visible = launcherVisible;
        ScheduleLayoutPass();
    }

    public PilotResponse? HandlePresentation(PilotRequest request)
    {
        switch (request.Action)
        {
            case "compass":
                return new PilotResponse
                {
                    Ok = false,
                    Error = "the movement panel is not part of the default world shell",
                };
            case "history":
            case "log":
                if (_interaction.Surface != ClientSurface.World)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "history is available only on the world view",
                    };
                ToggleHistory();
                return new PilotResponse { Ok = true };
            case "activity":
                if (_interaction.Surface != ClientSurface.World)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "the Activity drawer is available only on the world view",
                    };
                _world.ToggleSidebar();
                return new PilotResponse { Ok = true };
            case "theme":
                ToggleTheme();
                return new PilotResponse { Ok = true };
            case "help":
            case "guide":
                ToggleHelp();
                return new PilotResponse { Ok = true };
            case "scale":
                CycleScale();
                return new PilotResponse { Ok = true };
            case "zoom-in":
                if (_interaction.Surface != ClientSurface.World)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "map zoom is available only on the world view",
                    };
                _world.SetZoom((_settings?.MapZoomIndex ?? 0) + 1);
                return new PilotResponse { Ok = true };
            case "zoom-out":
                if (_interaction.Surface != ClientSurface.World)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "map zoom is available only on the world view",
                    };
                _world.SetZoom((_settings?.MapZoomIndex ?? 0) - 1);
                return new PilotResponse { Ok = true };
            case "zoom-reset":
                if (_interaction.Surface != ClientSurface.World)
                    return new PilotResponse
                    {
                        Ok = false,
                        Error = "map zoom is available only on the world view",
                    };
                _world.SetZoom(0);
                return new PilotResponse { Ok = true };
            case "stress":
                return new PilotResponse { Ok = true };
            case "focus-check":
                return _creation.EntryHasFocus
                    ? new PilotResponse { Ok = true }
                    : new PilotResponse
                    {
                        Ok = false,
                        Error = "the creation text entry does not own keyboard focus",
                    };
            case "close":
                if (_historyOpen)
                    CloseHistory();
                else if (_helpOpen)
                    CloseHelp();
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

        _header = new HBoxContainer();
        _shell.AddChild(_header);
        _wordmark = new Label { Text = "AEGIS", CustomMinimumSize = new Vector2(132, 0) };
        _header.AddChild(_wordmark);
        _header.AddChild(new VSeparator());
        _place = new Label
        {
            Text = "The first road",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _header.AddChild(_place);

        _characterButton = new Button { Text = "Character" };
        _characterButton.Pressed += () => SendKey('c');
        _header.AddChild(_characterButton);
        _packButton = new Button { Text = "Pack" };
        _packButton.Pressed += () => SendKey('i');
        _header.AddChild(_packButton);
        _journalButton = new Button { Text = "Journal" };
        _journalButton.Pressed += ToggleHistory;
        _header.AddChild(_journalButton);
        _helpButton = new Button { Text = "Help" };
        _helpButton.Pressed += ToggleHelp;
        _header.AddChild(_helpButton);

        _surfacePanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _shell.AddChild(_surfacePanel);
        _screenHost = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _screenHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _surfacePanel.AddChild(_screenHost);

        _creation = new CreationScreen(_fonts, _scale, _palette);
        _creation.KeyRequested += SendKey;
        AddScreen(_creation);
        _world = new WorldScreen(
            _fonts,
            _scale,
            _palette,
            _settings?.LightTheme == true,
            _activityState);
        _world.MapZoomChanged += SaveMapZoom;
        AddScreen(_world);
        _conversation = new ConversationScreen(_fonts, _scale, _palette);
        _conversation.KeyRequested += SendKey;
        AddScreen(_conversation);
        _task = new ModernTaskScreen(_fonts, _scale, _palette);
        _task.KeyRequested += SendKey;
        AddScreen(_task);
        _character = new CharacterLedgerScreen(_fonts, _scale, _palette);
        _character.KeyRequested += SendKey;
        AddScreen(_character);
        _pack = new PackScreen(_fonts, _scale, _palette);
        _pack.KeyRequested += SendKey;
        AddScreen(_pack);

        _history = new HistoryOverlay(_fonts, _scale, _palette, _activityState);
        _history.CloseRequested += CloseHistory;
        _history.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _history.Visible = false;
        AddChild(_history);

        _help = new HelpOverlay(_fonts, _scale, _palette);
        _help.CloseRequested += CloseHelp;
        _help.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _help.Visible = false;
        AddChild(_help);

        GetViewport().SizeChanged += ScheduleLayoutPass;
        RefreshVisuals();
    }

    private void AddScreen(Control screen)
    {
        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        screen.Visible = false;
        _screenHost.AddChild(screen);
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
        _creation.ApplyVisuals(_scale, _palette);
        _world.ApplyVisuals(_scale, _palette, _settings?.LightTheme == true);
        _conversation.ApplyVisuals(_scale, _palette);
        _task.ApplyVisuals(_scale, _palette);
        _character.ApplyVisuals(_scale, _palette);
        _pack.ApplyVisuals(_scale, _palette);
        _history.ApplyVisuals(_scale, _palette);
        _help.ApplyVisuals(_scale, _palette);

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

    private void SaveMapZoom(int index)
    {
        if (_settings is null)
            return;
        _settings.MapZoomIndex = MapZoom.ClampIndex(index);
        _settings.Save();
    }

    private void ToggleHelp()
    {
        if (_helpOpen)
        {
            CloseHelp();
            return;
        }
        if (_interaction.IsCreation)
            return;
        _overlayReturnFocus = GetViewport().GuiGetFocusOwner();
        _helpOpen = true;
        _help.Open();
    }

    private void CloseHelp()
    {
        if (!_helpOpen)
            return;
        _helpOpen = false;
        _help.Visible = false;
        RestoreOverlayFocus();
    }

    private void ToggleHistory()
    {
        if (_interaction.Surface != ClientSurface.World)
            return;
        if (_historyOpen)
        {
            CloseHistory();
            return;
        }
        _overlayReturnFocus = GetViewport().GuiGetFocusOwner();
        _historyOpen = true;
        _history.Open(_interaction.Transcript);
        ScheduleLayoutPass();
    }

    private void CloseHistory()
    {
        _historyOpen = false;
        _history.Visible = false;
        RestoreOverlayFocus();
        ScheduleLayoutPass();
    }

    private void RestoreOverlayFocus()
    {
        if (_overlayReturnFocus is { Visible: true } control && control.IsInsideTree())
            control.CallDeferred(Control.MethodName.GrabFocus);
        else if (_world.Visible)
            _world.CallDeferred(Control.MethodName.GrabFocus);
        _overlayReturnFocus = null;
    }

    private void ScheduleLayoutPass()
    {
        Callable.From(PerformLayoutPass).CallDeferred();
    }

    private void PerformLayoutPass()
    {
        Vector2 viewport = GetViewportRect().Size;
        _place.Visible = !_world.Visible && viewport.X >= 1400 && _scale.Scale < 1.5f;
        _creation.ApplyLayout(viewport.X);
        _world.ApplyLayout(viewport.X);
        _conversation.ApplyLayout(viewport.X);
        _task.ApplyLayout(viewport.X);
        _character.ApplyLayout(viewport.X);
        _pack.ApplyLayout(viewport.X);
    }

    private static char? WorldMovement(InputEventKey key)
    {
        WorldDirectionalKey? direction = key.Keycode switch
        {
            Key.Up => WorldDirectionalKey.Up,
            Key.Down => WorldDirectionalKey.Down,
            Key.Left => WorldDirectionalKey.Left,
            Key.Right => WorldDirectionalKey.Right,
            _ => null,
        };
        return direction is { } value
            ? WorldInputChord.Map(value, key.CtrlPressed, key.AltPressed)
            : null;
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
        if (_creation.Visible && _creation.MoveSelection(delta))
            return;
        if (_conversation.Visible && _conversation.MoveSelection(delta))
            return;
        if (_task.Visible && _task.MoveSelection(delta))
            return;
        if (_character.Visible && _character.MoveSelection(delta))
            return;
        if (_pack.Visible && _pack.MoveSelection(delta))
            return;
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
        _help.Visible ? _help
        : _history.Visible ? _history
        : _creation.Visible ? _creation
        : _conversation.Visible ? _conversation
        : _character.Visible ? _character
        : _pack.Visible ? _pack
        : _task.Visible ? _task
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

    private WorldHudPresentation CurrentHud()
    {
        if (_runtime is null)
            return new WorldHudPresentation(
                "The bearer",
                1,
                1,
                1,
                1,
                1,
                1,
                0,
                0,
                0,
                1,
                0,
                "",
                "",
                "",
                "");
        return WorldHudPresentation.From(_runtime.Game);
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
        _characterButton.Visible = false;
        _packButton.Visible = false;
        _journalButton.Visible = false;
        _helpButton.Visible = false;
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
