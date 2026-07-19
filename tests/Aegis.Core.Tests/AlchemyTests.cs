using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The stillroom's craft (D-090): the herb lane grown into alchemy v1. Three
/// sprigs steep into a hale-draught at the herbwife's bench (the satchel's
/// first sink, priced in simples and never in coin), 'd' drinks it anywhere
/// the road hurts (stronger than a meal at blood and wound alike), and the
/// stillcraft lesson (the fourth lesson, D-087's deferred slot) steeps a
/// draught of the bearer's own at any shrine rest, any world.
/// </summary>
public class AlchemyTests
{
    [Fact]
    public void TheDraught_IsSteepedAtTheStillroom_ForSprigs()
    {
        var game = new Game(42);
        game.Player.Herb = 7;
        OpenStillroom(game);

        game.ApplyKey(TradeKey(game, TradeGood.Draught));
        Assert.Equal(1, game.Player.Draughts);
        Assert.Equal(4, game.Player.Herb);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("stoppered vial"));

        game.ApplyKey(TradeKey(game, TradeGood.Draught));
        Assert.Equal(2, game.Player.Draughts);
        Assert.Equal(1, game.Player.Herb);

        // The satchel keeps two vials whole; the third steeping is turned away.
        game.ApplyKey(TradeKey(game, TradeGood.Draught));
        Assert.Equal(2, game.Player.Draughts);
        Assert.Equal(1, game.Player.Herb);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("come back to me"));
    }

    [Fact]
    public void TheSteeping_AsksForSprigs_AndTakesNoCoin()
    {
        var game = new Game(42);
        game.Player.Herb = 2;
        game.Player.Coin = 20;
        OpenStillroom(game);

        game.ApplyKey(TradeKey(game, TradeGood.Draught));
        Assert.Equal(0, game.Player.Draughts);
        Assert.Equal(2, game.Player.Herb);
        Assert.Equal(20, game.Player.Coin); // sprigs are the only price there is
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("You carry 2"));
    }

    [Fact]
    public void TheDraught_IsDrunkOnTheRoad()
    {
        var game = new Game(42);
        game.Player.Draughts = 1;
        game.Player.Hp = 1;
        game.Player.WoundedTurns = 30;
        int turnBefore = game.Turn;

        game.ApplyKey('d');
        Assert.Equal(Math.Min(game.Player.EffectiveMaxHp, 1 + Game.DraughtHeal), game.Player.Hp);
        // The cut, and then the swallowed turn's own tick of convalescence.
        Assert.Equal(30 - Game.DraughtWoundCut - 1, game.Player.WoundedTurns);
        Assert.Equal(0, game.Player.Draughts);
        Assert.Equal(turnBefore + 1, game.Turn); // the stopper and the swallow cost the turn
        var log = game.Log.Recent(4).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("drink the draught down"));
        Assert.Contains(log, t => t.Contains("ease it deep"));

        // Empty-handed the key refuses, and costs nothing.
        game.ApplyKey('d');
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("You carry no draught"));
    }

    [Fact]
    public void TheVial_Keeps_WhenNothingHurts()
    {
        var game = new Game(42);
        game.Player.Draughts = 1;
        int turnBefore = game.Turn;

        game.ApplyKey('d');
        Assert.Equal(1, game.Player.Draughts);
        Assert.Equal(turnBefore, game.Turn); // a refusal costs no turn
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("the vial keeps"));
    }

    [Fact]
    public void TheStillcraft_SteepsAtTheRest_InAnyWorld()
    {
        var game = new Game(42);
        game.Player.Coin = LessonCatalog.Def(LessonId.Stillcraft).Price;
        game.Player.Herb = 3;
        OpenStillroom(game);
        game.ApplyKey(TradeKey(game, TradeGood.Lesson));
        Assert.True(game.Player.HasLesson(LessonId.Stillcraft));
        Assert.Equal(0, game.Player.Coin);
        game.ApplyKey(' '); // leave the stillroom

        // The taught rest steeps a draught of the bearer's own.
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.ApplyKey('r');
        Assert.Equal(1, game.Player.Draughts);
        Assert.Equal(0, game.Player.Herb);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("A draught of your own"));
        game.ApplyKey(' '); // rise

        // The lesson crosses the waygate whole (D-052): the next world's shrine steeps too.
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        game.Player.Herb = 3;
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.ApplyKey('r');
        Assert.Equal(2, game.Player.Draughts);
        Assert.Equal(0, game.Player.Herb);
    }

    /// <summary>Bumps the herbwife and steps through her talk digit into the stillroom.</summary>
    private static void OpenStillroom(Game game)
    {
        var herbwife = game.World.Npcs.First(n => n.Id == "npc_herbwife");
        NpcTests.BumpNpc(game, herbwife);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static char TradeKey(Game game, TradeGood good) =>
        (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == good));
}
