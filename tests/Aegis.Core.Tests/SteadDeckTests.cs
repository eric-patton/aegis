using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The season deck (D-133, plan 2026-07 A2): the stead's own news beyond the
/// raids' war and the calendar's weather. Four cards to open: the far fields
/// (fortune), the drovers (a trade with two faces), the washout (foreshadowed
/// weather through the D-132 calendar), and the wedding (the calendar's first
/// cancellable promise). Every card moves the stores axis or the calendar,
/// writes a fact, and is narrated; each is dealt once per world.
/// </summary>
public class SteadDeckTests
{
    [Fact]
    public void TheFarFields_ComeGood_AndCanLiftTheLevy()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck(); // directed deals only: the organic deck stays in the box
        game.Debug_SetStores(1);
        game.Debug_DrawSteadEvent("fords_washout"); // the flood bares the lofts and calls the levy
        Wait(game, SteadRaids.TickTurns);
        Assert.True(game.LevyStands);
        int stores = game.Stores;

        game.Debug_DrawSteadEvent("far_fields");

        Assert.Equal(stores + 1, game.Stores);
        Assert.True(game.World.Facts.Exists("event", "far_fields"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("a measure the season had not promised"));
        if (game.Stores >= SteadLevy.LiftedAt) Assert.False(game.LevyStands);
    }

    [Fact]
    public void TheDrovers_TakeAMeasure_AndPriceTheBread()
    {
        var game = new Game(42);
        int price = game.RationPrice;

        game.Debug_DrawSteadEvent("drovers");

        Assert.Equal(SteadStores.Max - 1, game.Stores);
        Assert.True(game.World.Facts.Exists("event", "drovers"));
        Assert.Equal(price + 1, game.RationPrice); // the sold measure prices the board
        var log = game.Log.Recent(4).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("a measure gone at a hill price"));
        Assert.Contains(log, t => t.Contains("The steadholder calls it trade"));
    }

    [Fact]
    public void TheRiver_IsRead_AndTheWashoutClaimsItsNight()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_DrawSteadEvent("fords_washout");

        Assert.True(game.World.Facts.Exists("omen", "fords_washout"));
        Assert.Contains(game.Upcoming, f => f.Key == "fords_washout" && f.DueTick == 1);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("runs brown and full of trees"));

        int raids = game.Raids;
        Wait(game, SteadRaids.TickTurns);

        Assert.True(game.World.Facts.Exists("event", "fords_washout"));
        Assert.Equal(SteadStores.Max - 1, game.Stores); // the flood took one, the raid took none
        Assert.Equal(raids, game.Raids); // the claimed night let no raid ride
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("takes " + game.World.SettlementName + "'s fords"));
    }

    [Fact]
    public void TheWashout_CanBareTheLofts_AndCallTheLevy()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_SetStores(1);
        game.Debug_DrawSteadEvent("fords_washout");

        Wait(game, SteadRaids.TickTurns);

        Assert.Equal(0, game.Stores);
        Assert.True(game.World.Facts.Exists("event", "lofts_bare"));
        Assert.True(game.LevyStands);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("the river has taken"));
    }

    [Fact]
    public void TheBanns_AreRead_AndTheWeddingComesOff_RaidAndAll()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_DrawSteadEvent("wedding");

        Assert.True(game.World.Facts.Exists("omen", "banns_read"));
        Assert.Contains(game.Upcoming, f => f.Key == "wedding" && f.DueTick == 1);

        Wait(game, SteadRaids.TickTurns);

        Assert.True(game.World.Facts.Exists("event", "wedding"));
        // The feast spent one measure and the unclaimed night let the raid
        // ride through it: the dens do not check the banns.
        Assert.Equal(1, game.Raids);
        Assert.Equal(SteadStores.Max - 2, game.Stores);
        var log = game.Log.Recent(8).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("dancing until the tallow burns down"));
        Assert.Contains(log, t => t.Contains("By night the raiders come down"));
    }

    [Fact]
    public void TheLeanLofts_PutTheWeddingOff()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_SetStores(SteadDeck.FeastNeeds - 1);
        game.Debug_DrawSteadEvent("wedding");

        Wait(game, SteadRaids.TickTurns);

        Assert.False(game.World.Facts.Exists("event", "wedding"));
        Assert.True(game.World.Facts.Exists("event", "wedding_put_off"));
        Assert.DoesNotContain(game.Upcoming, f => f.Key == "wedding");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("no stead feasts at the boards"));
    }

    [Fact]
    public void TheSeasonsNews_IsSpoken_AtTheDoors()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Id != "npc_steadholder");
        NpcTests.BumpNpc(game, villager);
        Assert.DoesNotContain(game.Topics, t => t.Label == "The season's news");
        game.ApplyKey('z');

        game.Debug_DrawSteadEvent("drovers");
        NpcTests.BumpNpc(game, villager);
        Assert.Contains(game.Topics, t => t.Label == "The season's news");
        game.ApplyKey('z');

        // Newest first: the wedding's news replaces the drovers' at the doors.
        game.Debug_DrawSteadEvent("wedding");
        Wait(game, SteadRaids.TickTurns);
        NpcTests.BumpNpc(game, villager);
        int key = game.Topics.ToList().FindIndex(t => t.Label == "The season's news");
        game.ApplyKey((char)('1' + key));
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("There was a wedding"));
    }

    [Fact]
    public void TheDeck_DealsFromTheWorldsOwnSeed()
    {
        // No surgery: left alone, the season deals its own news. Twelve ticks
        // at a one-in-three chance make the odds of a silent season slim on
        // most seeds; seed 42's is not silent, and twin runs deal twin cards.
        var one = new Game(42);
        var two = new Game(42);
        Wait(one, SteadRaids.TickTurns * 12);
        Wait(two, SteadRaids.TickTurns * 12);

        static List<string> Dealt(Game g) =>
            new[] { "far_fields", "drovers", "fords_washout", "banns_read" }
                .Where(k => g.World.Facts.Exists("event", k) || g.World.Facts.Exists("omen", k)).ToList();

        Assert.NotEmpty(Dealt(one));
        Assert.Equal(Dealt(one), Dealt(two));
    }

    [Fact]
    public void TheCrossing_DealsANewDeck()
    {
        var game = new Game(42);
        game.Debug_DrawSteadEvent("fords_washout");
        Assert.Contains(game.Upcoming, f => f.Key == "fords_washout");

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The dealt future died with its world; the new valley's calendar
        // holds only its own winter, and its facts hold no old news.
        Assert.DoesNotContain(game.Upcoming, f => f.Key == "fords_washout");
        Assert.False(game.World.Facts.Exists("omen", "fords_washout"));
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
