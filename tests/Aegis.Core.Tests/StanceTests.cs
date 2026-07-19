using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The footing (D-094): 'x' cycles measured, pressing, guarded. Free on quiet
/// ground; under live steel it is an act the field can answer and costs the
/// turn (D-004's commitment). The trades are flat 2s, floored at the honest 1.
/// </summary>
public class StanceTests
{
    [Fact]
    public void TheFeet_CycleFreely_OnQuietGround()
    {
        var game = new Game(42);
        int turn = game.Turn;
        game.ApplyKey('x');
        Assert.Equal(Stance.Pressing, game.Player.Stance);
        game.ApplyKey('x');
        Assert.Equal(Stance.Guarded, game.Player.Stance);
        game.ApplyKey('x');
        Assert.Equal(Stance.Measured, game.Player.Stance);
        Assert.Equal(turn, game.Turn); // the overworld asks nothing for it
    }

    [Fact]
    public void TheShift_CostsTheTurn_UnderLiveSteel()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        int turn = game.Turn;
        game.ApplyKey('x');
        Assert.Equal(Stance.Pressing, game.Player.Stance);
        Assert.Equal(turn + 1, game.Turn); // the camp's living make it an act
    }

    [Fact]
    public void ThePressingBlow_LandsExactlyTwoHarder()
    {
        int measured = FirstBlow(stanceKeys: 0);
        int pressing = FirstBlow(stanceKeys: 1);
        int guarded = FirstBlow(stanceKeys: 2);
        Assert.Equal(measured + 2, pressing);
        Assert.Equal(Math.Max(1, measured - 2), guarded);
    }

    /// <summary>
    /// Same seed, same goblin, same dice: only the footing differs. The stance
    /// keys are pressed on the overworld, where they spend neither turn nor
    /// dice, so both games meet the fight with identical streams.
    /// </summary>
    private static int FirstBlow(int stanceKeys)
    {
        var game = new Game(42);
        for (int i = 0; i < stanceKeys; i++) game.ApplyKey('x');
        game.Debug_SetMode(MapMode.Site);
        var goblin = game.Monsters.First(m => m.Alive && m.SiteId == "goblin-camp");
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var p = goblin.Pos.Plus(dx, dy);
            if (game.World.Camp.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
            {
                game.Debug_SetPlayerPos(p);
                int before = goblin.Hp;
                game.ApplyKey(DirKey(-dx, -dy));
                return before - goblin.Hp;
            }
        }
        throw new InvalidOperationException("no goblin with an open flank");
    }

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (0, -1) => 'k',
        (0, 1) => 'j',
        _ => 'n',
    };
}
