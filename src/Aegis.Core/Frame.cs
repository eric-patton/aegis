namespace Aegis.Core;

/// <summary>16-color palette, deliberately console-shaped but console-independent.</summary>
public enum Hue : byte
{
    Black, DarkBlue, DarkGreen, DarkCyan, DarkRed, DarkMagenta, DarkYellow, Gray,
    DarkGray, Blue, Green, Cyan, Red, Magenta, Yellow, White,
}

public readonly record struct Cell(char Ch, Hue Fg, Hue Bg)
{
    public static readonly Cell Blank = new(' ', Hue.Gray, Hue.Black);
}

/// <summary>
/// An in-memory screen. Every frontend consumes this: the console renderer diffs it
/// to the terminal, the pilot serves it as text, tests assert against it. The game
/// never draws to a window; it draws here.
/// </summary>
public sealed class Frame
{
    public int Width { get; }
    public int Height { get; }
    private readonly Cell[] _cells;

    public Frame(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new Cell[width * height];
        Array.Fill(_cells, Cell.Blank);
    }

    public Cell this[int x, int y]
    {
        get => _cells[y * Width + x];
        set => _cells[y * Width + x] = value;
    }

    public void Put(int x, int y, char ch, Hue fg, Hue bg = Hue.Black)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            this[x, y] = new Cell(ch, fg, bg);
    }

    public void Write(int x, int y, string text, Hue fg, Hue bg = Hue.Black)
    {
        for (int i = 0; i < text.Length; i++)
            Put(x + i, y, text[i], fg, bg);
    }

    public string[] ToTextLines()
    {
        var lines = new string[Height];
        var buffer = new char[Width];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                buffer[x] = this[x, y].Ch;
            lines[y] = new string(buffer).TrimEnd();
        }
        return lines;
    }
}
