using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed partial class HistoryOverlay : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly ActivityFeedState _state;
    private readonly Label _summary;
    private readonly ActivityLogView _history;
    private readonly Button _close;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    public event Action? CloseRequested;

    public HistoryOverlay(
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette,
        ActivityFeedState state)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        _state = state;
        MouseFilter = MouseFilterEnum.Stop;

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);
        var stack = new VBoxContainer();
        panel.AddChild(WorldScreen.Wrap(stack));

        var header = new HBoxContainer();
        stack.AddChild(header);
        var title = new Label
        {
            Text = "History",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        header.AddChild(title);
        _summary = new Label();
        header.AddChild(_summary);
        _close = new Button { Text = "Return  Esc" };
        _close.Pressed += () => CloseRequested?.Invoke();
        header.AddChild(_close);

        _history = new ActivityLogView(fonts, scale, palette, state);
        stack.AddChild(_history);
        ApplyVisuals(scale, palette);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        AddThemeConstantOverride("margin_left", scale.Space4);
        AddThemeConstantOverride("margin_right", scale.Space4);
        AddThemeConstantOverride("margin_top", scale.Space3);
        AddThemeConstantOverride("margin_bottom", scale.Space3);
        UiThemeFactory.Mark(_summary, "muted", _fonts, scale, palette);
        _history.ApplyVisuals(scale, palette);
        _summary.Text = $"{_state.Entries.Count:N0} entries this session";
    }

    public void Open(IReadOnlyList<LogEntry> entries)
    {
        Visible = true;
        _state.SetEntries(entries);
        _summary.Text = $"{_state.Entries.Count:N0} entries this session";
        _history.FocusLog();
    }

    public void UpdateEntries(IReadOnlyList<LogEntry> entries)
    {
        _state.SetEntries(entries);
        _summary.Text = $"{_state.Entries.Count:N0} entries this session";
    }
}

internal sealed partial class IronRoseControl : PanelContainer
{
    private readonly ClientFonts _fonts;
    private readonly Label _title;
    private readonly Label _dragHandle;
    private readonly GridContainer _directions;
    private readonly List<Button> _directionButtons = [];
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private bool _dragging;

    public NormalizedFloatingPosition NormalizedPosition { get; private set; } =
        NormalizedFloatingPosition.Default;

    public event Action<char>? KeyRequested;
    public event Action? CloseRequested;
    public event Action<NormalizedFloatingPosition>? PositionCommitted;

