using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed partial class CreationChoiceCard : PanelContainer
{
    private readonly ClientFonts _fonts;
    private readonly PanelContainer _keyPanel;
    private readonly Label _key;
    private readonly Label _name;
    private readonly Label _selectedMark;
    private readonly Label _description;
    private readonly PanelContainer _mechanicsPanel;
    private readonly Label _detail;
    private readonly Label _projection;
    private readonly Label _unavailable;
    private readonly GridContainer _layout;
    private readonly MarginContainer _contentMargin;
    private readonly VBoxContainer _identity;
    private readonly VBoxContainer _mechanics;
    private UiScaleTokens _scale;
    private UiPalette _palette;
    private bool _selected;

    public CreationChoice Choice { get; }
    public Button Selection { get; }

    public CreationChoiceCard(
        CreationChoice choice,
        ClientFonts fonts,
        UiScaleTokens scale,
        UiPalette palette)
    {
        Choice = choice;
        _fonts = fonts;
        _scale = scale;
        _palette = palette;
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        Selection = new Button
        {
            Text = "",
            Disabled = !choice.Enabled,
            ToggleMode = true,
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        AddChild(Selection);

        _contentMargin = IgnoreMouse(new MarginContainer());
        AddChild(_contentMargin);
        _layout = IgnoreMouse(new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _contentMargin.AddChild(_layout);

        _identity = IgnoreMouse(new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _layout.AddChild(_identity);

        var heading = IgnoreMouse(new HBoxContainer());
        _identity.AddChild(heading);
        _keyPanel = IgnoreMouse(new PanelContainer());
        heading.AddChild(_keyPanel);
        _key = IgnoreMouse(new Label
        {
            Text = choice.Key.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        _keyPanel.AddChild(_key);
        _name = IgnoreMouse(new Label
        {
            Text = choice.Name,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        heading.AddChild(_name);
        _selectedMark = IgnoreMouse(new Label
        {
            Text = "SELECTED",
            Visible = false,
            VerticalAlignment = VerticalAlignment.Center,
        });
        heading.AddChild(_selectedMark);

        _description = IgnoreMouse(new Label
        {
            Text = choice.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _identity.AddChild(_description);

        _mechanicsPanel = IgnoreMouse(new PanelContainer
        {
            Visible = choice.Detail.Length > 0
                || choice.Projection.Length > 0
                || choice.DisabledReason.Length > 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        });
        _layout.AddChild(_mechanicsPanel);
        _mechanics = IgnoreMouse(new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        });
        _mechanicsPanel.AddChild(_mechanics);
        _detail = IgnoreMouse(new Label
        {
            Text = choice.Detail,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _mechanics.AddChild(_detail);
        _projection = IgnoreMouse(new Label
        {
            Text = choice.Projection,
            Visible = choice.Projection.Length > 0,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _mechanics.AddChild(_projection);
        _unavailable = IgnoreMouse(new Label
        {
            Text = choice.DisabledReason.Length > 0
                ? $"Unavailable: {choice.DisabledReason}"
                : "",
            Visible = choice.DisabledReason.Length > 0,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        _mechanics.AddChild(_unavailable);

        ApplyVisuals(scale, palette);
    }

    public void ApplyVisuals(UiScaleTokens scale, UiPalette palette)
    {
        _scale = scale;
        _palette = palette;
        Color quietBorder = Choice.Enabled ? palette.Muted : UiThemeFactory.WithAlpha(palette.Muted, 0.5f);
        Color disabledBackground = UiThemeFactory.Mix(palette.Panel, palette.Background, 0.35f);
        Color selectedBackground = UiThemeFactory.Mix(palette.Raised, palette.Accent, 0.15f);
        StyleBoxFlat selectedBox = UiThemeFactory.BorderBox(
            selectedBackground,
            palette.Accent,
            scale,
            scale.Space2);
        selectedBox.BorderWidthLeft = Math.Max(5, (int)MathF.Round(5 * scale.Scale));
        Selection.AddThemeStyleboxOverride(
            "normal",
            UiThemeFactory.BorderBox(palette.Raised, quietBorder, scale, scale.Space2));
        Selection.AddThemeStyleboxOverride(
            "hover",
            UiThemeFactory.BorderBox(
                UiThemeFactory.Mix(palette.Raised, palette.Accent, 0.08f),
                palette.Accent,
                scale,
                scale.Space2));
        Selection.AddThemeStyleboxOverride(
            "pressed",
            selectedBox);
        Selection.AddThemeStyleboxOverride(
            "hover_pressed",
            selectedBox);
        Selection.AddThemeStyleboxOverride(
            "disabled",
            UiThemeFactory.BorderBox(disabledBackground, quietBorder, scale, scale.Space2));
        Selection.AddThemeStyleboxOverride("focus", UiThemeFactory.InsetFocusBox(palette.Accent, scale));

        _contentMargin.AddThemeConstantOverride("margin_left", scale.Space2);
        _contentMargin.AddThemeConstantOverride("margin_right", scale.Space2);
        _contentMargin.AddThemeConstantOverride("margin_top", scale.Space1);
        _contentMargin.AddThemeConstantOverride("margin_bottom", scale.Space1);
        _layout.AddThemeConstantOverride("h_separation", scale.Space2);
        _layout.AddThemeConstantOverride("v_separation", scale.Space2);
        _identity.AddThemeConstantOverride("separation", scale.Space1);
        _mechanics.AddThemeConstantOverride("separation", scale.Space1);

        _keyPanel.CustomMinimumSize = new Vector2(
            Math.Max(34, scale.Control + scale.Space2),
            Math.Max(34, scale.Control + scale.Space1));
        _keyPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, palette.Accent, scale, scale.Space1));
        _mechanicsPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(palette.Panel, quietBorder, scale, scale.Space1));

        _key.AddThemeFontOverride("font", _fonts.MonoSemibold);
        _key.AddThemeFontSizeOverride("font_size", scale.Metadata);
        _key.AddThemeColorOverride(
            "font_color",
            Choice.Enabled ? palette.Accent : palette.Muted);
        _name.AddThemeFontOverride("font", _fonts.BodySemibold);
        _name.AddThemeFontSizeOverride("font_size", scale.Body);
        _name.AddThemeColorOverride(
            "font_color",
            Choice.Enabled ? palette.Accent : palette.Muted);
        _description.AddThemeFontOverride("font", _fonts.Body);
        _description.AddThemeFontSizeOverride("font_size", scale.Control);
        _description.AddThemeColorOverride(
            "font_color",
            Choice.Enabled ? palette.Text : palette.Muted);
        _detail.AddThemeFontOverride("font", _fonts.BodySemibold);
        _detail.AddThemeFontSizeOverride("font_size", scale.Control);
        _detail.AddThemeColorOverride(
            "font_color",
            Choice.Enabled ? palette.Text : palette.Muted);
        _projection.AddThemeFontOverride("font", _fonts.MonoSemibold);
        _projection.AddThemeFontSizeOverride("font_size", scale.Body);
        _projection.AddThemeColorOverride(
            "font_color",
            Choice.Enabled ? palette.Accent : palette.Muted);
        _unavailable.AddThemeFontOverride("font", _fonts.BodySemibold);
        _unavailable.AddThemeFontSizeOverride("font_size", scale.Metadata);
        _unavailable.AddThemeColorOverride("font_color", palette.Danger);
        _selectedMark.AddThemeFontOverride("font", _fonts.MonoSemibold);
        _selectedMark.AddThemeFontSizeOverride("font_size", scale.Metadata);
        _selectedMark.AddThemeColorOverride("font_color", palette.Accent);
        RefreshSelectedVisuals();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        Selection.SetPressedNoSignal(selected);
        RefreshSelectedVisuals();
    }

    public void SetStacked(bool stacked)
    {
        _layout.Columns = stacked || !_mechanicsPanel.Visible ? 1 : 2;
        _mechanicsPanel.CustomMinimumSize = !stacked && _mechanicsPanel.Visible
            ? new Vector2(250, 0)
            : Vector2.Zero;
    }

    private static T IgnoreMouse<T>(T control)
        where T : Control
    {
        control.MouseFilter = MouseFilterEnum.Ignore;
        return control;
    }

    private void RefreshSelectedVisuals()
    {
        _selectedMark.Visible = _selected;
        Color keyBackground = _selected ? _palette.Accent : _palette.Panel;
        Color keyText = _selected ? _palette.Background : _palette.Accent;
        Color mechanicsBackground = _selected
            ? UiThemeFactory.Mix(_palette.Panel, _palette.Accent, 0.10f)
            : _palette.Panel;
        _keyPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(keyBackground, _palette.Accent, _scale, _scale.Space1));
        _key.AddThemeColorOverride(
            "font_color",
            Choice.Enabled ? keyText : _palette.Muted);
        _mechanicsPanel.AddThemeStyleboxOverride(
            "panel",
            UiThemeFactory.BorderBox(
                mechanicsBackground,
                _selected ? _palette.Accent : _palette.Muted,
                _scale,
                _scale.Space2));
    }
}
