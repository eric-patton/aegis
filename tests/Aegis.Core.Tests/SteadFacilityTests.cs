using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The stead's works (D-134, plan 2026-07 A3): the facility ladder's first
/// rung, three coin sinks behind the steadholder's own bench, each funded
/// once per world and each modifying a system that already runs. The
/// palisade blunts every greedy raiding night to a plain one, the watchtower
/// spares the watch its bread, and the granary deepens the lofts by two
/// measures. A funded work pays regard exactly once (D-131's guard), and
/// like every stead thing the works are gone at the crossing.
/// </summary>
public class SteadFacilityTests
{
    [Fact]
    public void ThePalisade_BluntsTheGreedyNight()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck(); // choreographed ticks: the season's own deals stay in the box
        game.Player.Coin = 60;
        int regard = game.Regard;
        Fund(game, "palisade");

        Assert.True(game.PalisadeStands);
        Assert.True(game.World.Facts.Exists("event", "palisade_built"));
        Assert.Equal(60 - SteadFacilities.PalisadeCoin, game.Player.Coin);
        Assert.Equal(regard + 1, game.Regard);
        var snap = game.TakeSnapshot();
        Assert.True(snap.PalisadeStands);

        Wait(game, SteadRaids.TickTurns); // the plain raid is not the palisade's business
        Assert.Equal(SteadStores.Max - 1, game.Stores);

        Wait(game, SteadRaids.TickTurns); // the greedy night meets the timber: one measure, not two
        Assert.Equal(SteadStores.Max - 2, game.Stores);
        Assert.True(game.WatchStands); // the stead still reads the greed and posts its watch
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("sharpened timber where the open fold walls were"));
    }

    [Fact]
    public void TheMusteredNight_MeetsTheTimber()
    {
        var game = WrathTests.ArrangeCamp(42);
        game.Debug_HoldTheDeck();
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game); // the cull sets the muster, two ticks out
        game.Debug_SetMode(MapMode.Overworld);
        game.Player.Coin = 60;
        Fund(game, "palisade");

        Wait(game, SteadRaids.TickTurns * 2); // a cowed tick, then the mustered night

        Assert.Equal(SteadStores.Max - SteadStores.RaidTake, game.Stores); // held to one loft
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("meets the timber"));
    }

    [Fact]
    public void TheWatchtower_SparesTheWatchItsBread()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Player.Coin = 60;
        Fund(game, "watchtower");

        Wait(game, SteadRaids.TickTurns * 2); // a plain raid, then a greedy one posts the watch
        Assert.True(game.WatchStands);
        int stores = game.Stores;
        int raids = game.Raids;

        Wait(game, SteadRaids.TickTurns); // the watch turns the night, and the tower feeds no one

        Assert.Equal(raids, game.Raids);
        Assert.Equal(stores, game.Stores);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("The tower saw them on the hills"));
    }

    [Fact]
    public void TheGranary_DeepensTheLofts()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Player.Coin = 60;
        Fund(game, "granary");
        Assert.Equal(SteadStores.Max + SteadFacilities.GranaryRaise, game.StoresMax);

        game.Debug_ClearCamp();
        Wait(game, SteadRaids.TickTurns * 2); // the recovery climbs past the old brim

        Assert.Equal(SteadStores.Max + SteadFacilities.GranaryRaise, game.Stores);
        Assert.True(game.World.Facts.Exists("event", "lofts_full"));

        // Seed 42's hard winter (D-132) lands on tick 5 and takes its two
        // measures off the deeper lofts: a buffer, not a levy.
        Wait(game, SteadRaids.TickTurns * 3);
        Assert.Equal(SteadStores.Max, game.Stores);
        Assert.False(game.LevyStands);
    }

    [Fact]
    public void AWork_IsFundedOnce_AndPaysRegardOnce()
    {
        var game = new Game(42);
        game.Player.Coin = 100;
        Fund(game, "granary");
        int coin = game.Player.Coin;
        int regard = game.Regard;
        Assert.Equal(1, regard);

        Fund(game, "granary"); // the stead does not sell a thing twice

        Assert.Equal(coin, game.Player.Coin);
        Assert.Equal(regard, game.Regard);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("does not sell a thing twice"));
    }

    [Fact]
    public void ShortCoin_RaisesNothing()
    {
        var game = new Game(42);
        game.Player.Coin = SteadFacilities.GranaryCoin - 1;
        Fund(game, "granary");

        Assert.False(game.GranaryStands);
        Assert.Equal(SteadFacilities.GranaryCoin - 1, game.Player.Coin);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("does not build on promises"));
    }

    [Fact]
    public void TheCrossing_TakesTheWalls()
    {
        var game = new Game(42);
        game.Player.Coin = 100;
        Fund(game, "palisade");
        Fund(game, "granary");
        Assert.True(game.PalisadeStands);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The works were this world's alone: the next valley starts bare.
        Assert.False(game.PalisadeStands);
        Assert.False(game.GranaryStands);
        Assert.Equal(SteadStores.Max, game.StoresMax);
    }

    /// <summary>Walks the real key surface: the steadholder's bench opened from talk, the work's own digit pressed, the bench left.</summary>
    private static void Fund(Game game, string work)
    {
        var holder = game.World.Npcs.First(n => n.Id == "npc_steadholder");
        NpcTests.BumpNpc(game, holder);
        int bench = game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Facility || o.Label.Contains("stead's works"));
        game.ApplyKey((char)('1' + game.Topics.Count + bench));
        Assert.True(game.InTradeMenu);
        int digit = game.TradeOffers.ToList().FindIndex(o => o.Arg == work);
        game.ApplyKey((char)('1' + digit));
        game.ApplyKey('z');
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
