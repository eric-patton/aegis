using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The raids are real (D-079): the first coarse-tick faction event, D-023's
/// living-world half begun. While the camp stands the raiders come down on the
/// stead every tick of turns, each raid narrated as it lands, written as a fact,
/// and pricing bread a coin dearer for the rest of the world. Clearing the camp
/// is the designed exit condition; the grain already taken does not come back
/// before the crossing.
/// </summary>
public class RaidsTests
{
    [Fact]
    public void TheTick_BringsARaid_Perceivably_AndWritesTheFact()
    {
        var game = new Game(42);
        int priceBefore = game.RationPrice;
        Assert.Equal(0, game.Raids);

        Wait(game, SteadRaids.TickTurns);

        Assert.Equal(1, game.Raids);
        Assert.Equal(priceBefore + 1, game.RationPrice); // the stores thinned
        Assert.True(game.World.Facts.Exists("event", "raid")); // the world remembers
        var log = game.Log.Recent(6).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("the raiders come down on")); // the raid is named
        Assert.Contains(log, t => t.Contains("Bread will be dearer"));     // the cost is named
    }

    [Fact]
    public void TheRaids_StopAtTheCap()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns * (SteadRaids.Cap + 2));

        // The stead has only so much to lose.
        Assert.Equal(SteadRaids.Cap, game.Raids);
    }

    [Fact]
    public void ClearingTheCamp_IsTheExitCondition()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();

        Wait(game, SteadRaids.TickTurns * 2);

        Assert.Equal(0, game.Raids); // no camp, no raids
        Assert.False(game.World.Facts.Exists("event", "raid"));
    }

    [Fact]
    public void TheGrainTaken_DoesNotComeBack_UntilTheCrossing()
    {
        var game = new Game(42);
        int priceBefore = game.RationPrice;
        Wait(game, SteadRaids.TickTurns); // one raid lands
        game.Debug_ClearCamp();           // the raids end, but the grain is gone

        // The raid's +1 still stands; the friend's price (D-080) the camp-clear
        // just earned takes its own coin off beside it, two ledgers side by side.
        Assert.Equal(priceBefore + 1 - 1, game.RationPrice);
        Assert.Equal(1, game.Raids);
        Wait(game, SteadRaids.TickTurns); // and no further raid comes
        Assert.Equal(1, game.Raids);

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // A fresh world's stores stand whole.
        Assert.Equal(0, game.Raids);
        Assert.Equal(0, game.TakeSnapshot().Raids);
    }

    [Fact]
    public void ADenUnderAttack_DefendsItsOwn()
    {
        // While the bearer stands inside the camp the tick passes it by: the
        // raiders are pinned at their own door. The raid comes once the bearer
        // walks out and leaves them their nights again.
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        foreach (var m in game.Monsters) m.Hp = 0; // quiet the camp without clearing it

        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(0, game.Raids);

        game.Debug_SetMode(MapMode.Overworld);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(1, game.Raids);
    }

    [Fact]
    public void TheNewWorld_CountsItsTickFromArrival()
    {
        // Cross partway through a tick: the next world's raiders count their
        // nights from the bearer's arrival, not from the far side of the arch.
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns / 2);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // Half a tick here would have fired under a global clock; a full tick
        // from arrival is what the new world's dens actually keep.
        Wait(game, SteadRaids.TickTurns / 2);
        Assert.Equal(0, game.Raids);
        Wait(game, SteadRaids.TickTurns / 2);
        Assert.Equal(1, game.Raids);
    }

    [Fact]
    public void TheSteadsTalk_KeepsTheRaidLedger()
    {
        // D-080: the goblin-raids topic sharpens as the raids land, so the world
        // speaks its own state back through the ask-about surface.
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(1, game.Raids);

        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);
        var raidsTopic = game.Topics.First(t => t.Label == "The goblin raids");
        Assert.Contains("since you walked in", raidsTopic.Answer);
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
