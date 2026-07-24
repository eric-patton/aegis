using System.Text;
using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed partial class CreationScreen : MarginContainer
{
    private readonly ClientFonts _fonts;
    private readonly Label _eyebrow;
    private readonly ProgressBar _progress;
    private readonly Label _prompt;
    private readonly LineEdit _entry;
    private readonly RichTextLabel _review;
    private readonly ScrollContainer _choiceScroll;
    private readonly VBoxContainer _choices;
    private readonly Button _back;
    private readonly Button _continue;
    private readonly Label _hint;
    private readonly VBoxContainer _stack;
    private CreationStage? _stage;
    private string _submittedText = "";
    private bool _suppressEntry;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    public event Action<char>? KeyRequested;

    public CreationScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(820, 520),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Raised, palette.Accent, scale, 0));
        center.AddChild(panel);

        var inner = new MarginContainer();
        panel.AddChild(inner);
        _stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        inner.AddChild(_stack);

        _eyebrow = new Label();
        _stack.AddChild(_eyebrow);

        _progress = new ProgressBar
        {
            MinValue = 1,
            MaxValue = 10,
            ShowPercentage = false,
        };
        _stack.AddChild(_progress);

        _prompt = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _stack.AddChild(_prompt);

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
        _choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _choiceScroll.AddChild(_choices);

        var footer = new HBoxContainer();
        _stack.AddChild(footer);
        _back = new Button { Text = "Back  Esc" };
        _back.Pressed += () => KeyRequested?.Invoke('[');
        footer.AddChild(_back);

        _hint = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        footer.AddChild(_hint);

        _continue = new Button();
        _continue.Pressed += () => KeyRequested?.Invoke('.');
        footer.AddChild(_continue);

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
        _progress.CustomMinimumSize = new Vector2(0, Math.Max(6, scale.Space1));
        UiThemeFactory.Mark(_eyebrow, "eyebrow", _fonts, scale, palette);
        UiThemeFactory.Mark(_prompt, "heading", _fonts, scale, palette);
        UiThemeFactory.Mark(_hint, "muted", _fonts, scale, palette);
    }

    public void UpdateView(ClientInteractionContext context, bool becameVisible)
    {
        CreationPresentation creation = context.Creation
            ?? throw new InvalidOperationException("Creation screen requires a creation projection.");
        bool stageChanged = creation.Stage != _stage;
        _stage = creation.Stage;
        _eyebrow.Text = $"BECOMING  {creation.Step:00} / {creation.TotalSteps:00}";
        _progress.Value = creation.Step;
        _prompt.Text = creation.Prompt;
        _back.Disabled = creation.Step == 1;

        bool textStage = creation.Stage is CreationStage.Face or CreationStage.Name;
        bool reviewStage = creation.Stage == CreationStage.Review;
        _entry.Visible = textStage;
        _review.Visible = reviewStage;
        _choiceScroll.Visible = creation.Choices.Length > 0;
        _continue.Visible = textStage || reviewStage;
        _continue.Text = reviewStage ? "Begin  Enter" : "Continue  Enter";
        _hint.Text = textStage
            ? "Type normally. Backspace erases. Escape returns."
            : reviewStage
                ? "Review every choice before beginning."
                : "Use a number key or select a row.";

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
        _choices.AddThemeConstantOverride("separation", _scale.Space1);

        var buttons = new List<Button>();
        foreach (CreationChoice choice in choices)
        {
            string detail = choice.Detail.Length > 0 ? $"\n{choice.Detail}" : "";
            string reason = !choice.Enabled && choice.DisabledReason.Length > 0
                ? $"\nUnavailable: {choice.DisabledReason}"
                : "";
            var button = new Button
            {
                Text = $"{choice.Key}  {choice.Name}\n{choice.Description}{detail}{reason}",
                Disabled = !choice.Enabled,
                Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
                CustomMinimumSize = new Vector2(0, Math.Max(84, _scale.Space4 * 3)),
            };
            char key = choice.Key;
            button.Pressed += () => KeyRequested?.Invoke(key);
            _choices.AddChild(button);
            buttons.Add(button);
        }

        if (buttons.Count > 1)
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                buttons[index].FocusNeighborTop = buttons[(index - 1 + buttons.Count) % buttons.Count].GetPath();
                buttons[index].FocusNeighborBottom = buttons[(index + 1) % buttons.Count].GetPath();
            }
        }
    }

    private Button? FirstEnabledChoice() =>
        _choices.GetChildren().OfType<Button>().FirstOrDefault(button => !button.Disabled);
}

