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
    public void TheChief_IsToldApart_OnTheMap()
    {
        // The roster follow-on (D-110, delivered D-113): the named leader is
        // drawn capital among its lowercase raiders, same kind, same colors.
        var game = new Game(1);
        game.Debug_SetPlayerPos(game.World.CampSite.OverworldPos);
        game.Apply(Command.Enter);

        // Stand the chief beside the bearer so both are surely in frame.
        var chief = game.Monsters.Single(m => m.Chief);
        chief.Pos = OpenBeside(game, game.Player.Pos);

        var lines = Presenter.Render(game, 120, 40).ToTextLines();
        int Count(char c)
        {
            int n = 0;
            for (int y = 1; y < 32; y++)
                foreach (char ch in lines[y].PadRight(120)[..95])
                    if (ch == c) n++;
            return n;
        }
        Assert.Equal(1, Count('G'));
        Assert.True(Count('g') > 0, "the unnamed raiders still draw lowercase");
    }

    private static Pos OpenBeside(Game game, Pos origin)
    {
        var map = game.CurrentMap;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p == origin || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                return p;
            }
        throw new InvalidOperationException("no open cell beside the bearer");
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
