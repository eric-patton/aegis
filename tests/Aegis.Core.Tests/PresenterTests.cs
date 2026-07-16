using Aegis.Core;

namespace Aegis.Core.Tests;

public class PresenterTests
{
    [Theory]
    [InlineData(80, 24)]
    [InlineData(120, 40)]
    [InlineData(200, 60)]
    public void Render_AtAnySaneSize_FillsTheFrame(int width, int height)
    {
        var game = new Game(42);
        var frame = Presenter.Render(game, width, height);

        Assert.Equal(width, frame.Width);
        Assert.Equal(height, frame.Height);

        var lines = frame.ToTextLines();
        Assert.StartsWith(" AEGIS |", lines[0]);
        Assert.Contains("The Bearer", lines[1]);
        Assert.Contains(lines, l => l.Contains('@'));

        // Log separator sits exactly LogLines+1 from the bottom, wherever the bottom is.
        Assert.Matches("^-+$", lines[height - 6]);
    }

    [Theory]
    [InlineData(40, 10)]
    [InlineData(10, 5)]
    [InlineData(1, 1)]
    public void Render_BelowBaseline_CropsWithoutCrashing(int width, int height)
    {
        var game = new Game(42);
        var frame = Presenter.Render(game, width, height);
        Assert.Equal(width, frame.Width);
        Assert.Equal(height, frame.Height);
    }

    [Fact]
    public void WiderFrame_ShowsMoreMap()
    {
        var game = new Game(42);

        int MapCells(int width, int height)
        {
            var lines = Presenter.Render(game, width, height).ToTextLines();
            // Count terrain glyphs in the map band (rows 1 to height-8, left of the sidebar).
            int count = 0;
            for (int y = 1; y < height - 7; y++)
                foreach (char c in lines[y].PadRight(width)[..(width - 25)])
                    if (c is '.' or '&' or '^' or '~' or '#' or '+' or '>')
                        count++;
            return count;
        }

        Assert.True(MapCells(120, 40) > MapCells(80, 24));
    }
}
