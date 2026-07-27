using System.Text;
using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed partial class CreationScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly Label _eyebrow;
    private readonly HBoxContainer _route;
    private readonly List<PanelContainer> _routeSteps = [];
    private readonly List<Label> _routeLabels = [];
    private readonly ProgressBar _progress;
    private readonly Label _phase;
    private readonly Label _prompt;
    private readonly Label _guidance;
    private readonly LineEdit _entry;
    private readonly RichTextLabel _review;
    private readonly ScrollContainer _choiceScroll;
    private readonly GridContainer _choices;
    private readonly Button _back;
    private readonly Button _continue;
    private readonly Label _hint;
    private readonly VBoxContainer _stack;
    private readonly PanelContainer _panel;
    private readonly MarginContainer _inner;
    private readonly PanelContainer _detailPanel;
    private readonly Label _selectedDetail;
    private readonly HBoxContainer _footer;
    private readonly Dictionary<char, CreationChoiceCard> _choiceCards = [];
    private CreationStage? _stage;
    private char? _selectedKey;
    private string _submittedText = "";
    private bool _suppressEntry;
    private int _currentStep = 1;
    private float _viewportWidth = 1280;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    public event Action<char>? KeyRequested;

    public CreationScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, 0));
        AddChild(_panel);

        _inner = new MarginContainer();
        _panel.AddChild(_inner);
        _stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _inner.AddChild(_stack);

        _eyebrow = new Label();
        _stack.AddChild(_eyebrow);

        _route = new HBoxContainer();
        _stack.AddChild(_route);
        for (int step = 1; step <= 10; step++)
        {
            var stepPanel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            var stepLabel = new Label
            {
                Text = step.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            stepPanel.AddChild(stepLabel);
            _route.AddChild(stepPanel);
            _routeSteps.Add(stepPanel);
            _routeLabels.Add(stepLabel);
        }

        _progress = new ProgressBar
        {
            MinValue = 1,
            MaxValue = 10,
            ShowPercentage = false,
        };
        _stack.AddChild(_progress);

        _phase = new Label { Visible = false };
        _stack.AddChild(_phase);
        _prompt = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _stack.AddChild(_prompt);
        _guidance = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _stack.AddChild(_guidance);

        _entry = new LineEdit
        {
            MaxLength = 14,
            PlaceholderText = "Type a name",
            SelectAllOnFocus = false,
            ContextMenuEnabled = true,
        };
        _entry.TextChanged += OnEntryChanged;
        _entry.TextSubmitted += _ => KeyRequested?.Invoke('.');
        _stack.AddChild(_entry);

        _review = new RichTextLabel
        {
            FitContent = false,
            BbcodeEnabled = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ScrollActive = true,
            ScrollFollowing = false,
        };
        _stack.AddChild(_review);

        _choiceScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _stack.AddChild(_choiceScroll);
        _choices = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _choiceScroll.AddChild(_choices);

        _detailPanel = new PanelContainer();
        _stack.AddChild(_detailPanel);
        _selectedDetail = new Label
        {
            Text = "Select a choice to see what it changes.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _detailPanel.AddChild(WorldScreen.Wrap(_selectedDetail));

        _footer = new HBoxContainer();
        _stack.AddChild(_footer);
        _back = new Button { Text = "Back  Esc" };
        _back.Pressed += () => KeyRequested?.Invoke('[');
        _footer.AddChild(_back);

        _hint = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _footer.AddChild(_hint);

        _continue = new Button();
        _continue.Pressed += SubmitSelection;
        _footer.AddChild(_continue);

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
        _stack.AddThemeConstantOverride("separation", scale.Space2);
        _inner.AddThemeConstantOverride("margin_left", scale.Space4);
        _inner.AddThemeConstantOverride("margin_right", scale.Space4);
        _inner.AddThemeConstantOverride("margin_top", scale.Space3);
        _inner.AddThemeConstantOverride("margin_bottom", scale.Space3);
        _footer.AddThemeConstantOverride("separation", scale.Space2);
        _route.AddThemeConstantOverride("separation", scale.Space1);
        _choices.AddThemeConstantOverride("h_separation", scale.Space2);
        _choices.AddThemeConstantOverride("v_separation", scale.Space1);
        _progress.CustomMinimumSize = new Vector2(0, Math.Max(6, scale.Space1));
        _panel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Muted, scale, 0));
        _detailPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, palette.Accent, scale, 0));
        UiThemeFactory.Mark(_eyebrow, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_phase, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_prompt, "heading", _fonts, scale, palette);
        UiThemeFactory.Mark(_guidance, "muted", _fonts, scale, palette);
        UiThemeFactory.Mark(_hint, "muted", _fonts, scale, palette);
        _selectedDetail.AddThemeColorOverride("font_color", palette.Text);
        foreach (CreationChoiceCard card in _choiceCards.Values)
            card.ApplyVisuals(scale, palette);
        RefreshRouteVisuals();
    }

    public void ApplyLayout(float viewportWidth)
    {
        _viewportWidth = viewportWidth;
        _choices.Columns = viewportWidth >= 1280 && _scale.Scale < 1.5f ? 2 : 1;
        RefreshChoiceCardLayout();
        bool expandedRoute = viewportWidth >= 1180 && _scale.Scale < 1.5f;
        _route.Visible = expandedRoute;
        _progress.Visible = !expandedRoute;
        Callable.From(RefreshChoiceWidths).CallDeferred();
    }

    public void UpdateView(ClientInteractionContext context, bool becameVisible)
    {
        CreationPresentation creation = context.Creation
            ?? throw new InvalidOperationException("Creation screen requires a creation projection.");
        bool stageChanged = creation.Stage != _stage;
        if (stageChanged)
            _selectedKey = null;
        _stage = creation.Stage;
        _currentStep = creation.Step;
        _eyebrow.Text = $"BECOMING  {creation.Step:00} / {creation.TotalSteps:00}";
        _progress.Value = creation.Step;
        RefreshRouteVisuals();
        _prompt.Text = creation.Prompt;
        _phase.Text = creation.PhaseLabel;
        _phase.Visible = creation.PhaseLabel.Length > 0;
        _guidance.Text = creation.Guidance;
        _guidance.Visible = creation.Guidance.Length > 0;
        _back.Disabled = creation.Step == 1;

        bool textStage = creation.Stage is CreationStage.Face or CreationStage.Name;
        bool reviewStage = creation.Stage == CreationStage.Review;
        _entry.Visible = textStage;
        _review.Visible = reviewStage;
        _choiceScroll.Visible = creation.Choices.Length > 0;
        bool choiceStage = creation.Choices.Length > 0;
        _continue.Visible = textStage || reviewStage || choiceStage;
        _continue.Disabled = choiceStage && _selectedKey is null;
        _continue.Text = reviewStage ? "Begin  Enter" : "Continue  Enter";
        _detailPanel.Visible = choiceStage;
        _hint.Text = textStage
            ? "Type normally. Backspace erases. Escape returns."
            : reviewStage
                ? "Review every choice before beginning."
                : "Select a choice, review its effect, then continue.";

        if (textStage)
            SynchronizeEntry(creation.Entry, stageChanged || becameVisible);
        if (reviewStage)
            _review.Text = string.Join("\n\n", creation.ReviewLines);

        if (stageChanged || becameVisible)
            RebuildChoices(creation.Choices);

        if (becameVisible || stageChanged)
        {
            if (textStage)
                _entry.CallDeferred(Control.MethodName.GrabFocus);
            else
                FirstEnabledChoice()?.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    public bool HandleKey(InputEventKey key)
    {
        if (_stage is CreationStage.Face or CreationStage.Name or CreationStage.Review)
            return false;
        if (key.Keycode is Key.Enter or Key.KpEnter)
        {
            SubmitSelection();
            return true;
        }
        ChoiceGridDirection? direction = key.Keycode switch
        {
            Key.Up => ChoiceGridDirection.Up,
            Key.Down => ChoiceGridDirection.Down,
            Key.Left => ChoiceGridDirection.Left,
            Key.Right => ChoiceGridDirection.Right,
            _ => null,
        };
        if (direction is { } gridDirection)
            return MoveChoice(gridDirection);
        if (key.Unicode <= 0 || key.Unicode > char.MaxValue)
            return false;
        char value = (char)key.Unicode;
        if (!_choiceCards.TryGetValue(value, out CreationChoiceCard? card)
            || card.Selection.Disabled)
            return false;
        SelectChoice(card.Selection);
        card.Selection.GrabFocus();
        return true;
    }

    private void SynchronizeEntry(string authoritative, bool force)
    {
        bool focused = _entry.HasFocus();
        bool localAhead = focused
            && _submittedText.StartsWith(authoritative, StringComparison.Ordinal)
            && _entry.Text == _submittedText;
        if (!force && localAhead)
            return;

        _suppressEntry = true;
        _entry.Text = authoritative;
        _entry.CaretColumn = authoritative.Length;
        _submittedText = authoritative;
        _suppressEntry = false;
    }

    private void OnEntryChanged(string value)
    {
        if (_suppressEntry)
            return;

        string normalized = NormalizeName(value);
        if (normalized != value)
        {
            _suppressEntry = true;
            _entry.Text = normalized;
            _entry.CaretColumn = normalized.Length;
            _suppressEntry = false;
        }

        int common = 0;
        while (common < _submittedText.Length
            && common < normalized.Length
            && _submittedText[common] == normalized[common])
            common++;
        for (int index = _submittedText.Length; index > common; index--)
            KeyRequested?.Invoke('-');
        for (int index = common; index < normalized.Length; index++)
            KeyRequested?.Invoke(normalized[index]);
        _submittedText = normalized;
    }

    private static string NormalizeName(string value)
    {
        string filtered = new(
            value.Where(character => char.IsAsciiLetter(character) || character == ' ')
                .Take(14)
                .ToArray());
        if (filtered.Length == 0)
            return filtered;
        return char.ToUpperInvariant(filtered[0]) + filtered[1..];
    }

    private void RebuildChoices(IReadOnlyList<CreationChoice> choices)
    {
        foreach (Node child in _choices.GetChildren())
        {
            _choices.RemoveChild(child);
            child.QueueFree();
        }
        _choiceCards.Clear();
        _choices.AddThemeConstantOverride("separation", _scale.Space1);

        var buttons = new List<Button>();
        foreach (CreationChoice choice in choices)
        {
            var card = new CreationChoiceCard(choice, _fonts, _scale, _palette);
            Button button = card.Selection;
            char key = choice.Key;
            button.SetMeta("canonical_key", key.ToString());
            button.SetMeta(
                "explanation",
                choice.Explanation.Length > 0
                    ? choice.Explanation
                    : choice.Detail.Length > 0
                        ? choice.Detail
                        : choice.Description);
            button.Pressed += () => SelectChoice(button);
            button.FocusEntered += () => SelectChoice(button);
            _choices.AddChild(card);
            buttons.Add(button);
            _choiceCards[key] = card;
        }

        Button? selected = _selectedKey is { } selectedKey
            && _choiceCards.TryGetValue(selectedKey, out CreationChoiceCard? selectedCard)
                ? selectedCard.Selection
                : null;
        if (selected is not null)
            SelectChoice(selected);
        else
            _selectedDetail.Text = "Select a choice to see what it changes.";
        RefreshChoiceCardLayout();
        Callable.From(RefreshChoiceWidths).CallDeferred();
    }

    private Button? FirstEnabledChoice() =>
        _choiceCards.Values
            .Select(card => card.Selection)
            .FirstOrDefault(button => !button.Disabled);

    private void SelectChoice(Button button)
    {
        string key = button.GetMeta("canonical_key", "").AsString();
        if (key.Length != 1 || button.Disabled)
            return;
        _selectedKey = key[0];
        foreach (CreationChoiceCard choice in _choiceCards.Values)
            choice.Selection.SetPressedNoSignal(ReferenceEquals(choice.Selection, button));
        _selectedDetail.Text = button.GetMeta(
            "explanation",
            "Select a choice to see what it changes.").AsString();
        _continue.Disabled = false;
    }

    private bool MoveChoice(ChoiceGridDirection direction)
    {
        Button[] buttons = _choiceCards.Values
            .Select(card => card.Selection)
            .ToArray();
        if (buttons.Length == 0)
            return false;

        Control? focusOwner = GetViewport().GuiGetFocusOwner();
        int index = Array.IndexOf(buttons, focusOwner);
        if (index < 0 && _selectedKey is { } selected)
            index = Array.FindIndex(
                buttons,
                button => button.GetMeta("canonical_key", "").AsString() == selected.ToString());
        if (index < 0)
            index = Array.FindIndex(buttons, button => !button.Disabled);
        if (index < 0)
            return false;

        int next = index;
        while (true)
        {
            int candidate = ChoiceGridNavigation.Neighbor(
                next,
                buttons.Length,
                _choices.Columns,
                direction);
            if (candidate == next)
                return true;
            next = candidate;
            if (!buttons[next].Disabled)
                break;
        }

        SelectChoice(buttons[next]);
        buttons[next].GrabFocus();
        return true;
    }

    private void SubmitSelection()
    {
        if (_stage == CreationStage.Review || _stage is CreationStage.Face or CreationStage.Name)
        {
            KeyRequested?.Invoke('.');
            return;
        }
        if (_selectedKey is { } selected)
            KeyRequested?.Invoke(selected);
    }

    private void RefreshChoiceWidths()
    {
        float available = Math.Max(320, _choiceScroll.Size.X - _scale.Space2);
        float width = Math.Max(
            320,
            (available - (_choices.Columns - 1) * _scale.Space2) / _choices.Columns);
        _choices.CustomMinimumSize = new Vector2(available, 0);
        bool stackCard = RefreshChoiceCardLayout();
        foreach (CreationChoiceCard card in _choiceCards.Values)
        {
            card.SetStacked(stackCard);
            card.CustomMinimumSize = new Vector2(
                width,
                Math.Max(112, _scale.Space4 * 3));
        }
    }

    private bool RefreshChoiceCardLayout()
    {
        bool stacked =
            _viewportWidth / Math.Max(1, _choices.Columns) < 760 * _scale.Scale;
        foreach (CreationChoiceCard card in _choiceCards.Values)
            card.SetStacked(stacked);
        return stacked;
    }

    private void RefreshRouteVisuals()
    {
        for (int index = 0; index < _routeSteps.Count; index++)
        {
            int step = index + 1;
            bool current = step == _currentStep;
            bool complete = step < _currentStep;
            Color border = current || complete ? _palette.Accent : _palette.Muted;
            Color background = current ? _palette.Accent : _palette.Panel;
            _routeSteps[index].CustomMinimumSize = new Vector2(
                0,
                Math.Max(34, _scale.Space3 + _scale.Space1));
            _routeSteps[index].AddThemeStyleboxOverride(
                "panel",
                UiThemeFactory.BorderBox(background, border, _scale, _scale.Space1));
            _routeLabels[index].AddThemeFontOverride("font", _fonts.MonoSemibold);
            _routeLabels[index].AddThemeFontSizeOverride("font_size", _scale.Metadata);
            _routeLabels[index].AddThemeColorOverride(
                "font_color",
                current ? _palette.Background : border);
        }
    }
}

internal sealed partial class WorldScreen : Control
{
    private readonly ClientFonts _fonts;
    private readonly MapGridControl _map;
    private readonly ActivityFeedState _activityState;
    private readonly ActivityLogView _activity;
    private readonly Control _mapRegion;
    private readonly PanelContainer _sidebar;
    private readonly MarginContainer _sidebarMargin;
    private readonly Label _playerName;
    private readonly Label _condition;
    private readonly ResourceMeter _health;
    private readonly ResourceMeter _stamina;
    private readonly ResourceMeter _focus;
    private readonly Label _coin;
    private readonly Label _essence;
    private readonly Label _rations;
    private readonly Label _context;
    private readonly Label _zoomLabel;
    private readonly Button _sidebarButton;
    private readonly Button _sidebarClose;
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private bool _lightTheme;
    private int _mapZoomIndex;
    private bool _sidebarOpen;

    public event Action<int>? MapZoomChanged;

    public WorldScreen(
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette,
        bool lightTheme,
        ActivityFeedState activityState)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        _lightTheme = lightTheme;
        _activityState = activityState;
        ClipContents = true;

        _mapRegion = new Control();
        _mapRegion.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_mapRegion);
        var mapStack = new VBoxContainer();
        mapStack.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _mapRegion.AddChild(mapStack);

        var mapPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        mapStack.AddChild(mapPanel);
        var mapMargin = new MarginContainer();
        mapPanel.AddChild(mapMargin);
        _map = new MapGridControl(fonts.Mono, palette, lightTheme)
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        mapMargin.AddChild(_map);

        var footer = new PanelContainer();
        mapStack.AddChild(footer);
        var footerRow = new HBoxContainer();
        footer.AddChild(Wrap(footerRow));
        _context = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        footerRow.AddChild(_context);
        var zoomOut = new Button { Text = "−", TooltipText = "Zoom map out, Ctrl+minus" };
        zoomOut.Pressed += () => ChangeZoom(-1);
        footerRow.AddChild(zoomOut);
        _zoomLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(72, 0),
        };
        footerRow.AddChild(_zoomLabel);
        var zoomIn = new Button { Text = "+", TooltipText = "Zoom map in, Ctrl+plus" };
        zoomIn.Pressed += () => ChangeZoom(1);
        footerRow.AddChild(zoomIn);
        var zoomReset = new Button { Text = "Reset", TooltipText = "Reset map zoom, Ctrl+0" };
        zoomReset.Pressed += () => SetZoom(0);
        footerRow.AddChild(zoomReset);
        _sidebarButton = new Button { Text = "Activity" };
        _sidebarButton.Pressed += ToggleSidebar;
        footerRow.AddChild(_sidebarButton);

        _sidebar = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        AddChild(_sidebar);
        var sidebarScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            FollowFocus = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _sidebar.AddChild(sidebarScroll);
        var sidebarStack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _sidebarMargin = Wrap(sidebarStack);
        _sidebarMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _sidebarMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        sidebarScroll.AddChild(_sidebarMargin);
        var sidebarHeader = new HBoxContainer();
        sidebarStack.AddChild(sidebarHeader);
        var title = new Label
        {
            Text = "CURRENT CONDITION",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        sidebarHeader.AddChild(title);
        _sidebarClose = new Button { Text = "Close" };
        _sidebarClose.Pressed += ToggleSidebar;
        sidebarHeader.AddChild(_sidebarClose);

        _playerName = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        sidebarStack.AddChild(_playerName);
        _condition = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        sidebarStack.AddChild(_condition);
        _health = new ResourceMeter("♥");
        sidebarStack.AddChild(_health);
        _stamina = new ResourceMeter("◆");
        sidebarStack.AddChild(_stamina);
        _focus = new ResourceMeter("◉");
        sidebarStack.AddChild(_focus);
        sidebarStack.AddChild(new HSeparator());

        var activityTitle = new Label { Text = "ACTIVITY" };
        sidebarStack.AddChild(activityTitle);
        _activity = new ActivityLogView(fonts, scale, palette, activityState);
        sidebarStack.AddChild(_activity);
        sidebarStack.AddChild(new HSeparator());

        var currencies = new GridContainer { Columns = 2 };
        sidebarStack.AddChild(currencies);
        _coin = CurrencyLabel();
        currencies.AddChild(_coin);
        _essence = CurrencyLabel();
        currencies.AddChild(_essence);
        _rations = CurrencyLabel();
        currencies.AddChild(_rations);

        ApplyVisuals(scale, palette, lightTheme);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette, bool lightTheme)
    {
        _scale = scale;
        _palette = palette;
        _lightTheme = lightTheme;
        _sidebar.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, palette.Muted, scale, 0));
        UiThemeFactory.Mark(_playerName, "heading", _fonts, scale, palette);
        UiThemeFactory.Mark(_condition, "muted", _fonts, scale, palette);
        UiThemeFactory.Mark(_context, "muted", _fonts, scale, palette);
        UiThemeFactory.Mark(_zoomLabel, "eyebrow", _fonts, scale, palette);
        _health.ApplyVisuals(_fonts, scale, palette, palette.Health);
        _stamina.ApplyVisuals(_fonts, scale, palette, palette.Stamina);
        _focus.ApplyVisuals(_fonts, scale, palette, palette.Accent);
        _activity.ApplyVisuals(scale, palette);
        _activity.CustomMinimumSize = new Vector2(
            0,
            240 * Math.Min(scale.Scale, 1.5f));
        _sidebarMargin.AddThemeConstantOverride("margin_left", scale.Space3);
        _sidebarMargin.AddThemeConstantOverride("margin_right", scale.Space3);
        _sidebarMargin.AddThemeConstantOverride("margin_top", scale.Space2);
        _sidebarMargin.AddThemeConstantOverride("margin_bottom", scale.Space2);
        foreach (Label label in new[] { _coin, _essence, _rations })
        {
            label.AddThemeFontOverride("font", _fonts.MonoSemibold);
            label.AddThemeFontSizeOverride("font_size", scale.Metadata);
            label.AddThemeColorOverride("font_color", palette.Warm);
        }
        if (_map is not null && _map.IsInsideTree())
            _map.QueueRedraw();
    }

    public void ApplyLayout(float viewportWidth)
    {
        ResponsiveClientLayout layout = ResponsiveClientLayout.Resolve(
            (int)MathF.Round(viewportWidth),
            _scale.Scale);
        bool docked = layout.WorldRail == WorldRailPresentation.Docked;
        float width = Math.Min(
            Math.Max(0, Size.X - _scale.Space4 * 2),
            Math.Max(330, 360 * _scale.Scale));
        _sidebarButton.Visible = !docked;
        _sidebarClose.Visible = !docked;
        _sidebar.Visible = docked || _sidebarOpen;

        _sidebar.SetAnchor(Side.Left, 1);
        _sidebar.SetAnchor(Side.Top, 0);
        _sidebar.SetAnchor(Side.Right, 1);
        _sidebar.SetAnchor(Side.Bottom, 1);
        _sidebar.OffsetLeft = -width;
        _sidebar.OffsetTop = 0;
        _sidebar.OffsetRight = 0;
        _sidebar.OffsetBottom = 0;
        _sidebarMargin.CustomMinimumSize = new Vector2(
            Math.Max(0, width - _scale.Space1),
            Math.Max(0, Size.Y - _scale.Space1));

        _mapRegion.OffsetRight = docked ? -width - _scale.Space2 : 0;
    }

    public void UpdateView(
        Frame frame,
        ClientInteractionContext context,
        WorldHudPresentation hud,
        int mapZoomIndex)
    {
        _mapZoomIndex = MapZoom.ClampIndex(mapZoomIndex);
        _map.UpdateFrame(frame, _fonts.Mono, _palette, _lightTheme, _mapZoomIndex);
        _playerName.Text = hud.PlayerName;
        _condition.Text =
            $"Cycle {hud.Cycle}  |  Turn {hud.Turn}\n{hud.Season}  |  {hud.Weather}";
        _health.UpdateValue(hud.Health, hud.MaxHealth);
        _stamina.UpdateValue(hud.Stamina, hud.MaxStamina);
        _focus.UpdateValue(hud.Focus, hud.MaxFocus);
        _coin.Text = $"●  {hud.Coin:N0} coin";
        _essence.Text = $"✦  {hud.Essence:N0} essence";
        _rations.Text = hud.Rations > 0 ? $"□  {hud.Rations:N0} rations" : "";
        _context.Text =
            $"{hud.WorldName}  |  {hud.SettlementName}  |  {hud.Season}  |  {hud.Weather}";
        _zoomLabel.Text = $"{MapZoom.Percent(_mapZoomIndex)}%";
    }

    public void SetZoom(int index)
    {
        int clamped = MapZoom.ClampIndex(index);
        if (clamped == _mapZoomIndex)
            return;
        _mapZoomIndex = clamped;
        _zoomLabel.Text = $"{MapZoom.Percent(_mapZoomIndex)}%";
        _map.SetZoom(_mapZoomIndex);
        MapZoomChanged?.Invoke(_mapZoomIndex);
    }

    private void ChangeZoom(int delta) => SetZoom(_mapZoomIndex + delta);

    public void ToggleSidebar()
    {
        _sidebarOpen = !_sidebarOpen;
        ApplyLayout(Size.X);
    }

    private static Label CurrencyLabel() => new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    };

    internal static MarginContainer Wrap(Control child)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        margin.AddChild(child);
        return margin;
    }

    internal static string LogMarkup(IEnumerable<LogEntry> entries, UiPalette palette)
    {
        var builder = new StringBuilder();
        foreach (LogEntry entry in entries)
        {
            Color color = entry.Tone switch
            {
                LogTone.Danger or LogTone.Combat => palette.Combat,
                LogTone.Aegis => palette.Words,
                _ => palette.Field,
            };
            if (builder.Length > 0)
                builder.Append("\n\n");
            builder.Append("[color=#");
            builder.Append(color.ToHtml(includeAlpha: false));
            builder.Append(']');
            builder.Append(EscapeBbCode(entry.Text));
            builder.Append("[/color]");
        }
        return builder.ToString();
    }

    internal static string EscapeBbCode(string value) =>
        value.Replace("[", "[lb]", StringComparison.Ordinal)
            .Replace("]", "[rb]", StringComparison.Ordinal);
}

