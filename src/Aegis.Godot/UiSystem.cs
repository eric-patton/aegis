using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal readonly record struct UiScaleTokens(
    float Scale,
    int Metadata,
    int Body,
    int Control,
    int Heading,
    int Display,
    int Space1,
    int Space2,
    int Space3,
    int Space4,
    int Radius)
{
    public static UiScaleTokens FromIndex(int index)
    {
        float scale = index switch
        {
            1 => 1.25f,
            2 => 1.5f,
            3 => 1.75f,
            4 => 2f,
            _ => 1f,
        };
        int Px(float value) => Math.Max(1, (int)MathF.Round(value * scale));
        return new UiScaleTokens(
            scale,
            Px(18),
            Px(20),
            Px(18),
            Px(28),
            Px(36),
            Px(6),
            Px(12),
            Px(20),
            Px(28),
            Px(3));
    }

    public int Percent => (int)MathF.Round(Scale * 100);
}

internal sealed class ClientFonts
{
    public Font Body { get; } = GD.Load<Font>("res://assets/fonts/AtkinsonNext-Regular.ttf");
    public Font BodySemibold { get; } = GD.Load<Font>("res://assets/fonts/AtkinsonNext-SemiBold.ttf");
    public Font Mono { get; } = GD.Load<Font>("res://assets/fonts/AtkinsonMono-Regular.ttf");
    public Font MonoSemibold { get; } = GD.Load<Font>("res://assets/fonts/AtkinsonMono-SemiBold.ttf");
    public Font Prose { get; } = GD.Load<Font>("res://assets/fonts/Literata.ttf");
}

internal readonly record struct UiPalette(
    Color Background,
    Color Panel,
    Color Raised,
    Color Text,
    Color Muted,
    Color Accent,
    Color Warm,
    Color Danger,
    Color Health,
    Color Stamina,
    Color Field,
    Color Combat,
    Color Words)
{
    public static UiPalette Dark => new(
        Color.FromHtml("#0E1518"),
        Color.FromHtml("#151E22"),
        Color.FromHtml("#1D2A30"),
        Color.FromHtml("#F1EBDD"),
        Color.FromHtml("#B3BDBC"),
        Color.FromHtml("#78CED0"),
        Color.FromHtml("#E2A54D"),
        Color.FromHtml("#E06F68"),
        Color.FromHtml("#D95F5F"),
        Color.FromHtml("#65B86E"),
        Color.FromHtml("#B3BDBC"),
        Color.FromHtml("#E88A72"),
        Color.FromHtml("#78CED0"));

    public static UiPalette Light => new(
        Color.FromHtml("#F3EEE4"),
        Color.FromHtml("#E8E0D3"),
        Color.FromHtml("#FFFFFF"),
        Color.FromHtml("#182326"),
        Color.FromHtml("#536166"),
        Color.FromHtml("#14676E"),
        Color.FromHtml("#8A5B16"),
        Color.FromHtml("#9F3434"),
        Color.FromHtml("#A73535"),
        Color.FromHtml("#2F7A43"),
        Color.FromHtml("#536166"),
        Color.FromHtml("#A13B34"),
        Color.FromHtml("#14676E"));

    public Color MapColor(Hue hue, bool light) =>
        light
            ? hue switch
            {
                Hue.Black => Background,
                Hue.DarkBlue => Color.FromHtml("#45658D"),
                Hue.DarkGreen => Color.FromHtml("#48764F"),
                Hue.DarkCyan => Color.FromHtml("#3E7478"),
                Hue.DarkRed => Danger,
                Hue.DarkMagenta => Color.FromHtml("#755C83"),
                Hue.DarkYellow => Warm,
                Hue.Gray => Color.FromHtml("#4B5557"),
                Hue.DarkGray => Muted,
                Hue.Blue => Color.FromHtml("#315D98"),
                Hue.Green => Color.FromHtml("#326D3E"),
                Hue.Cyan => Accent,
                Hue.Red => Color.FromHtml("#A33236"),
                Hue.Magenta => Color.FromHtml("#6B4A78"),
                Hue.Yellow => Color.FromHtml("#825516"),
                Hue.White => Text,
                _ => Text,
            }
            : Resolve(AegisPalette.Resolve(hue));

    private static Color Resolve(Rgb24 value) => Color.Color8(value.R, value.G, value.B);
}

