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
    private readonly ScrollContainer _bodyScroll;
    private readonly GridContainer _body;
    private readonly PanelContainer _sectionPanel;
    private readonly VBoxContainer _sections;
    private readonly PanelContainer _listPanel;
    private readonly VBoxContainer _entries;
    private readonly PanelContainer _inspectorPanel;
    private readonly VBoxContainer _inspector;
    private readonly Label _inspectorKicker;
    private readonly Label _inspectorTitle;
    private readonly Label _inspectorSummary;
    private readonly Label _inspectorDetail;
    private readonly ProgressBar _inspectorProgress;
    private readonly Label _inspectorProgressText;
    private readonly Button _inspectorAction;
    private readonly Button _close;
    private readonly List<Button> _sectionButtons = [];
    private readonly List<Button> _entryButtons = [];
    private CharacterPresentation? _presentation;
    private int _selectedSection;
    private int _selectedEntry;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    private static readonly string[] SectionNames =
    [
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

        var heading = new HBoxContainer();
        _stack.AddChild(heading);
        var identityStack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        heading.AddChild(identityStack);
        var eyebrow = new Label { Text = "CHARACTER LEDGER" };
        identityStack.AddChild(eyebrow);
        _name = new Label();
        identityStack.AddChild(_name);
        _identity = new Label();
        identityStack.AddChild(_identity);
        _close = new Button { Text = "Return  Esc" };
        _close.Pressed += () => KeyRequested?.Invoke('q');
        heading.AddChild(_close);

        var meters = new HBoxContainer();
        _stack.AddChild(meters);
        _health = new ResourceMeter("♥") { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _stamina = new ResourceMeter("◆") { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _focus = new ResourceMeter("◉") { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        meters.AddChild(_health);
        meters.AddChild(_stamina);
        meters.AddChild(_focus);

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

        _sectionPanel = new PanelContainer { CustomMinimumSize = new Vector2(220, 0) };
        _body.AddChild(_sectionPanel);
        _sections = new VBoxContainer();
        _sectionPanel.AddChild(WorldScreen.Wrap(_sections));
        var sectionEyebrow = new Label { Text = "SECTIONS" };
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

        _listPanel = new PanelContainer { CustomMinimumSize = new Vector2(430, 0) };
        _body.AddChild(_listPanel);
        var listStack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _listPanel.AddChild(WorldScreen.Wrap(listStack));
        var listEyebrow = new Label { Text = "COMPLETE LIST" };
        listStack.AddChild(listEyebrow);
        var listScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        listStack.AddChild(listScroll);
        _entries = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        listScroll.AddChild(_entries);

        _inspectorPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(360, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _body.AddChild(_inspectorPanel);
        _inspector = new VBoxContainer();
        _inspectorPanel.AddChild(WorldScreen.Wrap(_inspector));
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
        UiThemeFactory.Mark(listEyebrow, "eyebrow", fonts, scale, palette);
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
        _selectedSection = Math.Clamp(_selectedSection, 0, SectionNames.Length - 1);
        RebuildEntries();
        if (becameVisible)
            _sectionButtons[_selectedSection].CallDeferred(Control.MethodName.GrabFocus);
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
        bool stacked = viewportWidth < 1180 || _scale.Scale >= 1.5f;
        _body.Columns = stacked ? 1 : 3;
        _sectionPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 220, stacked ? 220 : 0);
        _listPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 430, stacked ? 360 : 0);
        _inspectorPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 360, stacked ? 320 : 0);
        _body.CustomMinimumSize = new Vector2(
            Math.Max(700, viewportWidth - _scale.Space4 * 4),
            stacked ? 940 : 0);
    }

    public bool MoveSelection(int delta)
    {
        if (_entryButtons.Count == 0)
            return false;
        _selectedEntry = (_selectedEntry + delta + _entryButtons.Count) % _entryButtons.Count;
        SelectEntry(_selectedEntry);
        _entryButtons[_selectedEntry].GrabFocus();
        return true;
    }

    private void SelectSection(int index)
    {
        _selectedSection = index;
        _selectedEntry = 0;
        RebuildEntries();
        if (_entryButtons.Count > 0)
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
        foreach ((LedgerEntry entry, int index) in EntriesForSection().Select((value, index) => (value, index)))
        {
            int selected = index;
            var button = new Button
            {
                Text = EntryButtonText(entry),
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
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
            SelectEntry(_selectedEntry);
        }
        RefreshButtonVisuals();
    }

    private IReadOnlyList<LedgerEntry> EntriesForSection()
    {
        if (_presentation is null)
            return [];
        return _selectedSection switch
        {
            0 => _presentation.Attributes
                .Select(item => new LedgerEntry(
                    item.Name,
                    "ATTRIBUTE",
                    $"Current value {item.Value}",
                    item.Description))
                .ToArray(),
            1 => _presentation.Skills
                .Select(item => new LedgerEntry(
                    item.Name,
                    $"SKILL  LEVEL {item.Level}",
                    $"{item.Uses} of {item.NextLevelUses} uses",
                    item.Description,
                    item.Uses,
                    item.NextLevelUses))
                .ToArray(),
            2 => _presentation.Knacks
                .Select(item => new LedgerEntry(item.Name, "KNACK", item.Detail, "Kept for good."))
                .ToArray(),
            3 => _presentation.Lessons
                .Select(item => new LedgerEntry(item.Name, "LESSON", item.Detail, "Learned knowledge travels with the character."))
                .ToArray(),
            4 =>
            [
                new LedgerEntry("Burden", "CHARACTER", _presentation.Burden, "A lasting part of this character."),
                new LedgerEntry("Scars", "CHARACTER", _presentation.Scars, "Marks carried by the character."),
            ],
            5 =>
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

    private static string EntryButtonText(LedgerEntry entry) =>
        entry.Summary.Length > 0
            ? $"{entry.Title}\n{entry.Summary}"
            : entry.Title;

    private void SelectEntry(int index)
    {
        LedgerEntry[] entries = EntriesForSection().ToArray();
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
        RefreshButtonVisuals();
    }

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
            ApplySelectionStyle(_entryButtons[index], index == _selectedEntry);
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

internal sealed partial class PackScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly PanelContainer _panel;
    private readonly VBoxContainer _stack;
    private readonly GridContainer _shelf;
    private readonly GridContainer _body;
    private readonly PanelContainer _listPanel;
    private readonly VBoxContainer _gearList;
    private readonly PanelContainer _inspectorPanel;
    private readonly Label _inspectorKicker;
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
        var rootScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _panel.AddChild(rootScroll);
        var rootMargin = WorldScreen.Wrap(_stack);
        rootMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rootScroll.AddChild(rootMargin);

        var header = new HBoxContainer();
        _stack.AddChild(header);
        var titleStack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(titleStack);
        var eyebrow = new Label { Text = "OUTFITTER'S BENCH" };
        titleStack.AddChild(eyebrow);
        var title = new Label { Text = "Inventory and equipment" };
        titleStack.AddChild(title);
        var subtitle = new Label { Text = "Compare what you carry with what is serving now." };
        titleStack.AddChild(subtitle);
        var close = new Button { Text = "Return  Esc" };
        close.Pressed += () => KeyRequested?.Invoke('q');
        header.AddChild(close);

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
        var listScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        listStack.AddChild(listScroll);
        _gearList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        listScroll.AddChild(_gearList);

        _inspectorPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(460, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddChild(_inspectorPanel);
        var inspector = new VBoxContainer();
        _inspectorPanel.AddChild(WorldScreen.Wrap(inspector));
        _inspectorKicker = new Label();
        inspector.AddChild(_inspectorKicker);
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

        var resourcesPanel = new PanelContainer();
        _stack.AddChild(resourcesPanel);
        var resourcesStack = new VBoxContainer();
        resourcesPanel.AddChild(WorldScreen.Wrap(resourcesStack));
        var resourcesEyebrow = new Label { Text = "CARRIED RESOURCES" };
        resourcesStack.AddChild(resourcesEyebrow);
        _resources = new FlowContainer();
        resourcesStack.AddChild(_resources);

        UiThemeFactory.Mark(eyebrow, "eyebrow", fonts, scale, palette);
        UiThemeFactory.Mark(title, "heading", fonts, scale, palette);
        UiThemeFactory.Mark(subtitle, "muted", fonts, scale, palette);
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
            _gearButtons[_selectedGear].CallDeferred(Control.MethodName.GrabFocus);
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
        UiThemeFactory.Mark(_inspectorTitle, "heading", _fonts, scale, palette);
        _warning.AddThemeFontOverride("font", _fonts.BodySemibold);
        _warning.AddThemeColorOverride("font_color", palette.Danger);
        RefreshGearButtons();
    }

    public void ApplyLayout(float viewportWidth)
    {
        bool stacked = viewportWidth < 1180 || _scale.Scale >= 1.5f;
        _body.Columns = stacked ? 1 : 2;
        _listPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 520, stacked ? 300 : 0);
        _inspectorPanel.CustomMinimumSize = new Vector2(stacked ? 0 : 460, stacked ? 320 : 0);
        _shelf.Columns = stacked ? 1 : 3;
    }

    public bool MoveSelection(int delta)
    {
        if (_gearButtons.Count == 0)
            return false;
        _selectedGear = (_selectedGear + delta + _gearButtons.Count) % _gearButtons.Count;
        SelectGear(_selectedGear);
        _gearButtons[_selectedGear].GrabFocus();
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
        if (_presentation is null || _presentation.Gear.Length == 0)
        {
            var empty = new Label { Text = "No gear is currently carried." };
            _gearList.AddChild(empty);
            UiThemeFactory.Mark(empty, "muted", _fonts, _scale, _palette);
            ClearInspector();
            return;
        }
        for (int index = 0; index < _presentation.Gear.Length; index++)
        {
            int selected = index;
            GearPresentation gear = _presentation.Gear[index];
            string state = gear.Equipped ? "EQUIPPED" : gear.MeetsRequirement ? "READY" : "UNDER REQUIREMENT";
            var button = new Button
            {
                Text = $"{gear.Key}  {gear.Name}\n{gear.Slot}  |  {gear.Benefit}  |  {state}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += () => SelectGear(selected);
            _gearList.AddChild(button);
            _gearButtons.Add(button);
        }
        _selectedGear = Math.Clamp(_selectedGear, 0, _gearButtons.Count - 1);
        SelectGear(_selectedGear);
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
        _inspectorKicker.Text = gear.Equipped ? $"{gear.Slot.ToUpperInvariant()}  |  EQUIPPED" : gear.Slot.ToUpperInvariant();
        _inspectorTitle.Text = gear.Name;
        _benefit.Text = $"Benefit\n{gear.Benefit}";
        _requirement.Text = $"Requirement\n{gear.Requirement}";
        _condition.Text = $"Condition\n{gear.MaximumWear - gear.Wear} of {gear.MaximumWear} uses remain";
        _craft.Text = $"Craft\n{gear.Craft}  |  {gear.Move}";
        _warning.Visible = !gear.MeetsRequirement;
        _warning.Text = gear.MeetsRequirement
            ? ""
            : "UNDER REQUIREMENT\nYou may equip this item, but its benefit is reduced until the requirement is met.";
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
        _inspectorTitle.Text = "Nothing carried";
        _benefit.Text = "Your hands are empty.";
        _requirement.Text = "";
        _condition.Text = "";
        _craft.Text = "";
        _warning.Visible = false;
        _equip.Visible = false;
    }

    private void RefreshGearButtons()
    {
        for (int index = 0; index < _gearButtons.Count; index++)
        {
            Button button = _gearButtons[index];
            bool selected = index == _selectedGear;
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

    private static Label FactLabel(VBoxContainer parent)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        parent.AddChild(label);
        return label;
    }
}
