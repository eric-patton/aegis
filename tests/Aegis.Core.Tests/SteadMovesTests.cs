using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The stead acts on the tick (D-105): the home faction's own moves at last.
/// A raid come greedy posts the watch, which turns the raiding nights away at
/// a measure of upkeep and can bare the lofts itself; the last measure calls
/// the levy, which closes the larder and takes coin against carted grain
/// instead, the stores axis' first bearer-side input beside the camp-clear.
/// Every move is narrated as it lands and written to the graph.
/// </summary>
public class SteadMovesTests
{
    [Fact]
    public void AGreedyRaid_PostsTheWatch()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck(); // choreographed ticks: the season's own deals stay in the box
        Wait(game, SteadRaids.TickTurns);
        Assert.False(game.WatchStands); // a plain raid does not move the stead

        Wait(game, SteadRaids.TickTurns); // the second raid comes greedy
        Assert.True(game.WatchStands);
        Assert.True(game.World.Facts.Exists("event", "watch_posted"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("a watch is posted on the lofts"));
        var snap = game.TakeSnapshot();
        Assert.True(snap.WatchStands);
    }

    [Fact]
    public void TheWatch_TurnsTheRaid_AndEatsAMeasure()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        Wait(game, SteadRaids.TickTurns * 2); // watch posted, stores 3
        int raids = game.Raids;
        int stores = game.Stores;

        Wait(game, SteadRaids.TickTurns);

        Assert.Equal(raids, game.Raids); // turned away: no plunder lands
        Assert.Equal(stores - SteadWatch.Upkeep, game.Stores); // but the watch ate
        var log = game.Log.Recent(4).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("melt back into the hills with nothing"));
        Assert.Contains(log, t => t.Contains("The watch must eat"));
    }

    [Fact]
    public void TheCull_StandsTheWatchDown()
    {
        // Two raiders slain drop the dens' boldness below the greedy line;
        // the stead reads the quiet hills and sends the watch home, and that
        // same night a plain raid can land again: the watch guarded against
        // greed, not against raiding itself.
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        Wait(game, SteadRaids.TickTurns * 2);
        Assert.True(game.WatchStands);

        game.Debug_SetMode(MapMode.Site);
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game);
        game.Debug_SetMode(MapMode.Overworld);
        int raids = game.Raids;

        Wait(game, SteadRaids.TickTurns);

        Assert.False(game.WatchStands);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("stands its watch down"));
        Assert.Equal(raids + 1, game.Raids); // boldness 3: a plain raid still lands
    }

    [Fact]
    public void TheWatch_StandsDown_WhenTheCampFalls()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        Wait(game, SteadRaids.TickTurns * 2);
        Assert.True(game.WatchStands);

        game.Debug_ClearCamp();

        Assert.False(game.WatchStands);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("nothing left in the hills to watch for"));
    }

    [Fact]
    public void TheLastMeasure_CallsTheLevy()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns * 4); // two raids, then the watch eats down to 1

        Assert.Equal(1, game.Stores);
        Assert.True(game.LevyStands);
        Assert.True(game.World.Facts.Exists("event", "levy_called"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the stead calls a levy"));
        var snap = game.TakeSnapshot();
        Assert.True(snap.LevyStands);
    }

    [Fact]
    public void TheLarder_TakesTheLevysAnswer_NotBread()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns * 4);
        Assert.True(game.LevyStands);
        game.Player.Coin = 30;
        int regard = game.Regard;

        var holder = game.World.Npcs.First(n => n.Id == "npc_steadholder");
        NpcTests.BumpNpc(game, holder);
        Assert.Contains(game.Offers, o => o.Label.Contains("Answer the stead's levy"));

        int rations = game.Player.Rations;
        game.ApplyKey(OfferKey(game, TradeGood.Ration));

        Assert.Equal(rations, game.Player.Rations); // no bread changed hands
        Assert.Equal(30 - SteadLevy.AnswerCoin, game.Player.Coin);
        Assert.Equal(2, game.Stores); // the carted measure
        Assert.Equal(regard + 1, game.Regard); // a deed the stead perceives
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("who answered its levy"));

        // And that measure clears the last-measure line: the levy lifts.
        Assert.False(game.LevyStands);
        Assert.True(game.World.Facts.Exists("event", "levy_met"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("levy is met"));
    }

    [Fact]
    public void ShortCoin_AnswersNothing_AndBuysNothing()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns * 4);
        Assert.True(game.LevyStands);
        game.Player.Coin = SteadLevy.AnswerCoin - 1;

        var holder = game.World.Npcs.First(n => n.Id == "npc_steadholder");
        NpcTests.BumpNpc(game, holder);
        game.ApplyKey(OfferKey(game, TradeGood.Ration));

        Assert.Equal(SteadLevy.AnswerCoin - 1, game.Player.Coin);
        Assert.Equal(1, game.Stores);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("less buys nothing here tonight"));
    }

    [Fact]
    public void TheLevysAsk_IsSaid_WhileItStands()
    {
        var game = new Game(42);
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Id != "npc_steadholder");
        NpcTests.BumpNpc(game, villager);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("shaped like your hand"));
        game.ApplyKey('z');

        Wait(game, SteadRaids.TickTurns * 4);
        Assert.True(game.LevyStands);
        NpcTests.BumpNpc(game, villager);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("shaped like your hand"));
    }

    [Fact]
    public void TheSeasonsRecovery_LiftsTheLevy()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns * 4);
        Assert.True(game.LevyStands);

        game.Debug_ClearCamp();
        // The hard winter claims this seed's next tick (D-132) and takes the
        // last measure with it, so the season needs two carts, not one, to
        // climb clear of the levy line.
        Wait(game, SteadRaids.TickTurns * 3);

        Assert.False(game.LevyStands);
        Assert.True(game.World.Facts.Exists("event", "levy_met"));
    }

    [Fact]
    public void TheCrossing_StandsBothMovesDown()
    {
        var game = new Game(42);
        Wait(game, SteadRaids.TickTurns * 4); // watch up, levy standing
        Assert.True(game.WatchStands);
        Assert.True(game.LevyStands);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.False(game.WatchStands);
        Assert.False(game.LevyStands);
        var snap = game.TakeSnapshot();
        Assert.False(snap.WatchStands);
        Assert.False(snap.LevyStands);
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }

    private static char OfferKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == good)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {good} offer");
    }
}
