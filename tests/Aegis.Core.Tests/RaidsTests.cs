using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The raids are real (D-079) and the factions have state (D-089): while the
/// camp stands the raiders come down on the coarse tick, each raid narrated as
/// it lands and written as a fact; what a raid takes rides the dens' boldness
/// (plunder emboldens, dead raiders cow), bread's price rides the stead's
/// stores, bared lofts are the raids' own dark exit, and a stead whose camp
/// has fallen recovers a measure per tick until the lofts stand full.
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
    public void TheDens_Embolden_AsThePlunderGoesUnanswered()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(SteadStores.Max - 1, game.Stores); // the first raid takes a measure

        // A night of unanswered plunder emboldens: the second raid comes greedy.
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(SteadStores.Max - 3, game.Stores); // and carries off double
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("they come greedy now"));
    }

    [Fact]
    public void TheRaids_EndWhenTheLoftsBareOut()
    {
        // Since D-105 the stead is not passive on the way down: the second
        // raid comes greedy and posts the watch, so the later nights are
        // turned away, and it is the watch's own upkeep that walks the lofts
        // to the boards. The dark exit still closes the tick, by the stead's
        // own move now rather than the raiders' last ride.
        var game = new Game(42);
        int priceBefore = game.RationPrice;
        Wait(game, SteadRaids.TickTurns * 6);

        Assert.Equal(2, game.Raids); // one plain, one greedy; the rest turned
        Assert.Equal(0, game.Stores);
        Assert.Equal(priceBefore + 3, game.RationPrice);
        Assert.True(game.World.Facts.Exists("event", "lofts_bare"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("eaten the stead bare"));
    }

    [Fact]
    public void TheCull_CowsTheDens()
    {
        // Two raiders slain (the dread rung, D-078) drop the dens below the
        // raiding line: wrath's first faction-scale consequence, named once.
        var game = WrathTests.ArrangeCamp(42);
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game);
        Assert.Equal(2, game.Wrath);
        game.Debug_SetMode(MapMode.Overworld);

        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(0, game.Raids);
        Assert.Equal(SteadStores.Max, game.Stores);
        Assert.True(game.World.Facts.Exists("event", "dens_cowed"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("no torch shows on the hills"));

        // The quiet holds, and it is named only the once.
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(0, game.Raids);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("no torch shows on the hills"));
    }

    [Fact]
    public void TheStores_Recover_OnceTheCampFalls()
    {
        var game = new Game(42);
        int priceBefore = game.RationPrice;
        Wait(game, SteadRaids.TickTurns); // one raid lands
        Assert.Equal(priceBefore + 1, game.RationPrice);

        game.Debug_ClearCamp(); // the raids end; the grain is still gone
        Assert.Equal(priceBefore + 1 - 1, game.RationPrice); // raid's coin beside the friend's price

        // The next tick makes the season good: lofts full, the easing narrated.
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(SteadStores.Max, game.Stores);
        Assert.Equal(priceBefore - 1, game.RationPrice); // only the friend's price remains
        Assert.True(game.World.Facts.Exists("event", "lofts_full"));
        var log = game.Log.Recent(4).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("bread comes a coin back down"));
        Assert.Contains(log, t => t.Contains("lofts stand full again"));
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
    public void TheCrossing_StandsTheNewWorldWhole()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns); // one raid lands
        Assert.Equal(SteadStores.Max - 1, game.Stores);
        game.Debug_ClearCamp();

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // A fresh world's lofts stand full, its dens at their base nerve.
        Assert.Equal(0, game.Raids);
        Assert.Equal(SteadStores.Max, game.Stores);
        Assert.Equal(RaiderBoldness.Base, game.Boldness);
        var snap = game.TakeSnapshot();
        Assert.Equal(0, snap.Raids);
        Assert.Equal(SteadStores.Max, snap.Stores);
        Assert.Equal(RaiderBoldness.Base, snap.Boldness);
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