    public IronRoseControl(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        CustomMinimumSize = new Vector2(260, 278);
        MouseFilter = MouseFilterEnum.Stop;

        var stack = new VBoxContainer();
        AddChild(stack);
        var handleRow = new HBoxContainer();
        stack.AddChild(handleRow);
        _dragHandle = new Label
        {
            Text = "MOVE",
            TooltipText = "Drag to reposition",
            MouseDefaultCursorShape = CursorShape.Move,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _dragHandle.GuiInput += OnDragInput;
        handleRow.AddChild(_dragHandle);
        _title = new Label
        {
            Text = "IRON ROSE",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _title.GuiInput += OnDragInput;
        handleRow.AddChild(_title);
        var reset = new Button { Text = "Reset", TooltipText = "Reset position" };
        reset.Pressed += ResetPosition;
        handleRow.AddChild(reset);
        var close = new Button { Text = "Close", TooltipText = "Close movement controls" };
        close.Pressed += () => CloseRequested?.Invoke();
        handleRow.AddChild(close);

        _directions = new GridContainer { Columns = 3 };
        stack.AddChild(_directions);
        foreach ((string label, char key) in new[]
        {
            ("NW", 'y'), ("N", 'k'), ("NE", 'u'),
            ("W", 'h'), ("WAIT", '.'), ("E", 'l'),
            ("SW", 'b'), ("S", 'j'), ("SE", 'n'),
        })
        {
            var button = new Button
            {
                Text = label,
                CustomMinimumSize = new Vector2(68, 52),
                TooltipText = label == "WAIT" ? "Wait one turn" : $"Move {label}",
            };
            char captured = key;
            button.Pressed += () => KeyRequested?.Invoke(captured);
            _directions.AddChild(button);
            _directionButtons.Add(button);
        }
        ApplyVisuals(scale, palette);
    }

    public override void _Ready()
    {
        for (int index = 0; index < _directionButtons.Count; index++)
        {
            int row = index / 3;
            int column = index % 3;
            _directionButtons[index].FocusNeighborTop =
                _directionButtons[((row + 2) % 3) * 3 + column].GetPath();
            _directionButtons[index].FocusNeighborBottom =
                _directionButtons[((row + 1) % 3) * 3 + column].GetPath();
            _directionButtons[index].FocusNeighborLeft =
                _directionButtons[row * 3 + (column + 2) % 3].GetPath();
            _directionButtons[index].FocusNeighborRight =
                _directionButtons[row * 3 + (column + 1) % 3].GetPath();
        }
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        CustomMinimumSize = new Vector2(
            Math.Max(260, 260 * scale.Scale),
            Math.Max(278, 278 * scale.Scale));
        AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, scale.Space2));
        _directions.AddThemeConstantOverride("h_separation", scale.Space1);
        _directions.AddThemeConstantOverride("v_separation", scale.Space1);
        foreach (Button button in _directionButtons)
        {
            button.AddThemeStyleboxOverride(
                "normal",
                UiThemeFactory.BorderBox(palette.Panel, palette.Muted, scale, scale.Space1));
            button.AddThemeStyleboxOverride(
                "hover",
                UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, scale.Space1));
            button.AddThemeStyleboxOverride(
                "pressed",
                UiThemeFactory.BorderBox(palette.Warm, palette.Warm, scale, scale.Space1));
            button.AddThemeStyleboxOverride(
                "focus",
                UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, scale.Space1));
        }
        UiThemeFactory.Mark(_title, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_dragHandle, "muted", _fonts, scale, palette);
    }

    public void SetNormalizedPosition(NormalizedFloatingPosition position)
    {
        NormalizedPosition = new NormalizedFloatingPosition(
            Math.Clamp(position.X, 0, 1),
            Math.Clamp(position.Y, 0, 1));
    }

    public void ClampToViewport(Vector2 viewport, int margin)
    {
        float availableWidth = Math.Max(0, viewport.X - Size.X - margin * 2);
        float availableHeight = Math.Max(0, viewport.Y - Size.Y - margin * 2);
        (float x, float y) = NormalizedPosition.ToPixels(availableWidth, availableHeight);
        Position = new Vector2(
            MathF.Round(margin + x),
            MathF.Round(margin + y));
    }

    private void OnDragInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            }:
                _dragging = true;
                AcceptEvent();
                break;
            case InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: false,
            }:
                if (_dragging)
                {
                    _dragging = false;
                    CommitPosition();
                }
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                Position += motion.Relative;
                ClampPixels();
                AcceptEvent();
                break;
        }
    }

    private void ClampPixels()
    {
        Vector2 viewport = GetViewportRect().Size;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, Math.Max(0, viewport.X - Size.X)),
            Mathf.Clamp(Position.Y, 0, Math.Max(0, viewport.Y - Size.Y)));
    }

    private void CommitPosition()
    {
        Vector2 viewport = GetViewportRect().Size;
        int margin = _scale.Space2;
        float availableWidth = Math.Max(0, viewport.X - Size.X - margin * 2);
        float availableHeight = Math.Max(0, viewport.Y - Size.Y - margin * 2);
        NormalizedPosition = NormalizedFloatingPosition.FromPixels(
            Position.X - margin,
            Position.Y - margin,
            availableWidth,
            availableHeight);
        PositionCommitted?.Invoke(NormalizedPosition);
    }

    private void ResetPosition()
    {
        NormalizedPosition = NormalizedFloatingPosition.Default;
        ClampToViewport(GetViewportRect().Size, _scale.Space2);
        PositionCommitted?.Invoke(NormalizedPosition);
    }
}
