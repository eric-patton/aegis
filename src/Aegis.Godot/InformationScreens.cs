using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed record LedgerEntry(
    string Title,
    string Kicker,
    string Summary,
    string Detail,
    int Progress = 0,
    int ProgressMaximum = 0,
    char? ActionKey = null);

internal sealed partial class CharacterLedgerRow : Button
{
    private readonly MarginContainer _margin;
    private readonly HBoxContainer _row;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly ProgressBar _progress;

    public CharacterLedgerRow(LedgerEntry entry)
    {
        Text = "";
        ToggleMode = true;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        _margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_margin);
        _row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _margin.AddChild(_row);
        _title = new Label
        {
            Text = entry.Title,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _row.AddChild(_title);
        _summary = new Label
        {
            Text = entry.Summary,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _row.AddChild(_summary);
        _progress = new ProgressBar
        {
            Visible = entry.ProgressMaximum > 0,
            MouseFilter = MouseFilterEnum.Ignore,
            ShowPercentage = false,
            MinValue = 0,
            MaxValue = Math.Max(1, entry.ProgressMaximum),
            Value = entry.Progress,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _row.AddChild(_progress);
    }

    public void ApplyRowVisuals(
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette,
        bool selected)
    {
        CustomMinimumSize = new Vector2(0, Math.Max(42, 42 * scale.Scale));
        StyleBoxFlat selectedBox = UiThemeFactory.BorderBox(
            UiThemeFactory.Mix(palette.Raised, palette.Accent, 0.14f),
            palette.Accent,
            scale,
            scale.Space1);
        selectedBox.BorderWidthLeft = Math.Max(4, (int)MathF.Round(4 * scale.Scale));
        AddThemeStyleboxOverride(
            "normal",
            selected
                ? selectedBox
                : UiThemeFactory.BorderBox(palette.Raised, palette.Muted, scale, scale.Space1));
        AddThemeStyleboxOverride("pressed", selectedBox);
        AddThemeStyleboxOverride("hover_pressed", selectedBox);
        AddThemeStyleboxOverride("focus", UiThemeFactory.InsetFocusBox(palette.Accent, scale));
        SetPressedNoSignal(selected);
        _margin.AddThemeConstantOverride("margin_left", scale.Space2);
        _margin.AddThemeConstantOverride("margin_right", scale.Space2);
        _margin.AddThemeConstantOverride("margin_top", scale.Space1);
        _margin.AddThemeConstantOverride("margin_bottom", scale.Space1);
        _row.AddThemeConstantOverride("separation", scale.Space2);

        _title.AddThemeFontOverride("font", fonts.BodySemibold);
        _title.AddThemeFontSizeOverride("font_size", scale.Control);
        _title.AddThemeColorOverride("font_color", selected ? palette.Accent : palette.Text);
        _summary.AddThemeFontOverride("font", fonts.MonoSemibold);
        _summary.AddThemeFontSizeOverride("font_size", scale.Metadata);
        _summary.AddThemeColorOverride("font_color", palette.Muted);
        _progress.CustomMinimumSize = new Vector2(
            Math.Max(100, 120 * scale.Scale),
            Math.Max(6, scale.Space1));
    }
}

internal sealed partial class CharacterLedgerScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly PanelContainer _panel;
    private readonly VBoxContainer _stack;
    private readonly Label _name;
    private readonly Label _identity;
    private readonly ResourceMeter _health;
    private readonly ResourceMeter _stamina;
    private readonly ResourceMeter _focus;
    private readonly HBoxContainer _compactHeader;
    private readonly Label _compactName;
    private readonly OptionButton _compactSection;
    private readonly Button _compactClose;
    private readonly ScrollContainer _bodyScroll;
    private readonly GridContainer _body;
    private readonly PanelContainer _sectionPanel;
    private readonly VBoxContainer _sections;
    private readonly PanelContainer _listPanel;
    private readonly Label _listTitle;
    private readonly Label _listCount;
    private readonly LineEdit _filter;
    private readonly ScrollContainer _listScroll;
    private readonly VBoxContainer _entries;
    private readonly PanelContainer _inspectorPanel;
    private readonly ScrollContainer _inspectorScroll;
    private readonly VBoxContainer _inspector;
    private readonly Label _inspectorKicker;
    private readonly Label _inspectorTitle;
    private readonly Label _inspectorSummary;
    private readonly Label _inspectorDetail;
    private readonly ProgressBar _inspectorProgress;
    private readonly Label _inspectorProgressText;
    private readonly Button _inspectorAction;
    private readonly Button _inspectorBack;
    private readonly Button _close;
    private readonly List<Button> _sectionButtons = [];
    private readonly List<Button> _entryButtons = [];
    private CharacterPresentation? _presentation;
    private int _selectedSection;
    private int _selectedEntry;
    private string _filterText = "";
    private bool _compactLayout;
    private bool _compactInspectorOpen;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    private static readonly string[] SectionNames =
    [
        "Overview",
        "Attributes",
        "Skills",
        "Knacks",
        "Lessons",
        "Burden and scars",
        "Standing",
        "Pending choices",
    ];

    public event Action<char>? KeyRequested;

    public CharacterLedgerScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;

        _panel = new PanelContainer();
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_panel);
        _stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _panel.AddChild(WorldScreen.Wrap(_stack));

        _compactHeader = new HBoxContainer { Visible = false };
        _stack.AddChild(_compactHeader);
        _compactName = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _compactHeader.AddChild(_compactName);
        _compactSection = new OptionButton
        {
            CustomMinimumSize = new Vector2(260, 0),
            TooltipText = "Choose a Character section",
        };
        foreach (string section in SectionNames)
            _compactSection.AddItem(section);
        _compactSection.ItemSelected += index => SelectSection((int)index);
        _compactHeader.AddChild(_compactSection);
        _compactClose = new Button { Text = "Return  Esc" };
        _compactClose.Pressed += () => KeyRequested?.Invoke('q');
        _compactHeader.AddChild(_compactClose);