internal sealed partial class WorldScreen : VBoxContainer
{
    private readonly ClientFonts _fonts;
    private readonly MapGridControl _map;
    private readonly Label _status;
    private readonly RichTextLabel _activity;
    private readonly Label _hint;
    private readonly HSplitContainer _split;
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private bool _lightTheme;

    public WorldScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette, bool lightTheme)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        _lightTheme = lightTheme;
        SizeFlagsVertical = SizeFlags.ExpandFill;

        _split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(_split);

        var mapPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _split.AddChild(mapPanel);
        var mapMargin = new MarginContainer();
        mapPanel.AddChild(mapMargin);
        _map = new MapGridControl(fonts.Mono, palette, lightTheme)
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        mapMargin.AddChild(_map);

        var rail = new VBoxContainer { CustomMinimumSize = new Vector2(320, 0) };
        _split.AddChild(rail);
        var statusPanel = new PanelContainer();
        rail.AddChild(statusPanel);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        statusPanel.AddChild(Wrap(_status));

        var activityPanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        rail.AddChild(activityPanel);
        _activity = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        activityPanel.AddChild(Wrap(_activity));

        _hint = new Label
        {
            Text = "Arrow keys and HJKL move. Open Move for all eight directions.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        AddChild(_hint);
        ApplyVisuals(scale, palette, lightTheme);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette, bool lightTheme)
    {
        _scale = scale;
        _palette = palette;
        _lightTheme = lightTheme;
        AddThemeConstantOverride("separation", scale.Space2);
        _split.SplitOffsets = [Math.Max(660, (int)(760 * scale.Scale))];
        UiThemeFactory.Mark(_hint, "muted", _fonts, scale, palette);
        if (_map is not null && _map.IsInsideTree())
            _map.QueueRedraw();
    }

    public void UpdateView(Frame frame, ClientInteractionContext context, string status)
    {
        _status.Text = status;
        _map.UpdateFrame(frame, _fonts.Mono, _palette, _lightTheme);
        _activity.Text = LogMarkup(context.Transcript.TakeLast(12), _palette);
    }

    private static MarginContainer Wrap(Control child)
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
                LogTone.Danger => palette.Danger,
                LogTone.Reward => palette.Warm,
                LogTone.Aegis => palette.Accent,
                LogTone.Combat => palette.Text,
                _ => palette.Muted,
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
    private readonly VBoxContainer _actions;
    private readonly RichTextLabel _transcript;
    private readonly Button _leave;
    private readonly HSplitContainer _split;
    private UiScaleTokens _scale;
    private UiPalette _palette;

    public event Action<char>? KeyRequested;

    public ConversationScreen(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        var stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(stack);
        _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        stack.AddChild(_title);

        _split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        stack.AddChild(_split);
        var actionScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(390, 0),
        };
        _split.AddChild(actionScroll);
        _actions = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        actionScroll.AddChild(_actions);

        _transcript = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _split.AddChild(_transcript);

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
        UiThemeFactory.Mark(_title, "heading", _fonts, scale, palette);
    }

    public void UpdateView(ClientInteractionContext context, bool becameVisible)
    {
        _title.Text = context.Title;
        RebuildActions(context.Actions, becameVisible);
        _transcript.Text = WorldScreen.LogMarkup(context.Transcript, _palette);
        Callable.From(ScrollToBottom).CallDeferred();
    }

    private void RebuildActions(IReadOnlyList<ClientAction> actions, bool focusFirst)
    {
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
            _actions.AddChild(button);
            buttons.Add(button);
        }
        if (buttons.Count > 0)
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                buttons[index].FocusNeighborTop = buttons[(index - 1 + buttons.Count) % buttons.Count].GetPath();
                buttons[index].FocusNeighborBottom = buttons[(index + 1) % buttons.Count].GetPath();
            }
            if (focusFirst)
                buttons.FirstOrDefault(button => !button.Disabled)?.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private void ScrollToBottom()
    {
        VScrollBar bar = _transcript.GetVScrollBar();
        bar.Value = bar.MaxValue;
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
