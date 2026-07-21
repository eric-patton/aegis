using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The stead's works (D-134, plan 2026-07 A3): the facility ladder's first
/// rung, three coin sinks behind the steadholder's own bench, each funded
/// once per world and each modifying a system that already runs. The
/// palisade blunts every greedy raiding night to a plain one, the watchtower
/// spares the watch its bread, and the granary deepens the lofts by two
/// measures. The second rung (D-135) adds the stillroom's new wing (a third
/// vial racked) and the smithy bench, the one work whose standing digit is a
/// verb: it files wear off the bearer's own iron and seeds the Smithing
/// craft. A funded work pays regard exactly once (D-131's guard), and like
/// every stead thing the works are gone at the crossing; the craft is the
/// bearer's, and crosses. The crossing split's audit (D-136, plan 2026-07
/// A4) closes the layer: the works, the deeper lofts, and the deeper satchel
/// all reset with the World bucket, and what leaks forward is one story
/// fact, the builder's echo, hushed-gated and never mechanical.
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

    [Fact]
    public void TheStillroomWing_RacksAThirdVial()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Player.Coin = 60;
        game.Player.Herb = 3 * Game.DraughtHerbs;

        Draw(game);
        Draw(game);
        Assert.Equal(2, game.Player.Draughts);
        Draw(game); // the satchel holds two: the pot stays cold
        Assert.Equal(2, game.Player.Draughts);
        Assert.Equal(Game.DraughtHerbs, game.Player.Herb);

        Fund(game, "stillwing");
        Assert.True(game.StillwingStands);
        Assert.Equal(2 + SteadFacilities.StillwingRack, game.DraughtCap);
        var snap = game.TakeSnapshot();
        Assert.True(snap.StillwingStands);

        Draw(game); // the racked third vial
        Assert.Equal(3, game.Player.Draughts);
        Assert.Equal(0, game.Player.Herb);
    }

    [Fact]
    public void TheBench_FilesTheWornIron_AndSeedsTheCraft()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Player.Coin = 60;
        var axe = GearCatalog.Create("woodaxe");
        axe.Wear = 10;
        game.Player.Weapon = axe;
        Fund(game, "smithy");
        Assert.True(game.SmithyStands);
        Assert.True(game.TakeSnapshot().SmithyStands);

        Fund(game, "smithy"); // the standing digit is the bench itself: one sitting
        Assert.Equal(10 - SteadFacilities.BenchBase, axe.Wear);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Smithing));

        Fund(game, "smithy");
        Fund(game, "smithy"); // 6, 2, then true
        Assert.Equal(0, axe.Wear);
        Assert.Equal(3, game.Player.Skills.Uses(SkillId.Smithing));

        Fund(game, "smithy"); // nothing for the file: no use counted
        Assert.Equal(3, game.Player.Skills.Uses(SkillId.Smithing));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("nothing for the file"));
    }

    [Fact]
    public void TheCrossing_TakesTheBench_TheCraftCrosses()
    {
        var game = new Game(42);
        game.Player.Coin = 100;
        var axe = GearCatalog.Create("woodaxe");
        axe.Wear = SteadFacilities.BenchBase;
        game.Player.Weapon = axe;
        Fund(game, "stillwing");
        Fund(game, "smithy");
        Fund(game, "smithy"); // one sitting before the road
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Smithing));

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.False(game.StillwingStands);
        Assert.False(game.SmithyStands);
        Assert.Equal(2, game.DraughtCap);
        // The wing and the bench were the world's; the hands are the bearer's.
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Smithing));
    }

    [Fact]
    public void TheBuildersEcho_CrossesAsFoundingTalk()
    {
        // Master 43, like the echo-ballad test: its second world tells the
        // stead, so the NearHouse pool stays the one this was written against.
        var game = new Game(43);
        game.Player.Coin = 200;
        Fund(game, "palisade");
        Fund(game, "watchtower");
        Fund(game, "granary");
        Fund(game, "stillwing");
        Fund(game, "smithy");
        string settlement = game.World.SettlementName;

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The whole ladder stood, so the echo names the whole ladder; the
        // works themselves stayed behind with the world that held them.
        var echo = game.World.Facts.Find("legacy", "builders_hand");
        Assert.NotNull(echo);
        Assert.Equal(settlement, echo!.Object);
        Assert.Contains("palisade to smithy bench", echo.Detail);
        Assert.False(game.PalisadeStands);
        Assert.Equal(SteadStores.Max, game.StoresMax);
        Assert.Equal(2, game.DraughtCap);

        // The founding talk says it aloud: NearHouse deals one card a visit,
        // so walk up to the doors until the drover's telling comes around.
        bool heard = false;
        for (int i = 0; i < 6 && !heard; i++)
        {
            game.ApplyKey('k');
            if (game.InScene) game.ApplyKey('3');
            heard = game.Log.Recent(50).Any(e => e.Text.Contains("Drovers out of the passes"));
            game.ApplyKey('j');
        }
        Assert.True(heard);
    }

    [Fact]
    public void ABareStead_LeavesNoEcho()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.False(game.World.Facts.Exists("legacy", "builders_hand"));
    }

    [Fact]
    public void TheHushedName_StillsTheBuildersEcho()
    {
        var game = new Game(42);
        game.Player.Coin = 60;
        Fund(game, "granary");

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey(HushedKey());
        game.ApplyKey('>');
        Assert.Equal(2, game.Cycle);

        // The building was the bearer's open deed: a hushed world was never
        // told who paid for the last one's timber.
        Assert.False(game.World.Facts.Exists("legacy", "builders_hand"));
    }

    private static char HushedKey()
    {
        for (int i = 0; i < OathCatalog.All.Count; i++)
            if (OathCatalog.All[i].Id == OathId.HushedName) return (char)('1' + i);
        throw new InvalidOperationException("no hushed name oath");
    }

    /// <summary>Draws a draught at the herbwife's stillroom through the real key surface.</summary>
    private static void Draw(Game game)
    {
        var wife = game.World.Npcs.First(n => n.Id == "npc_herbwife");
        NpcTests.BumpNpc(game, wife);
        int trade = game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Trade);
        game.ApplyKey((char)('1' + game.Topics.Count + trade));
        Assert.True(game.InTradeMenu);
        int digit = game.TradeOffers.ToList().FindIndex(o => o.Good == TradeGood.Draught);
        game.ApplyKey((char)('1' + digit));
        game.ApplyKey('z');
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
