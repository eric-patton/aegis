using Aegis.Core;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed partial class MapGridControl : Control
{
    private Frame? _frame;
    private Font _font;
    private UiPalette _palette;
    private bool _lightTheme;
    private int _zoomIndex;

    public MapGridControl(Font font, UiPalette palette, bool lightTheme)
    {
        _font = font;
        _palette = palette;
        _lightTheme = lightTheme;
        ClipContents = true;
        FocusMode = FocusModeEnum.Click;
        MouseFilter = MouseFilterEnum.Stop;
        Resized += QueueRedraw;
        GuiInput += OnMapInput;
    }

    public void UpdateFrame(
        Frame frame,
        Font font,
        UiPalette palette,
        bool lightTheme,
        int zoomIndex)
    {
        _frame = frame;
        _font = font;
        _palette = palette;
        _lightTheme = lightTheme;
        _zoomIndex = MapZoom.ClampIndex(zoomIndex);
        QueueRedraw();
    }

    public void SetZoom(int zoomIndex)
    {
        _zoomIndex = MapZoom.ClampIndex(zoomIndex);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_frame is null || Size.X < 1 || Size.Y < 1)
            return;

        int sidebarWidth = _frame.Width >= 110 ? 31 : 23;
        int columns = Math.Max(1, _frame.Width - sidebarWidth - 2);
        int rows = Math.Max(1, _frame.Height - 7);
        SquareCellLayout layout = SquareCellLayout.Fit(
            (int)MathF.Floor(Size.X),
            (int)MathF.Floor(Size.Y),
            columns,
            rows,
            1,
            48);
        int cellSize = Math.Clamp(
            (int)MathF.Round(layout.CellSize * MapZoom.Factor(_zoomIndex)),
            1,
            64);
        int originX = (int)MathF.Round((Size.X - columns * cellSize) / 2f);
        int originY = (int)MathF.Round((Size.Y - rows * cellSize) / 2f);
        int fontSize = Math.Max(1, cellSize - Math.Max(1, cellSize / 9));
        float fontHeight = _font.GetHeight(fontSize);
        float ascent = _font.GetAscent(fontSize);

        for (int row = 0; row < rows; row++)
        {
            int sourceY = row + 1;
            int y = originY + row * cellSize;
            for (int column = 0; column < columns; column++)
            {
                int x = originX + column * cellSize;
                Cell cell = _frame[column, sourceY];
                Color background = _palette.MapColor(cell.Bg, _lightTheme);
                DrawRect(
                    new Rect2(x, y, cellSize, cellSize),
                    background,
                    filled: true);
                if (cell.Ch == ' ')
                    continue;

                float baseline = MathF.Round(y + (cellSize - fontHeight) / 2f + ascent);
                DrawString(
                    _font,
                    new Vector2(x, baseline),
                    cell.Ch.ToString(),
                    HorizontalAlignment.Center,
                    cellSize,
                    fontSize,
                    _palette.MapColor(cell.Fg, _lightTheme));
            }
        }
    }

    private void OnMapInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            GrabFocus();
            AcceptEvent();
        }
    }
}