internal sealed partial class ConversationScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly Label _title;
    private readonly Label _resources;
    private readonly VBoxContainer _actions;
    private readonly RichTextLabel _transcript;
    private readonly Label _selectedAction;
    private readonly Button _leave;
    private readonly GridContainer _layout;
    private readonly PanelContainer _actionPanel;
    private readonly PanelContainer _transcriptPanel;
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private char? _selectedKey;

    public event Action<char>? KeyRequested;

    public ConversationScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        var pageScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(pageScroll);
        var stack = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        pageScroll.AddChild(stack);
        _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        stack.AddChild(_title);
        _resources = new Label();
        stack.AddChild(_resources);

        _layout = new GridContainer
        {
            Columns = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        stack.AddChild(_layout);
        _actionPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(390, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _layout.AddChild(_actionPanel);
        var actionScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _actionPanel.AddChild(WorldScreen.Wrap(actionScroll));
        _actions = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        actionScroll.AddChild(_actions);

        _transcriptPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _layout.AddChild(_transcriptPanel);
        _transcript = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _transcriptPanel.AddChild(WorldScreen.Wrap(_transcript));

        var selectedPanel = new PanelContainer();
        stack.AddChild(selectedPanel);
        _selectedAction = new Label
        {
            Text = "Choose a topic or action.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        selectedPanel.AddChild(WorldScreen.Wrap(_selectedAction));

        _leave = new Button { Text = "Leave  Esc" };
        _leave.Pressed += () => KeyRequested?.Invoke('q');
        stack.AddChild(_leave);
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
        UiThemeFactory.Mark(_resources, "eyebrow", _fonts, scale, palette);
        _selectedAction.AddThemeColorOverride("font_color", palette.Muted);
    }

    public void ApplyLayout(float viewportWidth)
    {
        ResponsiveClientLayout layout = ResponsiveClientLayout.Resolve(
            (int)MathF.Round(viewportWidth),
            _scale.Scale);
        bool split = layout.Conversation == ConversationPresentation.Split;
        _layout.Columns = split ? 2 : 1;
        _actionPanel.CustomMinimumSize = split
            ? new Vector2(Math.Max(390, 390 * _scale.Scale), 0)
            : new Vector2(0, Math.Max(190, 190 * _scale.Scale));
        _transcriptPanel.CustomMinimumSize = split
            ? Vector2.Zero
            : new Vector2(0, Math.Max(240, 240 * _scale.Scale));
    }

    public void UpdateView(
        ClientInteractionContext context,
        bool becameVisible,
        WorldHudPresentation hud)
    {
        _title.Text = context.Title;
        _resources.Text = $"COIN  {hud.Coin:N0}     ESSENCE  {hud.Essence:N0}";
        RebuildActions(context.Actions, becameVisible);
        _transcript.Text = WorldScreen.LogMarkup(context.Transcript, _palette);
        QueueScrollToBottom();
    }

    public bool MoveSelection(int delta)
    {
        Button[] buttons = _actions.GetChildren()
            .OfType<Button>()
            .Where(button => !button.Disabled)
            .ToArray();
        if (buttons.Length == 0)
            return false;

        Control? owner = GetViewport().GuiGetFocusOwner();
        int index = Array.IndexOf(buttons, owner);
        if (index < 0 && _selectedKey is { } selected)
            index = Array.FindIndex(buttons, button => ButtonKey(button) == selected);
        index = (index + delta + buttons.Length) % buttons.Length;
        _selectedKey = ButtonKey(buttons[index]);
        buttons[index].GrabFocus();
        return true;
    }

    private void RebuildActions(IReadOnlyList<ClientAction> actions, bool focusFirst)
    {
        if (GetViewport().GuiGetFocusOwner() is Button focused && focused.GetParent() == _actions)
            _selectedKey = ButtonKey(focused);
        foreach (Node child in _actions.GetChildren())
        {
            _actions.RemoveChild(child);
            child.QueueFree();
        }
        _actions.AddThemeConstantOverride("separation", _scale.Space1);
        var buttons = new List<Button>();
        foreach (ClientAction action in actions)
        {
            var button = new Button
            {
                Text = $"{action.Key}  {action.Label}",
                Disabled = !action.Enabled,
                Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, Math.Max(56, _scale.Space4 * 2)),
            };
            char key = action.Key;
            button.Pressed += () => KeyRequested?.Invoke(key);
            button.SetMeta("canonical_key", key.ToString());
            button.SetMeta("action_label", action.Label);
            button.FocusEntered += () => Select(button);
            _actions.AddChild(button);
            buttons.Add(button);
        }
        if (buttons.Count > 0)
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                buttons[index].FocusNeighborTop = buttons[(index - 1 + buttons.Count) % buttons.Count].GetPath();
                buttons[index].FocusNeighborBottom = buttons[(index + 1) % buttons.Count].GetPath();
                buttons[index].FocusNeighborLeft = buttons[index].GetPath();
                buttons[index].FocusNeighborRight = buttons[index].GetPath();
            }
            Button? restore = _selectedKey is { } selected
                ? buttons.FirstOrDefault(button => ButtonKey(button) == selected && !button.Disabled)
                : null;
            if (focusFirst || restore is not null)
                (restore ?? buttons.FirstOrDefault(button => !button.Disabled))
                    ?.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private static char? ButtonKey(Button button)
    {
        string value = button.GetMeta("canonical_key", "").AsString();
        return value.Length == 1 ? value[0] : null;
    }

    private void Select(Button button)
    {
        _selectedKey = ButtonKey(button);
        _selectedAction.Text = button.GetMeta("action_label", "Choose a topic or action.").AsString();
    }

    private void ScrollToBottom()
    {
        VScrollBar bar = _transcript.GetVScrollBar();
        bar.Value = bar.MaxValue;
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
}

internal sealed partial class LegacyScreen : MarginContainer
{
    private readonly RichTextLabel _frameText;
    private readonly Font _font;
    private UiPalette _palette;
    private bool _lightTheme;

    public LegacyScreen(Font font, UiScaleTokens scale, UiPalette palette, bool lightTheme)
    {
        _font = font;
        _palette = palette;
        _lightTheme = lightTheme;
        _frameText = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = true,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        AddChild(_frameText);
        ApplyVisuals(scale, palette, lightTheme);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette, bool lightTheme)
    {
        _palette = palette;
        _lightTheme = lightTheme;
        AddThemeConstantOverride("margin_left", scale.Space3);
        AddThemeConstantOverride("margin_right", scale.Space3);
        AddThemeConstantOverride("margin_top", scale.Space2);
        AddThemeConstantOverride("margin_bottom", scale.Space2);
        _frameText.AddThemeFontOverride("normal_font", _font);
        _frameText.AddThemeFontSizeOverride("normal_font_size", scale.Metadata);
    }

    public void UpdateView(Frame frame)
    {
        var builder = new StringBuilder();
        for (int y = 0; y < frame.Height; y++)
        {
            Hue? current = null;
            for (int x = 0; x < frame.Width; x++)
            {
                Cell cell = frame[x, y];
                if (current != cell.Fg)
                {
                    if (current is not null)
                        builder.Append("[/color]");
                    current = cell.Fg;
                    builder.Append("[color=#");
                    builder.Append(_palette.MapColor(cell.Fg, _lightTheme).ToHtml(includeAlpha: false));
                    builder.Append(']');
                }
                builder.Append(WorldScreen.EscapeBbCode(cell.Ch.ToString()));
            }
            if (current is not null)
                builder.Append("[/color]");
            if (y + 1 < frame.Height)
                builder.Append('\n');
        }
        _frameText.Text = builder.ToString();
    }
}
