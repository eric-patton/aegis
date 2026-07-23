using System.Text;
using Aegis.Core;
using Aegis.Host;

namespace Aegis.Cli;

/// <summary>
/// Diff-renders frames to the terminal with VT escape sequences (alt screen buffer,
/// 16-color SGR). Windows 11 Terminal and every modern *nix terminal speak this.
/// </summary>
public sealed class ConsoleRenderer : IFrameSink, IDisposable
{
    private Frame? _last;
    private readonly StringBuilder _sb = new(16 * 1024);

    public ConsoleRenderer()
    {
        Console.OutputEncoding = Encoding.UTF8;
        // Enter alt buffer, hide cursor, clear.
        Console.Write("\x1b[?1049h\x1b[?25l\x1b[2J");
    }

    /// <summary>Current console size, floored at 1x1 (the presenter handles small-window cropping).</summary>
    public (int Width, int Height) CurrentSize
    {
        get
        {
            try
            {
                return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight));
            }
            catch (IOException)
            {
                return (Presenter.DefaultWidth, Presenter.DefaultHeight);
            }
        }
    }

    public void Draw(Frame frame)
    {
        _sb.Clear();
        bool full = _last is null || _last.Width != frame.Width || _last.Height != frame.Height;
        if (full) _sb.Append("\x1b[2J");

        for (int y = 0; y < frame.Height; y++)
        {
            int runStart = -1;
            for (int x = 0; x <= frame.Width; x++)
            {
                bool changed = x < frame.Width && (full || _last![x, y] != frame[x, y]);
                if (changed && runStart < 0) runStart = x;
                if (!changed && runStart >= 0)
                {
                    EmitRun(frame, runStart, x, y);
                    runStart = -1;
                }
            }
        }

        _sb.Append("\x1b[0m");
        Console.Write(_sb.ToString());
        _last = frame;
    }

    private void EmitRun(Frame frame, int fromX, int toX, int y)
    {
        _sb.Append($"\x1b[{y + 1};{fromX + 1}H");
        Hue? fg = null, bg = null;
        for (int x = fromX; x < toX; x++)
        {
            var cell = frame[x, y];
            if (cell.Fg != fg || cell.Bg != bg)
            {
                fg = cell.Fg;
                bg = cell.Bg;
                _sb.Append($"\x1b[{FgCode(cell.Fg)};{BgCode(cell.Bg)}m");
            }
            _sb.Append(cell.Ch);
        }
    }

    private static int FgCode(Hue hue) => Code(hue, 30, 90);
    private static int BgCode(Hue hue) => Code(hue, 40, 100);

    private static int Code(Hue hue, int normalBase, int brightBase) => hue switch
    {
        Hue.Black => normalBase + 0,
        Hue.DarkRed => normalBase + 1,
        Hue.DarkGreen => normalBase + 2,
        Hue.DarkYellow => normalBase + 3,
        Hue.DarkBlue => normalBase + 4,
        Hue.DarkMagenta => normalBase + 5,
        Hue.DarkCyan => normalBase + 6,
        Hue.Gray => normalBase + 7,
        Hue.DarkGray => brightBase + 0,
        Hue.Red => brightBase + 1,
        Hue.Green => brightBase + 2,
        Hue.Yellow => brightBase + 3,
        Hue.Blue => brightBase + 4,
        Hue.Magenta => brightBase + 5,
        Hue.Cyan => brightBase + 6,
        Hue.White => brightBase + 7,
        _ => normalBase + 7,
    };

    public void Dispose()
    {
        // Leave alt buffer, show cursor, reset attributes.
        Console.Write("\x1b[0m\x1b[?25h\x1b[?1049l");
    }
}
