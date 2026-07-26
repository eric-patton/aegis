using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed partial class ResourceMeter : Control
{
    private readonly ProgressBar _bar;
    private readonly Label _icon;
    private readonly Label _value;

    public ResourceMeter(string icon)
    {
        CustomMinimumSize = new Vector2(0, 30);
        MouseFilter = MouseFilterEnum.Ignore;

        _bar = new ProgressBar
        {
            ShowPercentage = false,
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _bar.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_bar);

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(row);
        _icon = new Label
        {
            Text = icon,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(_icon);
        _value = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(_value);
    }

    public void ApplyVisuals(
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette,
        Color fill)
    {
        CustomMinimumSize = new Vector2(0, Math.Max(26, scale.Space3 + scale.Space1));
        _bar.AddThemeStyleboxOverride(
            "background",
            UiThemeFactory.BorderBox(palette.Panel, fill, scale, scale.Space1));
        _bar.AddThemeStyleboxOverride(
            "fill",
            UiThemeFactory.BorderBox(fill, fill, scale, scale.Space1));
        _icon.AddThemeFontOverride("font", fonts.MonoSemibold);
        _icon.AddThemeFontSizeOverride("font_size", scale.Metadata);
        _icon.AddThemeColorOverride("font_color", palette.Text);
        _value.AddThemeFontOverride("font", fonts.MonoSemibold);
        _value.AddThemeFontSizeOverride("font_size", scale.Metadata);
        _value.AddThemeColorOverride("font_color", palette.Text);
    }

    public void UpdateValue(int value, int maximum)
    {
        _bar.MaxValue = Math.Max(1, maximum);
        _bar.Value = Math.Clamp(value, 0, Math.Max(1, maximum));
        _value.Text = $"{value}/{maximum}";
    }
}

internal sealed class ActivityFeedState
{
    private readonly FollowTailState _follow = new();
    private LogEntry[] _entries = [];

    public event Action? Changed;

    public ActivityFilter Filter { get; private set; }
    public IReadOnlyList<LogEntry> Entries => _entries;
    public bool Following => _follow.Following;
    public bool HasNewEntries => _follow.HasNewEntries;

    public IEnumerable<LogEntry> FilteredEntries =>
        _entries.Where(entry => ActivityLog.Includes(Filter, entry.Tone));

    public void Open(IReadOnlyList<LogEntry> entries)
    {
        _entries = [.. entries];
        _follow.Open(_entries.Length);
        Changed?.Invoke();
    }

    public void SetEntries(IReadOnlyList<LogEntry> entries)
    {
        _follow.EntriesChanged(entries.Count);
        _entries = [.. entries];
        Changed?.Invoke();
    }

    public void SetFilter(ActivityFilter filter)
    {
        if (Filter == filter)
            return;
        Filter = filter;
        Changed?.Invoke();
    }

    public void UserScrolled(double value, double maximum, double page)
    {
        bool wasFollowing = _follow.Following;
        bool hadNewEntries = _follow.HasNewEntries;
        _follow.UserScrolled(value, maximum, page);
        if (wasFollowing != _follow.Following || hadNewEntries != _follow.HasNewEntries)
            Changed?.Invoke();
    }

    public void Resume()
    {
        _follow.Resume();
        Changed?.Invoke();
    }
}

internal sealed partial class ActivityLogView : VBoxContainer
{
    private readonly ClientFonts _fonts;
    private readonly ActivityFeedState _state;
    private readonly Dictionary<ActivityFilter, Button> _filters = [];
    private readonly RichTextLabel _log;
    private readonly Button _latest;
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private bool _programmaticScroll;

    public ActivityLogView(
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette,
        ActivityFeedState state)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        _state = state;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        var filterRow = new HBoxContainer();
        AddChild(filterRow);
        foreach ((ActivityFilter filter, string label) in new[]
        {
            (ActivityFilter.All, "All"),
            (ActivityFilter.Field, "Field"),
            (ActivityFilter.Combat, "Combat"),
            (ActivityFilter.Words, "Words"),
        })
        {
            var button = new Button
            {
                Text = label,
                ToggleMode = true,
                FocusMode = FocusModeEnum.All,
            };
            button.Pressed += () => _state.SetFilter(filter);
            filterRow.AddChild(button);
            _filters.Add(filter, button);
        }

        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = true,
            ScrollFollowing = false,
            FocusMode = FocusModeEnum.All,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        AddChild(_log);
        _log.GetVScrollBar().ValueChanged += OnScrollChanged;

        _latest = new Button
        {
            Text = "Return to latest",
            Visible = false,
        };
        _latest.Pressed += _state.Resume;
        AddChild(_latest);

        _state.Changed += Refresh;
        ApplyVisuals(scale, palette);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        AddThemeConstantOverride("separation", scale.Space1);
        foreach ((ActivityFilter filter, Button button) in _filters)
        {
            Color color = FilterColor(filter, palette);
            button.AddThemeColorOverride("font_color", color);
            button.AddThemeColorOverride("font_hover_color", color);
            button.AddThemeColorOverride("font_pressed_color", palette.Background);
            button.AddThemeStyleboxOverride(
                "pressed",
                UiThemeFactory.BorderBox(color, color, scale, scale.Space1));
        }
        _log.AddThemeFontOverride("normal_font", _fonts.Body);
        _log.AddThemeFontOverride("bold_font", _fonts.BodySemibold);
        _log.AddThemeFontSizeOverride("normal_font_size", scale.Metadata);
        _log.AddThemeFontSizeOverride("bold_font_size", scale.Metadata);
        _log.AddThemeColorOverride("default_color", palette.Text);
        Refresh();
    }

    public void FocusLog() => _log.CallDeferred(Control.MethodName.GrabFocus);

    private void Refresh()
    {
        double previous = _log.GetVScrollBar().Value;
        _log.Text = WorldScreen.LogMarkup(_state.FilteredEntries, _palette);
        foreach ((ActivityFilter filter, Button button) in _filters)
            button.SetPressedNoSignal(_state.Filter == filter);
        _latest.Visible = _state.HasNewEntries;
        if (_state.Following)
            QueueScrollToBottom();
        else
            Callable.From(() => RestoreScroll(previous)).CallDeferred();
    }

    private void OnScrollChanged(double value)
    {
        if (_programmaticScroll)
            return;
        VScrollBar bar = _log.GetVScrollBar();
        _state.UserScrolled(value, bar.MaxValue, bar.Page);
    }

    private void ScrollToBottom()
    {
        VScrollBar bar = _log.GetVScrollBar();
        RestoreScroll(bar.MaxValue);
    }

    private async void QueueScrollToBottom()
    {
        if (!IsInsideTree())
        {
            Callable.From(QueueScrollToBottom).CallDeferred();
            return;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ScrollToBottom();
    }

    private void RestoreScroll(double value)
    {
        _programmaticScroll = true;
        _log.GetVScrollBar().Value = value;
        _programmaticScroll = false;
    }

    private static Color FilterColor(ActivityFilter filter, UiPalette palette) => filter switch
    {
        ActivityFilter.Field => palette.Field,
        ActivityFilter.Combat => palette.Combat,
        ActivityFilter.Words => palette.Words,
        _ => palette.Text,
    };
}

internal sealed partial class ModernTaskScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly Label _title;
    private readonly RichTextLabel _body;
    private readonly VBoxContainer _actions;
    private readonly Label _selected;
    private readonly Button _cancel;
    private readonly GridContainer _layout;
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private char? _selectedKey;

    public event Action<char>? KeyRequested;

    public ModernTaskScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;

        var stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(stack);
        _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        stack.AddChild(_title);

        _layout = new GridContainer
        {
            Columns = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        stack.AddChild(_layout);

        var bodyPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _layout.AddChild(bodyPanel);
        _body = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollActive = true,
            FocusMode = FocusModeEnum.All,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        bodyPanel.AddChild(WorldScreen.Wrap(_body));

        var actionPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(390, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _layout.AddChild(actionPanel);
        var actionScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        actionPanel.AddChild(WorldScreen.Wrap(actionScroll));
        _actions = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        actionScroll.AddChild(_actions);

        var commitment = new PanelContainer();
        stack.AddChild(commitment);
        var commitmentRow = new HBoxContainer();
        commitment.AddChild(WorldScreen.Wrap(commitmentRow));
        _selected = new Label
        {
            Text = "Choose an action.",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        commitmentRow.AddChild(_selected);
        _cancel = new Button { Text = "Cancel  Esc" };
        _cancel.Pressed += () => KeyRequested?.Invoke('q');
        commitmentRow.AddChild(_cancel);
        ApplyVisuals(scale, palette);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        AddThemeConstantOverride("margin_left", scale.Space3);
        AddThemeConstantOverride("margin_right", scale.Space3);
        AddThemeConstantOverride("margin_top", scale.Space3);
        AddThemeConstantOverride("margin_bottom", scale.Space3);
        _layout.AddThemeConstantOverride("h_separation", scale.Space2);
        _layout.AddThemeConstantOverride("v_separation", scale.Space2);
        UiThemeFactory.Mark(_title, "heading", _fonts, scale, palette);
        _selected.AddThemeColorOverride("font_color", palette.Muted);
    }

    public void ApplyLayout(float viewportWidth)
    {
        bool split = viewportWidth >= 1400 && _scale.Scale < 1.5f;
        _layout.Columns = split ? 2 : 1;
        foreach (Control child in _layout.GetChildren().OfType<Control>())
        {
            child.CustomMinimumSize = split
                ? child.CustomMinimumSize with { Y = 0 }
                : new Vector2(0, Math.Max(220, 220 * _scale.Scale));
        }
    }

    public void UpdateView(ClientInteractionContext context, bool becameVisible)
    {
        _title.Text = context.Task?.Title.Length > 0
            ? context.Task.Title
            : context.Title.Length > 0
                ? context.Title
                : "Choose";
        _body.Text = context.Task?.Body ?? context.Detail;
        RebuildActions(context.Actions, becameVisible);
    }

    public bool MoveSelection(int delta)
    {
        Button[] buttons = _actions.GetChildren()
            .OfType<Button>()
            .Where(button => !button.Disabled)
            .ToArray();
        if (buttons.Length == 0)
            return false;
        int index = Array.IndexOf(buttons, GetViewport().GuiGetFocusOwner());
        if (index < 0 && _selectedKey is { } selected)
            index = Array.FindIndex(buttons, button => ButtonKey(button) == selected);
        index = (index + delta + buttons.Length) % buttons.Length;
        Select(buttons[index]);
        buttons[index].GrabFocus();
        return true;
    }

    private void RebuildActions(IReadOnlyList<ClientAction> actions, bool focusFirst)
    {
        foreach (Node child in _actions.GetChildren())
        {
            _actions.RemoveChild(child);
            child.QueueFree();
        }

        var buttons = new List<Button>();
        foreach (ClientAction action in actions)
        {
            var button = new Button
            {
                Text = $"{action.Key}  {action.Label}",
                Disabled = !action.Enabled,
                Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, Math.Max(58, _scale.Space4 * 2)),
            };
            button.SetMeta("canonical_key", action.Key.ToString());
            button.SetMeta("action_label", action.Label);
            button.FocusEntered += () => Select(button);
            button.MouseEntered += () => Select(button);
            char key = action.Key;
            button.Pressed += () => KeyRequested?.Invoke(key);
            _actions.AddChild(button);
            buttons.Add(button);
        }

        for (int index = 0; index < buttons.Count; index++)
        {
            buttons[index].FocusNeighborTop = buttons[(index - 1 + buttons.Count) % buttons.Count].GetPath();
            buttons[index].FocusNeighborBottom = buttons[(index + 1) % buttons.Count].GetPath();
            buttons[index].FocusNeighborLeft = buttons[index].GetPath();
            buttons[index].FocusNeighborRight = buttons[index].GetPath();
        }

        Button? restore = _selectedKey is { } selectedKey
            ? buttons.FirstOrDefault(button => ButtonKey(button) == selectedKey && !button.Disabled)
            : null;
        if (focusFirst || restore is not null)
            (restore ?? buttons.FirstOrDefault(button => !button.Disabled))
                ?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void Select(Button button)
    {
        _selectedKey = ButtonKey(button);
        _selected.Text = button.GetMeta("action_label", "Choose an action.").AsString();
    }

    private static char? ButtonKey(Button button)
    {
        string value = button.GetMeta("canonical_key", "").AsString();
        return value.Length == 1 ? value[0] : null;
    }
}

internal sealed partial class HelpOverlay : MarginContainer
{
    private static readonly (string Title, string Body)[] Articles =
    [
        (
            "Movement",
            """
            Move with the arrow keys or HJKL. Ctrl+Left and Ctrl+Right move northwest and northeast. Alt+Left and Alt+Right move southwest and southeast. Ctrl+minus, Ctrl+plus, and Ctrl+0 change map zoom without changing the rest of the interface.
            """),
        (
            "World actions",
            """
            Enter and exit with greater-than and less-than. Grab with G, rest with R, camp with M, and wait with period. Available contextual actions appear in the focused task surface.
            """),
        (
            "Character and pack",
            """
            Open Character with C and Pack with I. Every action is available by keyboard and pointer. Disabled actions include a written reason.
            """),
        (
            "Activity and history",
            """
            The Activity sidebar keeps the current session log. Filter it by Field, Combat, or Words. Open Journal to read the full history. Return to latest resumes automatic bottom-follow.
            """),
        (
            "Appearance",
            """
            Light and dark themes keep identical layout and state meaning. UI scale and map zoom are separate. Focus, selection, disabled actions, warnings, and errors never rely on color alone.
            """),
    ];

    private readonly ClientFonts _fonts;
    private readonly LineEdit _search;
    private readonly ItemList _topics;
    private readonly RichTextLabel _article;
    private readonly Label _title;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    public event Action? CloseRequested;

    public HelpOverlay(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        MouseFilter = MouseFilterEnum.Stop;

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);
        var stack = new VBoxContainer();
        panel.AddChild(WorldScreen.Wrap(stack));

        var header = new HBoxContainer();
        stack.AddChild(header);
        _title = new Label
        {
            Text = "Help",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        header.AddChild(_title);
        var close = new Button { Text = "Return  Esc" };
        close.Pressed += () => CloseRequested?.Invoke();
        header.AddChild(close);

        _search = new LineEdit { PlaceholderText = "Search help" };
        _search.TextChanged += Filter;
        stack.AddChild(_search);

        var body = new GridContainer
        {
            Columns = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        stack.AddChild(body);
        _topics = new ItemList
        {
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _topics.ItemSelected += ShowArticle;
        body.AddChild(_topics);
        _article = new RichTextLabel
        {
            ScrollActive = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        body.AddChild(_article);
        ApplyVisuals(scale, palette);
        Filter("");
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        AddThemeConstantOverride("margin_left", scale.Space4);
        AddThemeConstantOverride("margin_right", scale.Space4);
        AddThemeConstantOverride("margin_top", scale.Space3);
        AddThemeConstantOverride("margin_bottom", scale.Space3);
        UiThemeFactory.Mark(_title, "heading", _fonts, scale, palette);
    }

    public void Open()
    {
        Visible = true;
        _search.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void Filter(string query)
    {
        _topics.Clear();
        for (int index = 0; index < Articles.Length; index++)
        {
            (string title, string body) = Articles[index];
            if (query.Length > 0
                && !title.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !body.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;
            int item = _topics.AddItem(title);
            _topics.SetItemMetadata(item, index);
        }

        if (_topics.ItemCount > 0)
        {
            _topics.Select(0);
            ShowArticle(0);
        }
        else
        {
            _article.Text = "No help topic matches that search.";
        }
    }

    private void ShowArticle(long item)
    {
        int article = _topics.GetItemMetadata((int)item).AsInt32();
        _article.Text = Articles[article].Body;
    }
}
