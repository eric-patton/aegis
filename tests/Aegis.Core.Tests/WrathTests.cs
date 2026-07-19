using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The raiders' wrath (D-078, the second faction and the enemy ledger): D-023's
/// dual reputation grows its Infamy-shaped half. Wrath rises one notch per raider
/// slain, on its own faster ladder (1, 2, 4: hate compounds where gratitude
/// steps), resets at every crossing like the regard it mirrors, and past the
/// dread rung it grows teeth the bearer can feel: raiders' blows land one point
/// the weaker. The two ledgers now share a keyed store, the start of D-023's
/// per-faction bookkeeping.
/// </summary>
public class WrathTests
{
    [Fact]
    public void TheLadder_CompoundsWhereRegardSteps()
    {
        Assert.Equal(1, RaiderWrath.Threshold(1));
        Assert.Equal(2, RaiderWrath.Threshold(2));
        Assert.Equal(4, RaiderWrath.Threshold(3));

        Assert.Equal(0, RaiderWrath.RungFor(0));
        Assert.Equal(1, RaiderWrath.RungFor(1));
        Assert.Equal(2, RaiderWrath.RungFor(2));
        Assert.Equal(2, RaiderWrath.RungFor(3));
        Assert.Equal(3, RaiderWrath.RungFor(4));
        Assert.Equal(3, RaiderWrath.RungFor(100)); // the cap holds

        Assert.Equal("", RaiderWrath.TitleOf(0));
        Assert.Equal("a name the raiders curse", RaiderWrath.TitleOf(1));
        Assert.Equal("a dread on the raiders", RaiderWrath.TitleOf(2));
        Assert.Equal("the bane of the dens", RaiderWrath.TitleOf(4));
    }

    [Fact]
    public void TheDread_StaysTheHand_OnlyPastItsRung_AndNeverBelowOne()
    {
        // Below the dread rung the roll stands as thrown.
        Assert.Equal(5, RaiderWrath.Steadied(0, 5));
        Assert.Equal(5, RaiderWrath.Steadied(1, 5));
        // At and past the dread rung the blow lands one the weaker.
        Assert.Equal(4, RaiderWrath.Steadied(2, 5));
        Assert.Equal(4, RaiderWrath.Steadied(4, 5));
        // Never below one: a landed blow still lands.
        Assert.Equal(1, RaiderWrath.Steadied(2, 1));
    }

    [Fact]
    public void SlayingARaider_RaisesWrath_AndTheAegisSpeaksOnce()
    {
        var game = ArrangeCamp(11);
        Assert.Equal(0, game.Wrath);
        Assert.False(game.Player.WrathLineHeard);

        SlayNext(game);

        Assert.Equal(1, game.Wrath);
        var log = game.Log.Recent(8).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("The raiders have a name for you now")); // the crossed rung speaks
        Assert.Contains(log, t => t.Contains("hate is also a kind of regard"));       // the once-only Aegis aside
        Assert.True(game.Player.WrathLineHeard);
    }

    [Fact]
    public void TheSecondSlaying_CrossesIntoDread_AndTheAsideDoesNotReturn()
    {
        var game = ArrangeCamp(11);
        SlayNext(game);
        SlayNext(game);

        Assert.Equal(2, game.Wrath);
        Assert.Equal(RaiderWrath.DreadRung, RaiderWrath.RungFor(game.Wrath));
        var log = game.Log.Recent(8).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("Dread has entered the raiders' work"));
        // The aside was spent on the first slaying; the whole log holds it once.
        Assert.Single(game.Log.Entries, e => e.Text.Contains("hate is also a kind of regard"));
    }

    [Fact]
    public void ABlowToOne_IsAFavorToTheOther()
    {
        // The oldest relationship on the ledger: emptying the camp raises the
        // stead's regard and the raiders' wrath in the same strokes.
        var game = ArrangeCamp(11);
        while (game.Monsters.Any(m => m.Alive && m.Kind == MonsterKind.Goblin))
            SlayNext(game);

        Assert.True(game.CampCleared);
        Assert.Equal(3, game.Regard);      // the stead's thanks (D-076)
        Assert.Equal(game.Monsters.Count(m => m.Kind == MonsterKind.Goblin), game.Wrath); // one notch per raider
    }

    [Fact]
    public void TheSnapshot_CarriesWrath_AndTitle()
    {
        var game = ArrangeCamp(11);
        var bare = game.TakeSnapshot();
        Assert.Equal(0, bare.Wrath);
        Assert.Equal("", bare.WrathTitle);

        SlayNext(game);
        var snap = game.TakeSnapshot();
        Assert.Equal(1, snap.Wrath);
        Assert.Equal("a name the raiders curse", snap.WrathTitle);
    }

    [Fact]
    public void TheWrath_ResetsAtEachCrossing()
    {
        var game = ArrangeCamp(42);
        while (game.Monsters.Any(m => m.Alive && m.Kind == MonsterKind.Goblin))
            SlayNext(game);
        Assert.True(game.Wrath > 0);

        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The next world's dens have not met the bearer: the wrath stays behind
        // with the dead who earned it.
        Assert.Equal(0, game.Wrath);
    }

    [Fact]
    public void ADebugClearedCamp_MovesNoWrath()
    {
        // Debug_ClearCamp zeroes tenants without a slaying: no raider fell to the
        // bearer's hand, so the dens have nothing to count. (This is also why
        // every pre-D-078 regard test still reads a wrathless world.)
        var game = new Game(42);
        game.Debug_ClearCamp();
        Assert.Equal(3, game.Regard);
        Assert.Equal(0, game.Wrath);
    }

    /// <summary>Enters the camp and leaves every goblin standing, ready to be slain one by one.</summary>
    private static Game ArrangeCamp(ulong seed)
    {
        var game = new Game(seed);
        game.Debug_SetMode(MapMode.Site);
        return game;
    }

    /// <summary>
    /// Slays the next living goblin through the real kill path: dropped to one
    /// hit point, the bearer set beside it, and the blow struck as a bump.
    /// </summary>
    private static void SlayNext(Game game)
    {
        var goblin = game.Monsters.First(m => m.Alive && m.Kind == MonsterKind.Goblin);
        goblin.Hp = 1;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = goblin.Pos.Plus(dx, dy);
            if (game.World.Camp.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
            {
                game.Debug_SetPlayerPos(p);
                game.Apply(DirToCommand(-dx, -dy)); // bump back toward the goblin
                if (!goblin.Alive) return;
            }
        }
        throw new InvalidOperationException("could not slay the goblin (seed choice)");
    }

    private static Command DirToCommand(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => Command.MoveN,
        (0, 1) => Command.MoveS,
        (-1, 0) => Command.MoveW,
        (1, 0) => Command.MoveE,
        (-1, -1) => Command.MoveNW,
        (1, -1) => Command.MoveNE,
        (-1, 1) => Command.MoveSW,
        (1, 1) => Command.MoveSE,
        _ => Command.None,
    };
}