        _bodyScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _stack.AddChild(_bodyScroll);
        _body = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _bodyScroll.AddChild(_body);

        _sectionPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(220, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddChild(_sectionPanel);
        _sections = new VBoxContainer();
        var sectionMargin = WorldScreen.Wrap(_sections);
        sectionMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        sectionMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        _sectionPanel.AddChild(sectionMargin);
        var identityStack = new VBoxContainer();
        _sections.AddChild(identityStack);
        var eyebrow = new Label { Text = "CHARACTER" };
        identityStack.AddChild(eyebrow);
        _name = new Label();
        identityStack.AddChild(_name);
        _identity = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        identityStack.AddChild(_identity);
        _health = new ResourceMeter("♥");
        identityStack.AddChild(_health);
        _stamina = new ResourceMeter("◆");
        identityStack.AddChild(_stamina);
        _focus = new ResourceMeter("◉");
        identityStack.AddChild(_focus);
        _sections.AddChild(new HSeparator());
        var sectionEyebrow = new Label { Text = "RECORD" };
        _sections.AddChild(sectionEyebrow);
        for (int index = 0; index < SectionNames.Length; index++)
        {
            int selected = index;
            var button = new Button
            {
                Text = SectionNames[index],
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += () => SelectSection(selected);
            _sections.AddChild(button);
            _sectionButtons.Add(button);
        }
        var sectionSpacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        _sections.AddChild(sectionSpacer);
        _close = new Button { Text = "Return to world  Esc" };
        _close.Pressed += () => KeyRequested?.Invoke('q');
        _sections.AddChild(_close);

        _listPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(430, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddChild(_listPanel);
        var listStack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        var listMargin = WorldScreen.Wrap(listStack);
        listMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        listMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        _listPanel.AddChild(listMargin);
        var listHeader = new HBoxContainer();
        listStack.AddChild(listHeader);
        _listTitle = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        listHeader.AddChild(_listTitle);
        _listCount = new Label();
        listHeader.AddChild(_listCount);
        _filter = new LineEdit
        {
            PlaceholderText = "Filter this section",
            ClearButtonEnabled = true,
        };
        _filter.TextChanged += value =>
        {
            _filterText = value.Trim();
            _selectedEntry = 0;
            RebuildEntries();
        };
        listStack.AddChild(_filter);
        _listScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        listStack.AddChild(_listScroll);
        _entries = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _listScroll.AddChild(_entries);

        _inspectorPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(360, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddChild(_inspectorPanel);
        _inspectorScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _inspectorPanel.AddChild(_inspectorScroll);
        _inspector = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inspectorMargin = WorldScreen.Wrap(_inspector);
        inspectorMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        inspectorMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        _inspectorScroll.AddChild(inspectorMargin);
        _inspectorBack = new Button
        {
            Text = "Back to list",
            Visible = false,
            Alignment = HorizontalAlignment.Left,
        };
        _inspectorBack.Pressed += ShowCompactList;
        _inspector.AddChild(_inspectorBack);
        _inspectorKicker = new Label();
        _inspector.AddChild(_inspectorKicker);
        _inspectorTitle = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _inspector.AddChild(_inspectorTitle);
        _inspectorSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _inspector.AddChild(_inspectorSummary);
        _inspectorProgress = new ProgressBar { ShowPercentage = false };
        _inspector.AddChild(_inspectorProgress);
        _inspectorProgressText = new Label();
        _inspector.AddChild(_inspectorProgressText);
        _inspectorDetail = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _inspector.AddChild(_inspectorDetail);
        _inspectorAction = new Button { Visible = false };
        _inspectorAction.Pressed += ActivateInspector;
        _inspector.AddChild(_inspectorAction);

        UiThemeFactory.Mark(eyebrow, "eyebrow", fonts, scale, palette);
        UiThemeFactory.Mark(sectionEyebrow, "eyebrow", fonts, scale, palette);
        UiThemeFactory.Mark(_listTitle, "eyebrow", fonts, scale, palette);
        UiThemeFactory.Mark(_listCount, "muted", fonts, scale, palette);
        ApplyVisuals(scale, palette);
    }

    public void UpdateView(ClientInteractionContext context, bool becameVisible)
    {
        _presentation = context.Character
            ?? throw new InvalidOperationException("Character Ledger requires a character projection.");
        _name.Text = _presentation.Name;
        _identity.Text = _presentation.Identity;
        _health.UpdateValue(_presentation.Health, _presentation.MaxHealth);
        _stamina.UpdateValue(_presentation.Stamina, _presentation.MaxStamina);
        _focus.UpdateValue(_presentation.Focus, _presentation.MaxFocus);
        _compactName.Text = _presentation.Name;
        _selectedSection = Math.Clamp(_selectedSection, 0, SectionNames.Length - 1);
        _compactSection.Select(_selectedSection);
        RebuildEntries();
        if (becameVisible)
        {
            if (_compactLayout)
                _compactSection.CallDeferred(Control.MethodName.GrabFocus);
            else
                _sectionButtons[_selectedSection].CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        AddThemeConstantOverride("margin_left", scale.Space3);
        AddThemeConstantOverride("margin_right", scale.Space3);
        AddThemeConstantOverride("margin_top", scale.Space3);
        AddThemeConstantOverride("margin_bottom", scale.Space3);
        _stack.AddThemeConstantOverride("separation", scale.Space2);
        _compactHeader.AddThemeConstantOverride("separation", scale.Space2);
        _body.AddThemeConstantOverride("h_separation", scale.Space2);
        _body.AddThemeConstantOverride("v_separation", scale.Space2);
        _sections.AddThemeConstantOverride("separation", scale.Space1);
        _entries.AddThemeConstantOverride("separation", scale.Space1);
        _inspector.AddThemeConstantOverride("separation", scale.Space2);
        _panel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Muted, scale, 0));
        _sectionPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, palette.Muted, scale, 0));
        _listPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, palette.Muted, scale, 0));
        _inspectorPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, 0));
        UiThemeFactory.Mark(_name, "heading", _fonts, scale, palette);
        UiThemeFactory.Mark(_identity, "muted", _fonts, scale, palette);
        UiThemeFactory.Mark(_compactName, "heading", _fonts, scale, palette);
        UiThemeFactory.Mark(_listTitle, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_listCount, "muted", _fonts, scale, palette);
        _filter.AddThemeFontOverride("font", _fonts.Body);
        _filter.AddThemeFontSizeOverride("font_size", scale.Control);
        UiThemeFactory.Mark(_inspectorKicker, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_inspectorTitle, "heading", _fonts, scale, palette);
        UiThemeFactory.Mark(_inspectorSummary, "muted", _fonts, scale, palette);
        _inspectorDetail.AddThemeColorOverride("font_color", palette.Text);
        _inspectorProgressText.AddThemeColorOverride("font_color", palette.Muted);
        _health.ApplyVisuals(_fonts, scale, palette, palette.Health);
        _stamina.ApplyVisuals(_fonts, scale, palette, palette.Stamina);
        _focus.ApplyVisuals(_fonts, scale, palette, palette.Accent);
        RefreshButtonVisuals();
    }

    public void ApplyLayout(float viewportWidth)
    {
        _compactLayout = viewportWidth < 1180 || _scale.Scale >= 1.5f;
        _bodyScroll.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _compactHeader.Visible = _compactLayout;
        _sectionPanel.Visible = !_compactLayout;
        _sectionPanel.CustomMinimumSize = new Vector2(_compactLayout ? 0 : 250, 0);
        _listPanel.CustomMinimumSize = new Vector2(_compactLayout ? 0 : 430, 0);
        _inspectorPanel.CustomMinimumSize = new Vector2(_compactLayout ? 0 : 360, 0);
        _body.CustomMinimumSize = new Vector2(
            Math.Max(480, viewportWidth - _scale.Space4 * 4),
            Math.Max(420, Size.Y - _scale.Space4 * 2));
        ApplyResponsivePanels();
    }

    public bool MoveSelection(int delta)
    {
        Control? focusOwner = GetViewport().GuiGetFocusOwner();
        if (_compactLayout && ReferenceEquals(focusOwner, _compactSection))
            return false;

        int sectionIndex = focusOwner is Button focusedSection
            ? _sectionButtons.IndexOf(focusedSection)
            : -1;
        if (sectionIndex >= 0)
        {
            int nextSection =
                (sectionIndex + delta + _sectionButtons.Count) % _sectionButtons.Count;
            SelectSection(nextSection);
            _sectionButtons[nextSection].GrabFocus();
            return true;
        }

        if (_entryButtons.Count == 0)
            return false;
        _selectedEntry = (_selectedEntry + delta + _entryButtons.Count) % _entryButtons.Count;
        SelectEntry(_selectedEntry, false);
        _entryButtons[_selectedEntry].GrabFocus();
        _listScroll.EnsureControlVisible(_entryButtons[_selectedEntry]);
        return true;
    }

    private void SelectSection(int index)
    {
        _selectedSection = index;
        _selectedEntry = 0;
        _compactInspectorOpen = index == 0;
        _compactSection.Select(index);
        if (_filter.Text.Length > 0)
            _filter.Text = "";
        RebuildEntries();
        _listScroll.ScrollVertical = 0;
        _inspectorScroll.ScrollVertical = 0;
        ApplyResponsivePanels();
        if (_compactLayout)
        {
            Control compactFocus = _selectedSection == 0 ? _inspectorScroll : _filter;
            compactFocus.GrabFocus();
        }
        else if (_entryButtons.Count > 0)
            _entryButtons[0].GrabFocus();
    }

    private void RebuildEntries()
    {
        foreach (Node child in _entries.GetChildren())
        {
            _entries.RemoveChild(child);
            child.QueueFree();
        }
        _entryButtons.Clear();
        LedgerEntry[] entries = FilteredEntries();
        _listTitle.Text = _selectedSection == 0
            ? "CHARACTER OVERVIEW"
            : SectionNames[_selectedSection].ToUpperInvariant();
        _listCount.Text = $"{entries.Length} {(entries.Length == 1 ? "entry" : "entries")}";
        foreach ((LedgerEntry entry, int index) in entries.Select((value, index) => (value, index)))
        {
            int selected = index;
            var button = new CharacterLedgerRow(entry);
            button.Pressed += () => SelectEntry(selected);
            _entries.AddChild(button);
            _entryButtons.Add(button);
        }
        if (_entryButtons.Count == 0)
        {
            var empty = new Label
            {
                Text = "Nothing recorded here yet.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            _entries.AddChild(empty);
            UiThemeFactory.Mark(empty, "muted", _fonts, _scale, _palette);
            ClearInspector();
        }
        else
        {
            _selectedEntry = Math.Clamp(_selectedEntry, 0, _entryButtons.Count - 1);
            SelectEntry(_selectedEntry, false);
        }
        RefreshSectionLabels();
        RefreshButtonVisuals();
        ApplyResponsivePanels();
    }

    private IReadOnlyList<LedgerEntry> EntriesForSection()
    {
        if (_presentation is null)
            return [];
        return _selectedSection switch
        {
            0 =>
            [
                new LedgerEntry(
                    _presentation.Name,
                    "CHARACTER OVERVIEW",
                    _presentation.Identity.Length > 0
                        ? _presentation.Identity
                        : "No history recorded",
                    OverviewDetail(_presentation)),
            ],
            1 => _presentation.Attributes
                .Select(item => new LedgerEntry(
                    item.Name,
                    "ATTRIBUTE",
                    $"Current value {item.Value}",
                    item.Description))
                .ToArray(),
            2 => _presentation.Skills
                .Select(item => new LedgerEntry(
                    item.Name,
                    $"SKILL  LEVEL {item.Level}",
                    $"{item.Uses} of {item.NextLevelUses} uses",
                    string.Join(
                        "\n\n",
                        item.Description,
                        "HOW IT GROWS",
                        "Use this skill in meaningful attempts. Its use count advances toward the next level.",
                        "NEXT LEVEL",
                        $"Level {item.Level + 1} begins at {item.NextLevelUses} total uses."),
                    item.Uses,
                    item.NextLevelUses))
                .ToArray(),
            3 => _presentation.Knacks
                .Select(item => new LedgerEntry(item.Name, "KNACK", item.Detail, "Kept for good."))
                .ToArray(),
            4 => _presentation.Lessons
                .Select(item => new LedgerEntry(item.Name, "LESSON", item.Detail, "Learned knowledge travels with the character."))
                .ToArray(),
            5 =>
            [
                new LedgerEntry("Burden", "CHARACTER", _presentation.Burden, "A lasting part of this character."),
                new LedgerEntry("Scars", "CHARACTER", _presentation.Scars, "Marks carried by the character."),
            ],
            6 =>
            [
                new LedgerEntry("Standing", "LEGEND", _presentation.Standing, "Standing grows from accumulated legend."),
            ],
            _ => _presentation.PendingKnacks
                .Select(item => new LedgerEntry(
                    item.Name,
                    "CHOICE READY",
                    item.Detail,
                    "This choice is permanent.",
                    ActionKey: item.Key))
                .ToArray(),
        };
    }

    private void SelectEntry(int index, bool revealInspector = true)
    {
        LedgerEntry[] entries = FilteredEntries();
        if (index < 0 || index >= entries.Length)
            return;
        _selectedEntry = index;
        LedgerEntry entry = entries[index];
        _inspectorKicker.Text = entry.Kicker;
        _inspectorTitle.Text = entry.Title;
        _inspectorSummary.Text = entry.Summary;
        _inspectorDetail.Text = entry.Detail;
        _inspectorProgress.Visible = entry.ProgressMaximum > 0;
        _inspectorProgressText.Visible = entry.ProgressMaximum > 0;
        _inspectorProgress.MaxValue = Math.Max(1, entry.ProgressMaximum);
        _inspectorProgress.Value = entry.Progress;
        _inspectorProgressText.Text = entry.ProgressMaximum > 0
            ? $"{entry.Progress} / {entry.ProgressMaximum} uses"
            : "";
        _inspectorAction.Visible = entry.ActionKey is not null;
        _inspectorAction.Text = entry.ActionKey is { } key ? $"Choose  {key}" : "";
        _inspectorAction.SetMeta("canonical_key", entry.ActionKey?.ToString() ?? "");
        _inspectorScroll.ScrollVertical = 0;
        if (_compactLayout && revealInspector)
        {
            _compactInspectorOpen = true;
            ApplyResponsivePanels();
            _inspectorBack.GrabFocus();
        }
        RefreshButtonVisuals();
    }

    private static string OverviewDetail(CharacterPresentation presentation)
    {
        string attributes = string.Join(
            "  •  ",
            presentation.Attributes.Select(attribute => $"{attribute.Name} {attribute.Value}"));
        string practices =
            $"{presentation.Skills.Length} skills  •  "
            + $"{presentation.Knacks.Length} knacks  •  "
            + $"{presentation.Lessons.Length} lessons";
        return string.Join(
            "\n\n",
            attributes,
            practices,
            $"Burden: {presentation.Burden}",
            $"Scars: {presentation.Scars}",
            $"Standing: {presentation.Standing}");
    }

    private void ShowCompactList()
    {
        _compactInspectorOpen = false;
        ApplyResponsivePanels();
        if (_entryButtons.Count > 0)
            _entryButtons[_selectedEntry].GrabFocus();
        else
            _filter.GrabFocus();
    }

    private void ApplyResponsivePanels()
    {
        bool overview = _selectedSection == 0;
        if (_compactLayout)
        {
            _body.Columns = 1;
            _listPanel.Visible = !overview && !_compactInspectorOpen;
            _inspectorPanel.Visible = overview || _compactInspectorOpen;
            _inspectorBack.Visible = !overview && _compactInspectorOpen;
            return;
        }

        _compactInspectorOpen = false;
        _body.Columns = overview ? 2 : 3;
        _listPanel.Visible = !overview;
        _inspectorPanel.Visible = true;
        _inspectorBack.Visible = false;
    }

    private LedgerEntry[] FilteredEntries()
    {
        IEnumerable<LedgerEntry> entries = EntriesForSection();
        if (_filterText.Length > 0)
        {
            entries = entries.Where(entry =>
                entry.Title.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
                || entry.Summary.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
                || entry.Detail.Contains(_filterText, StringComparison.OrdinalIgnoreCase));
        }
        return entries.ToArray();
    }

    private void RefreshSectionLabels()
    {
        for (int index = 0; index < _sectionButtons.Count; index++)
        {
            int count = SectionCount(index);
            _sectionButtons[index].Text = count > 0
                ? $"{SectionNames[index]}  {count}"
                : SectionNames[index];
        }
    }

    private int SectionCount(int section) =>
        _presentation is null
            ? 0
            : section switch
            {
                0 => 0,
                1 => _presentation.Attributes.Length,
                2 => _presentation.Skills.Length,
                3 => _presentation.Knacks.Length,
                4 => _presentation.Lessons.Length,
                5 => 0,
                6 => 0,
                _ => _presentation.PendingKnacks.Length,
            };

    private void ActivateInspector()
    {
        string key = _inspectorAction.GetMeta("canonical_key", "").AsString();
        if (key.Length == 1)
            KeyRequested?.Invoke(key[0]);
    }

    private void ClearInspector()
    {
        _inspectorKicker.Text = "LEDGER";
        _inspectorTitle.Text = "Nothing recorded";
        _inspectorSummary.Text = "";
        _inspectorDetail.Text = "This section will fill as the character changes.";
        _inspectorProgress.Visible = false;
        _inspectorProgressText.Visible = false;
        _inspectorAction.Visible = false;
    }

    private void RefreshButtonVisuals()
    {
        for (int index = 0; index < _sectionButtons.Count; index++)
            ApplySelectionStyle(_sectionButtons[index], index == _selectedSection);
        for (int index = 0; index < _entryButtons.Count; index++)
        {
            if (_entryButtons[index] is CharacterLedgerRow row)
                row.ApplyRowVisuals(_fonts, _scale, _palette, index == _selectedEntry);
            else
                ApplySelectionStyle(_entryButtons[index], index == _selectedEntry);
        }
    }

    private void ApplySelectionStyle(Button button, bool selected)
    {
        button.SetPressedNoSignal(selected);
        StyleBoxFlat selectedBox = UiThemeFactory.BorderBox(
            UiThemeFactory.Mix(_palette.Raised, _palette.Accent, 0.16f),
            _palette.Accent,
            _scale,
            _scale.Space2);
        selectedBox.BorderWidthLeft = Math.Max(4, (int)MathF.Round(4 * _scale.Scale));
        button.AddThemeStyleboxOverride(
            "normal",
            selected
                ? selectedBox
                : UiThemeFactory.BorderBox(_palette.Raised, _palette.Muted, _scale, _scale.Space2));
        button.AddThemeStyleboxOverride("pressed", selectedBox);
        button.AddThemeStyleboxOverride("hover_pressed", selectedBox);
        button.AddThemeStyleboxOverride("focus", UiThemeFactory.InsetFocusBox(_palette.Accent, _scale));
        button.AddThemeColorOverride("font_color", selected ? _palette.Accent : _palette.Text);
        button.AddThemeColorOverride("font_pressed_color", _palette.Accent);
    }
}

