using Aegis.Core;

namespace Aegis.Host;

public readonly record struct Rgb24(byte R, byte G, byte B)
{
    public int Packed => (R << 16) | (G << 8) | B;
}

public static class AegisPalette
{
    public static readonly Rgb24 Clear = new(12, 16, 22);

    public static Rgb24 Resolve(Hue hue) => hue switch
    {
        Hue.Black => Clear,
        Hue.DarkBlue => new Rgb24(78, 118, 178),
        Hue.DarkGreen => new Rgb24(78, 148, 104),
        Hue.DarkCyan => new Rgb24(72, 148, 160),
        Hue.DarkRed => new Rgb24(180, 78, 84),
        Hue.DarkMagenta => new Rgb24(154, 92, 176),
        Hue.DarkYellow => new Rgb24(186, 145, 68),
        Hue.Gray => new Rgb24(161, 171, 184),
        Hue.DarkGray => new Rgb24(120, 131, 145),
        Hue.Blue => new Rgb24(115, 157, 230),
        Hue.Green => new Rgb24(127, 209, 127),
        Hue.Cyan => new Rgb24(96, 211, 231),
        Hue.Red => new Rgb24(244, 112, 118),
        Hue.Magenta => new Rgb24(203, 133, 226),
        Hue.Yellow => new Rgb24(244, 198, 96),
        Hue.White => new Rgb24(232, 237, 243),
        _ => new Rgb24(232, 237, 243),
    };
}
