using Aegis.Core;

namespace Aegis.Host;

public sealed class FrameObservation
{
    public int Width { get; set; }
    public int Height { get; set; }
    public FrameCellObservation[] Cells { get; set; } = [];

    public static FrameObservation From(Frame frame)
    {
        var cells = new FrameCellObservation[frame.Width * frame.Height];
        int index = 0;
        for (int y = 0; y < frame.Height; y++)
        {
            for (int x = 0; x < frame.Width; x++)
            {
                Cell cell = frame[x, y];
                cells[index++] = new FrameCellObservation
                {
                    Glyph = cell.Ch,
                    Foreground = AegisPalette.Resolve(cell.Fg).Packed,
                    Background = AegisPalette.Resolve(cell.Bg).Packed,
                };
            }
        }

        return new FrameObservation
        {
            Width = frame.Width,
            Height = frame.Height,
            Cells = cells,
        };
    }
}

public sealed class FrameCellObservation
{
    public int Glyph { get; set; }
    public int Foreground { get; set; }
    public int Background { get; set; }
}