internal sealed partial class GearRow : Button
{
    private static readonly float[] ColumnWidths = [190, 76, 118, 178, 92, 136];
    private readonly Label[] _cells;
    private readonly bool _warning;

    public GearRow(GearPresentation gear, string state)
    {
        Text = "";
        ToggleMode = true;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, 62);
        _warning = !gear.MeetsRequirement;

        var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);
        var grid = new GridContainer
        {
            Columns = 6,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        margin.AddChild(grid);
        string[] values =
        [
            $"{gear.Key}  {gear.Name}",
            gear.Slot,
            gear.Benefit,
            gear.Requirement,
            $"{gear.MaximumWear - gear.Wear}/{gear.MaximumWear}",
            state,
        ];
        _cells = values.Select((value, index) =>
        {
            var label = new Label
            {
                Text = value,
                CustomMinimumSize = new Vector2(ColumnWidths[index], 0),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            grid.AddChild(label);
            return label;
        }).ToArray();
    }

    public void ApplyRowVisuals(
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette,
        bool selected)
    {
        CustomMinimumSize = new Vector2(0, Math.Max(62, 62 * scale.Scale));
        foreach ((Label cell, int index) in _cells.Select((value, index) => (value, index)))
        {
            cell.AddThemeFontOverride(
                "font",
                index is 0 or 5 ? fonts.BodySemibold : fonts.Body);
            cell.AddThemeFontSizeOverride("font_size", scale.Control);
            cell.AddThemeColorOverride(
                "font_color",
                _warning && (index == 3 || index == 5)
                    ? palette.Danger
                    : selected && index == 0
                        ? palette.Accent
                        : palette.Text);
            cell.CustomMinimumSize = new Vector2(ColumnWidths[index], 0);
        }
    }
}

