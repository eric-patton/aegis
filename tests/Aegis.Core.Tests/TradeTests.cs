using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Coin sinks v0 (D-036, D-025): the steadholder sells rations, the herbwife dresses
/// wounds, prices read the fact graph, and provisions are the one store of value that
/// survives death.
/// </summary>
public class TradeTests
{
    [Fact]
    public void Steadholder_SellsRations_RefusingBrokeAndOverloaded()
    {
        var game = new Game(42);
        game.Player.Coin = 3;
        NpcTests.BumpNpc(game, Steadholder(game));

        Assert.Contains(game.Offers, o => o.Good == TradeGood.Ration);
        char buy = OfferKey(game, TradeGood.Ration);

        // Broke: 3 coin against a 4 coin loaf.
        game.ApplyKey(buy);
        Assert.Equal(0, game.Player.Rations);
        Assert.Equal(3, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("not a charity"));

        // Flush: five purchases at 4 coin each, menu open the whole time.
        game.Player.Coin = 24;
        for (int i = 0; i < 5; i++) game.ApplyKey(buy);
        Assert.True(game.InTalkMenu);
        Assert.Equal(Game.RationCap, game.Player.Rations);
        Assert.Equal(4, game.Player.Coin);

        // The cap refuses without taking coin.
        game.ApplyKey(buy);
        Assert.Equal(Game.RationCap, game.Player.Rations);
        Assert.Equal(4, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("all a walking body can"));
    }

    [Fact]
    public void Eating_Heals_TakesATurn_AndIsRefusedWhenPointless()
    {
        var game = new Game(42);
        game.Player.Rations = 2;
        int turn = game.Turn;

        // Whole and rested: the ration keeps, and no time passes.
        game.ApplyKey('e');
        Assert.Equal(2, game.Player.Rations);
        Assert.Equal(turn, game.Turn);

        game.Debug_HurtPlayer(10);
        game.ApplyKey('e');
        Assert.Equal(1, game.Player.Rations);
        Assert.Equal(16, game.Player.Hp);
        Assert.Equal(turn + 1, game.Turn);

        // The second bite caps at max hp.
        game.ApplyKey('e');
        Assert.Equal(0, game.Player.Rations);
        Assert.Equal(20, game.Player.Hp);

        // Nothing left to eat: no turn passes.
        game.Debug_HurtPlayer(5);
        game.ApplyKey('e');
        Assert.Equal(turn + 2, game.Turn);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("nothing to eat"));
    }

    [Fact]
    public void Mending_IsPricedByTheWound_AndCuresIt()
    {
        var game = new Game(42);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(80, game.Player.WoundedTurns);
        Assert.Equal(20, game.MendPrice);

        // The price falls as convalescence passes: waiting is always the free option.
        for (int i = 0; i < 8; i++) game.ApplyKey('.');
        Assert.Equal(72, game.Player.WoundedTurns);
        Assert.Equal(18, game.MendPrice);

        // The bump itself takes a turn (71 left). The dressing is done at the
        // stillroom's table now (D-081): off the talk menu, onto her bench.
        NpcTests.BumpNpc(game, Herbwife(game));
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Mending);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.True(game.InTradeMenu);
        char mend = BenchKey(game, TradeGood.Mending);

        // Death dropped the coin, and the herbwife does not work on credit.
        game.Player.Coin = 5;
        game.ApplyKey(mend);
        Assert.Equal(71, game.Player.WoundedTurns);
        Assert.Equal(5, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("let time do it"));

        game.Player.Coin = 20;
        game.ApplyKey(mend);
        Assert.Equal(0, game.Player.WoundedTurns);
        Assert.Equal(2, game.Player.Coin);
        Assert.Equal(game.Player.MaxHp, game.Player.EffectiveMaxHp);

        // Whole again: the entry stays listed (digits never shift under a
        // buyer's hand) but reads whole, and pressing it takes no coin.
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Mending && o.Label.Contains("you are whole"));
        game.ApplyKey(mend);
        Assert.Equal(2, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("You are whole"));
    }

    [Fact]
    public void RationPrice_RisesWhileTheBlightStands()
    {
        var game = new Game(41);
        Assert.Equal(4, game.RationPrice);

        // Master 41's second world tells the Creeping Blight (pinned by BlightTests).
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(CreepingBlightTemplate.Id, game.World.Facts.OfType("story").First().Subject);
        Assert.Equal(6, game.RationPrice);

        // The offer label quotes the lean price.
        NpcTests.BumpNpc(game, Steadholder(game));
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Ration && o.Label.Contains("6 coin"));
        game.ApplyKey(' ');

        // Once the story completes, the larders recover.
        game.World.Facts.Add("story_complete", CreepingBlightTemplate.Id, game.World.SettlementName);
        Assert.Equal(4, game.RationPrice);
    }

    [Fact]
    public void Rations_SurviveDeath_AndCrossings()
    {
        var game = new Game(42);
        game.Player.Coin = 30;
        game.Player.Rations = 3;

        // Death drops coin into the remnant; provisions stay on your person.
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(3, game.Player.Rations);
        Assert.NotNull(game.Remnant);
        Assert.Equal(30, game.Remnant!.Coin);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal(3, game.Player.Rations);
    }

    [Fact]
    public void TradeSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        // Granted, not journaled: grant identically on both sides.
        live.Player.Coin = 20;
        live.Debug_HurtPlayer(6);

        var target = Steadholder(live).Pos;
        for (int guard = 0; guard < 400 && !live.InTalkMenu; guard++)
        {
            // The shuttered window may open on the way (D-117); leaving is journaled too.
            if (live.InScene) { live.ApplyKey('3'); continue; }
            char? key = UnbinderTests.StepTo(live, target);
            if (key is null) break;
            live.ApplyKey(key.Value);
        }
        Assert.True(live.InTalkMenu, "bot never reached the steadholder");

        char buy = OfferKey(live, TradeGood.Ration);
        live.ApplyKey(buy);
        live.ApplyKey(buy);
        live.ApplyKey(' ');
        live.ApplyKey('e');
        Assert.Equal(1, live.Player.Rations);

        var replayed = new Game(seed);
        replayed.Player.Coin = 20;
        replayed.Debug_HurtPlayer(6);
        foreach (char key in journal.ToString()) replayed.ApplyKey(key);

        Assert.Equal(live.Player.Rations, replayed.Player.Rations);
        Assert.Equal(live.Player.Coin, replayed.Player.Coin);
        Assert.Equal(live.Player.Hp, replayed.Player.Hp);
        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(
            live.Log.Recent(15).Select(e => e.Text),
            replayed.Log.Recent(15).Select(e => e.Text));
    }

    private static Npc Steadholder(Game game) => game.World.Npcs.First(n => n.Id == "npc_steadholder");
    private static Npc Herbwife(Game game) => game.World.Npcs.First(n => n.Id == "npc_herbwife");

    /// <summary>The digit that selects a good in the open talk menu.</summary>
    private static char OfferKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == good)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {good} offer in this menu");
    }

    /// <summary>The digit that selects a good at an open bench (D-071/D-081).</summary>
    private static char BenchKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.TradeOffers.Count; i++)
            if (game.TradeOffers[i].Good == good)
                return (char)('1' + i);
        throw new InvalidOperationException($"no {good} entry on this bench");
    }
}