internal static class UiThemeFactory
{
    public static Theme Build(ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        var theme = new Theme
        {
            DefaultFont = fonts.Body,
            DefaultFontSize = scale.Body,
        };

        theme.SetFont("font", "Label", fonts.Body);
        theme.SetFontSize("font_size", "Label", scale.Body);
        theme.SetColor("font_color", "Label", palette.Text);

        theme.SetFont("font", "Button", fonts.BodySemibold);
        theme.SetFontSize("font_size", "Button", scale.Control);
        theme.SetColor("font_color", "Button", palette.Text);
        theme.SetColor("font_hover_color", "Button", palette.Text);
        theme.SetColor("font_pressed_color", "Button", palette.Background);
        theme.SetColor("font_focus_color", "Button", palette.Text);
        theme.SetColor("font_disabled_color", "Button", palette.Muted);
        theme.SetStylebox("normal", "Button", Box(palette.Raised, scale, scale.Space2));
        theme.SetStylebox("hover", "Button", Box(Mix(palette.Raised, palette.Accent, 0.16f), scale, scale.Space2));
        theme.SetStylebox("pressed", "Button", Box(palette.Warm, scale, scale.Space2));
        theme.SetStylebox("disabled", "Button", Box(WithAlpha(palette.Panel, 0.55f), scale, scale.Space2));
        theme.SetStylebox("focus", "Button", FocusBox(palette.Accent, scale));

        theme.SetFont("font", "LineEdit", fonts.Body);
        theme.SetFontSize("font_size", "LineEdit", scale.Heading);
        theme.SetColor("font_color", "LineEdit", palette.Text);
        theme.SetColor("caret_color", "LineEdit", palette.Accent);
        theme.SetColor("selection_color", "LineEdit", WithAlpha(palette.Accent, 0.35f));
        theme.SetStylebox("normal", "LineEdit", FieldBox(palette.Background, palette.Muted, scale));
        theme.SetStylebox("focus", "LineEdit", FieldBox(palette.Background, palette.Accent, scale));

        theme.SetFont("normal_font", "RichTextLabel", fonts.Prose);
        theme.SetFont("bold_font", "RichTextLabel", fonts.BodySemibold);
        theme.SetFont("mono_font", "RichTextLabel", fonts.Mono);
        theme.SetFontSize("normal_font_size", "RichTextLabel", scale.Body);
        theme.SetFontSize("bold_font_size", "RichTextLabel", scale.Body);
        theme.SetFontSize("mono_font_size", "RichTextLabel", scale.Body);
        theme.SetColor("default_color", "RichTextLabel", palette.Text);

        theme.SetStylebox("panel", "PanelContainer", Box(palette.Panel, scale, 0));
        theme.SetStylebox("panel", "PopupPanel", BorderBox(palette.Raised, palette.Accent, scale, scale.Space2));
        theme.SetStylebox("background", "ProgressBar", Box(palette.Panel, scale, 0));
        theme.SetStylebox("fill", "ProgressBar", Box(palette.Accent, scale, 0));
        theme.SetColor("font_color", "ProgressBar", palette.Text);
        return theme;
    }

    public static void Mark(Label label, string role, ClientFonts fonts, UiScaleTokens scale, UiPalette palette)
    {
        switch (role)
        {
            case "display":
                label.AddThemeFontOverride("font", fonts.MonoSemibold);
                label.AddThemeFontSizeOverride("font_size", scale.Display);
                label.AddThemeColorOverride("font_color", palette.Accent);
                break;
            case "heading":
                label.AddThemeFontOverride("font", fonts.BodySemibold);
                label.AddThemeFontSizeOverride("font_size", scale.Heading);
                break;
            case "eyebrow":
                label.AddThemeFontOverride("font", fonts.MonoSemibold);
                label.AddThemeFontSizeOverride("font_size", scale.Metadata);
                label.AddThemeColorOverride("font_color", palette.Accent);
                break;
            case "muted":
                label.AddThemeFontSizeOverride("font_size", scale.Metadata);
                label.AddThemeColorOverride("font_color", palette.Muted);
                break;
        }
    }

    public static StyleBoxFlat BorderBox(
        Color background,
        Color border,
        UiScaleTokens scale,
        int contentMargin)
    {
        StyleBoxFlat box = Box(background, scale, contentMargin);
        box.SetBorderWidthAll(Math.Max(1, (int)MathF.Round(scale.Scale)));
        box.BorderColor = border;
        return box;
    }

    private static StyleBoxFlat FieldBox(Color background, Color border, UiScaleTokens scale)
    {
        StyleBoxFlat box = BorderBox(background, border, scale, scale.Space2);
        box.BorderWidthBottom = Math.Max(2, (int)MathF.Round(2 * scale.Scale));
        return box;
    }

    private static StyleBoxFlat FocusBox(Color border, UiScaleTokens scale)
    {
        var box = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = border };
        box.SetBorderWidthAll(Math.Max(2, (int)MathF.Round(2 * scale.Scale)));
        box.SetCornerRadiusAll(scale.Radius);
        box.SetExpandMarginAll(Math.Max(1, (int)MathF.Round(scale.Scale)));
        return box;
    }

    private static StyleBoxFlat Box(Color color, UiScaleTokens scale, int contentMargin)
    {
        var box = new StyleBoxFlat { BgColor = color };
        box.SetCornerRadiusAll(scale.Radius);
        box.SetContentMarginAll(contentMargin);
        return box;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = alpha;
        return color;
    }

    private static Color Mix(Color first, Color second, float amount) => new(
        Mathf.Lerp(first.R, second.R, amount),
        Mathf.Lerp(first.G, second.G, amount),
        Mathf.Lerp(first.B, second.B, amount),
        Mathf.Lerp(first.A, second.A, amount));
}