internal sealed partial class PackScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly PanelContainer _panel;
    private readonly ScrollContainer _rootScroll;
    private readonly VBoxContainer _stack;
    private readonly GridContainer _shelf;
    private readonly GridContainer _body;
    private readonly PanelContainer _listPanel;
    private readonly VBoxContainer _gearList;
    private readonly Dictionary<Button, int> _gearIndices = [];
    private readonly List<int> _visibleGearIndices = [];
    private readonly Dictionary<string, Button> _filterButtons = [];
    private readonly OptionButton _sort;
    private readonly PanelContainer _inspectorPanel;
    private readonly ScrollContainer _inspectorScroll;
    private readonly Label _inspectorKicker;
    private readonly Label _equippedItem;
    private readonly Label _inspectorTitle;
    private readonly Label _benefit;
    private readonly Label _requirement;
    private readonly Label _condition;
    private readonly Label _craft;
    private readonly Label _warning;
    private readonly Button _equip;
    private readonly FlowContainer _resources;
    private readonly List<PanelContainer> _slotCards = [];
    private readonly List<Label> _slotLabels = [];
    private readonly List<Button> _gearButtons = [];
    private PackPresentation? _presentation;
    private int _selectedGear;
    private string _gearFilter = "All";
    private int _sortMode;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    public event Action<char>? KeyRequested;

    public PackScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        _panel = new PanelContainer();
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_panel);
        _stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _rootScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = false,
        };
        _panel.AddChild(_rootScroll);
        var rootMargin = WorldScreen.Wrap(_stack);
        rootMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rootMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
        _rootScroll.AddChild(rootMargin);

        var shelfHeader = new HBoxContainer();
        _stack.AddChild(shelfHeader);
        var eyebrow = new Label
        {
            Text = "EQUIPPED",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        shelfHeader.AddChild(eyebrow);
        var close = new Button { Text = "Return to world  Esc" };
        close.Pressed += () => KeyRequested?.Invoke('q');
        shelfHeader.AddChild(close);
        _shelf = new GridContainer { Columns = 3 };
        _stack.AddChild(_shelf);
        for (int index = 0; index < 3; index++)
        {
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _shelf.AddChild(card);
            var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            card.AddChild(WorldScreen.Wrap(label));
            _slotCards.Add(card);
            _slotLabels.Add(label);
        }

        _body = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _stack.AddChild(_body);
        _listPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddChild(_listPanel);
        var listStack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _listPanel.AddChild(WorldScreen.Wrap(listStack));
        var listEyebrow = new Label { Text = "CARRIED GEAR" };
        listStack.AddChild(listEyebrow);
        var listTools = new HBoxContainer();
        listStack.AddChild(listTools);
        foreach (string filter in new[] { "All", "Weapon", "Armor", "Ranged" })
        {
            var button = new Button
            {
                Text = filter,
                ToggleMode = true,
            };
            button.Pressed += () => SetGearFilter(filter);
            listTools.AddChild(button);
            _filterButtons[filter] = button;
        }
        var toolSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        listTools.AddChild(toolSpacer);
        var sortLabel = new Label
        {
            Text = "Sort",
            VerticalAlignment = VerticalAlignment.Center,
        };
        listTools.AddChild(sortLabel);
        _sort = new OptionButton();
        _sort.AddItem("Pack order");
        _sort.AddItem("Item A-Z");
        _sort.AddItem("Slot");
        _sort.ItemSelected += value =>
        {
            _sortMode = (int)value;
            RebuildGear();
        };
        listTools.AddChild(_sort);
        var columns = new GridContainer { Columns = 6 };
        listStack.AddChild(columns);
        float[] headingWidths = [190, 76, 118, 178, 92, 136];
        foreach ((string column, int index) in new[] { "ITEM", "SLOT", "BENEFIT", "REQUIREMENT", "WEAR", "STATE" }
            .Select((value, index) => (value, index)))
        {
            var label = new Label
            {
                Text = column,
                CustomMinimumSize = new Vector2(headingWidths[index], 0),
            };
            UiThemeFactory.Mark(label, "eyebrow", fonts, scale, palette);
            columns.AddChild(label);
        }
        var listScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        listStack.AddChild(listScroll);
        _gearList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        listScroll.AddChild(_gearList);
        listStack.AddChild(new HSeparator());
        var resourcesEyebrow = new Label { Text = "CARRIED RESOURCES" };
        listStack.AddChild(resourcesEyebrow);
        _resources = new FlowContainer();
        listStack.AddChild(_resources);

        _inspectorPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(460, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddChild(_inspectorPanel);
        _inspectorScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = false,
        };
        _inspectorPanel.AddChild(_inspectorScroll);
        var inspector = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var inspectorMargin = WorldScreen.Wrap(inspector);
        inspectorMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _inspectorScroll.AddChild(inspectorMargin);
        _inspectorKicker = new Label();
        inspector.AddChild(_inspectorKicker);
        _equippedItem = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        inspector.AddChild(_equippedItem);
        _inspectorTitle = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        inspector.AddChild(_inspectorTitle);
        _benefit = FactLabel(inspector);
        _requirement = FactLabel(inspector);
        _condition = FactLabel(inspector);
        _craft = FactLabel(inspector);
        _warning = new Label
        {
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        inspector.AddChild(_warning);
        _equip = new Button();
        _equip.Pressed += ActivateSelected;
        inspector.AddChild(_equip);

        UiThemeFactory.Mark(eyebrow, "eyebrow", fonts, scale, palette);
        UiThemeFactory.Mark(listEyebrow, "eyebrow", fonts, scale, palette);
        UiThemeFactory.Mark(resourcesEyebrow, "eyebrow", fonts, scale, palette);
        ApplyVisuals(scale, palette);
    }

    public void UpdateView(ClientInteractionContext context, bool becameVisible)
    {
        _presentation = context.Pack
            ?? throw new InvalidOperationException("Outfitter's Bench requires a pack projection.");
        for (int index = 0; index < _slotLabels.Count; index++)
        {
            EquippedSlotPresentation slot = _presentation.Slots[index];
            _slotLabels[index].Text = $"{slot.Slot.ToUpperInvariant()}\n{slot.Item}\n{slot.Summary}";
        }
        RebuildGear();
        RebuildResources();
        if (becameVisible && _gearButtons.Count > 0)
        {
            int visible = Math.Max(0, _visibleGearIndices.IndexOf(_selectedGear));
            _gearButtons[visible].CallDeferred(Control.MethodName.GrabFocus);
            Callable.From(() =>
            {
                if (_rootScroll.VerticalScrollMode == ScrollContainer.ScrollMode.Disabled)
                    _rootScroll.ScrollVertical = 0;
            }).CallDeferred();
        }
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        AddThemeConstantOverride("margin_left", scale.Space3);
        AddThemeConstantOverride("margin_right", scale.Space3);
        AddThemeConstantOverride("margin_top", scale.Space3);
        AddThemeConstantOverride("margin_bottom", scale.Space3);
        _stack.AddThemeConstantOverride("separation", scale.Space2);
        _shelf.AddThemeConstantOverride("h_separation", scale.Space2);
        _shelf.AddThemeConstantOverride("v_separation", scale.Space2);
        _body.AddThemeConstantOverride("h_separation", scale.Space2);
        _body.AddThemeConstantOverride("v_separation", scale.Space2);
        _gearList.AddThemeConstantOverride("separation", scale.Space1);
        _panel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Muted, scale, 0));
        _listPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, palette.Muted, scale, 0));
        _inspectorPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, 0));
        foreach (PanelContainer slot in _slotCards)
            slot.AddThemeStyleboxOverride(
                "panel",
                UiThemeFactory.BorderBox(palette.Panel, palette.Accent, scale, 0));
        foreach (Label label in _slotLabels)
        {
            label.AddThemeFontOverride("font", _fonts.BodySemibold);
            label.AddThemeFontSizeOverride("font_size", scale.Control);
            label.AddThemeColorOverride("font_color", palette.Text);
        }
        UiThemeFactory.Mark(_inspectorKicker, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_equippedItem, "muted", _fonts, scale, palette);
        UiThemeFactory.Mark(_inspectorTitle, "heading", _fonts, scale, palette);
        _warning.AddThemeFontOverride("font", _fonts.BodySemibold);
        _warning.AddThemeColorOverride("font_color", palette.Danger);
        foreach ((string filter, Button button) in _filterButtons)
        {
            bool selected = filter == _gearFilter;
            button.SetPressedNoSignal(selected);
            button.AddThemeColorOverride(
                "font_color",
                selected ? palette.Raised : palette.Text);
            button.AddThemeColorOverride(
                "font_pressed_color",
                selected ? palette.Raised : palette.Text);
            button.AddThemeStyleboxOverride(
                "normal",
                selected
                    ? UiThemeFactory.BorderBox(palette.Accent, palette.Accent, scale, scale.Space2)
                    : UiThemeFactory.BorderBox(palette.Raised, palette.Muted, scale, scale.Space2));
            button.AddThemeStyleboxOverride(
                "pressed",
                UiThemeFactory.BorderBox(palette.Accent, palette.Accent, scale, scale.Space2));
        }
        RefreshGearButtons();
    }

    public void ApplyLayout(float viewportWidth)
    {
        bool stacked = viewportWidth < 1450 || _scale.Scale >= 1.5f;
        _rootScroll.VerticalScrollMode = stacked
            ? ScrollContainer.ScrollMode.Auto
            : ScrollContainer.ScrollMode.Disabled;
        if (!stacked)
            _rootScroll.ScrollVertical = 0;
        _body.Columns = stacked ? 1 : 2;
        _listPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 800, stacked ? 360 : 0);
        _inspectorPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 500, stacked ? 360 : 0);
        _body.CustomMinimumSize = new Vector2(0, stacked ? 760 : 540);
        _shelf.Columns = stacked ? 1 : 3;
    }

    public bool MoveSelection(int delta)
    {
        if (_gearButtons.Count == 0)
            return false;
        int visible = _visibleGearIndices.IndexOf(_selectedGear);
        if (visible < 0)
            visible = 0;
        else
            visible = (visible + delta + _gearButtons.Count) % _gearButtons.Count;
        _selectedGear = _visibleGearIndices[visible];
        SelectGear(_selectedGear);
        _gearButtons[visible].GrabFocus();
        return true;
    }

    private void RebuildGear()
    {
        foreach (Node child in _gearList.GetChildren())
        {
            _gearList.RemoveChild(child);
            child.QueueFree();
        }
        _gearButtons.Clear();
        _gearIndices.Clear();
        _visibleGearIndices.Clear();
        if (_presentation is null || _presentation.Gear.Length == 0)
        {
            var empty = new Label { Text = "No gear is currently carried." };
            _gearList.AddChild(empty);
            UiThemeFactory.Mark(empty, "muted", _fonts, _scale, _palette);
            ClearInspector();
            return;
        }

        IEnumerable<int> indices = Enumerable.Range(0, _presentation.Gear.Length)
            .Where(index => _gearFilter == "All"
                || _presentation.Gear[index].Slot.Equals(
                    _gearFilter,
                    StringComparison.OrdinalIgnoreCase));
        indices = _sortMode switch
        {
            1 => indices.OrderBy(index => _presentation.Gear[index].Name),
            2 => indices
                .OrderBy(index => _presentation.Gear[index].Slot)
                .ThenBy(index => _presentation.Gear[index].Name),
            _ => indices,
        };
        foreach (int index in indices)
        {
            int selected = index;
            GearPresentation gear = _presentation.Gear[index];
            string state = gear.Equipped
                ? "Equipped"
                : gear.MeetsRequirement
                    ? "Ready"
                    : "⚠ Reduced benefit";
            var button = new GearRow(gear, state);
            button.Pressed += () => SelectGear(selected);
            _gearList.AddChild(button);
            _gearButtons.Add(button);
            _gearIndices[button] = index;
            _visibleGearIndices.Add(index);
        }
        if (_gearButtons.Count == 0)
        {
            var empty = new Label
            {
                Text = "No carried gear matches this filter.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            _gearList.AddChild(empty);
            UiThemeFactory.Mark(empty, "muted", _fonts, _scale, _palette);
            ClearInspector();
            return;
        }
        if (!_visibleGearIndices.Contains(_selectedGear))
            _selectedGear = _visibleGearIndices[0];
        SelectGear(_selectedGear);
    }

    private void SetGearFilter(string filter)
    {
        _gearFilter = filter;
        RebuildGear();
        ApplyVisuals(_scale, _palette);
        if (_gearButtons.Count > 0)
            _gearButtons[0].GrabFocus();
    }

    private void RebuildResources()
    {
        foreach (Node child in _resources.GetChildren())
        {
            _resources.RemoveChild(child);
            child.QueueFree();
        }
        if (_presentation is null)
            return;
        foreach (CarriedResourcePresentation resource in _presentation.Resources)
        {
            if (resource.Amount is "0" or "No")
                continue;
            var chip = new PanelContainer();
            chip.AddThemeStyleboxOverride(
                "panel",
                UiThemeFactory.BorderBox(_palette.Panel, _palette.Muted, _scale, 0));
            var label = new Label { Text = $"{resource.Name}  {resource.Amount}" };
            label.AddThemeFontOverride("font", _fonts.MonoSemibold);
            label.AddThemeFontSizeOverride("font_size", _scale.Metadata);
            label.AddThemeColorOverride("font_color", _palette.Warm);
            chip.AddChild(WorldScreen.Wrap(label));
            _resources.AddChild(chip);
        }
    }

    private void SelectGear(int index)
    {
        if (_presentation is null || index < 0 || index >= _presentation.Gear.Length)
            return;
        _selectedGear = index;
        GearPresentation gear = _presentation.Gear[index];
        GearPresentation? equipped = _presentation.Gear.FirstOrDefault(item =>
            item.Equipped
            && item.Slot.Equals(gear.Slot, StringComparison.OrdinalIgnoreCase));
        _inspectorKicker.Text = "COMPARE AND EQUIP";
        _equippedItem.Text = equipped is null
            ? $"EQUIPPED {gear.Slot.ToUpperInvariant()}\nEmpty"
            : $"EQUIPPED {gear.Slot.ToUpperInvariant()}\n{equipped.Name}";
        _inspectorTitle.Text = gear.Name;
        _benefit.Text =
            $"BENEFIT\nEquipped: {equipped?.Benefit ?? "None"}\nSelected: {gear.Benefit}";
        _requirement.Text =
            $"REQUIREMENT\nEquipped: {equipped?.Requirement ?? "None"}\nSelected: {gear.Requirement}";
        _condition.Text =
            $"WEAR\nEquipped: {ConditionOf(equipped)}\nSelected: {ConditionOf(gear)}";
        _craft.Text =
            $"PRACTICE AND MOVE\nEquipped: {CraftOf(equipped)}\nSelected: {CraftOf(gear)}";
        _warning.Visible = !gear.MeetsRequirement;
        _warning.Text = gear.MeetsRequirement
            ? ""
            : $"⚠ REDUCED BENEFIT\n{gear.Requirement}. You may equip this item, but its canonical benefit is reduced until the requirement is met.";
        _equip.Visible = true;
        _equip.Disabled = gear.Equipped;
        _equip.Text = gear.Equipped ? "Equipped" : $"Equip  {gear.Key}";
        _equip.SetMeta("canonical_key", gear.Key.ToString());
        RefreshGearButtons();
    }

    private void ActivateSelected()
    {
        string key = _equip.GetMeta("canonical_key", "").AsString();
        if (key.Length == 1 && !_equip.Disabled)
            KeyRequested?.Invoke(key[0]);
    }

    private void ClearInspector()
    {
        _inspectorKicker.Text = "GEAR";
        _equippedItem.Text = "";
        _inspectorTitle.Text = "Nothing carried";
        _benefit.Text = "Your hands are empty.";
        _requirement.Text = "";
        _condition.Text = "";
        _craft.Text = "";
        _warning.Visible = false;
        _equip.Visible = false;
    }

    private static string ConditionOf(GearPresentation? gear) =>
        gear is null
            ? "None"
            : $"{gear.MaximumWear - gear.Wear} of {gear.MaximumWear} uses remain";

    private static string CraftOf(GearPresentation? gear) =>
        gear is null ? "None" : $"{gear.Craft}  |  {gear.Move}";

    private void RefreshGearButtons()
    {
        for (int index = 0; index < _gearButtons.Count; index++)
        {
            Button button = _gearButtons[index];
            bool selected = _gearIndices.TryGetValue(button, out int gearIndex)
                && gearIndex == _selectedGear;
            button.SetPressedNoSignal(selected);
            StyleBoxFlat selectedBox = UiThemeFactory.BorderBox(
                UiThemeFactory.Mix(_palette.Raised, _palette.Accent, 0.16f),
                _palette.Accent,
                _scale,
                _scale.Space2);
            selectedBox.BorderWidthLeft = Math.Max(4, (int)MathF.Round(4 * _scale.Scale));
            button.AddThemeStyleboxOverride(
                "normal",
                selected
                    ? selectedBox
                    : UiThemeFactory.BorderBox(_palette.Raised, _palette.Muted, _scale, _scale.Space2));
            button.AddThemeStyleboxOverride("pressed", selectedBox);
            button.AddThemeStyleboxOverride("hover_pressed", selectedBox);
            button.AddThemeStyleboxOverride("focus", UiThemeFactory.InsetFocusBox(_palette.Accent, _scale));
            button.AddThemeColorOverride("font_color", selected ? _palette.Accent : _palette.Text);
            button.AddThemeColorOverride("font_pressed_color", _palette.Accent);
            button.AddThemeFontOverride("font", _fonts.MonoSemibold);
            button.AddThemeFontSizeOverride("font_size", _scale.Metadata);
            if (button is GearRow row)
                row.ApplyRowVisuals(_fonts, _scale, _palette, selected);
        }
    }

    private static Label FactLabel(VBoxContainer parent)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        parent.AddChild(label);
        return label;
    }
}
