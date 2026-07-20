using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The peddler and the fence (D-124): the road's own trader, camped with a
/// cart outside every stead, and the crime family's missing buyer. Pilfering
/// now pockets a small thing off the mantel beside the loaf; nothing in the
/// stead will buy its own heirlooms back, and the cart never asks. Bread at
/// the road's price to anyone (the larder's bars are the stead's books, not
/// the cart's), and hides a coin over the wood's-edge bench: the arbitrage's
/// first stone (D-025).
/// </summary>
public class PeddlerTests
{
    [Fact]
    public void TheCart_StandsOnEveryRoad()
    {
        var game = new Game(1);
        var peddler = game.World.Peddler;
        Assert.Equal(NpcKind.Peddler, peddler.Kind);
        Assert.Equal("peddler", peddler.Role);
        Assert.True(game.World.Facts.Exists("person", "npc_peddler"));
        Assert.True(game.World.Facts.Exists("wanderer", "npc_peddler"));

        // Every tier keeps a cart: the deep worlds' roads are still roads.
        var deep = WorldGen.Generate(SeedTree.Derive(1, "cycle", 2), tier: 2);
        Assert.Contains(deep.Npcs, n => n.Kind == NpcKind.Peddler);
    }

    [Fact]
    public void TheTalk_AtTheCart()
    {
        var game = new Game(1);
        NpcTests.BumpNpc(game, game.World.Peddler);

        Assert.True(game.InTalkMenu);
        Assert.True(game.World.Facts.Exists("met", "npc_peddler"));
        Assert.Equal(2, game.Topics.Count);
        Assert.Equal(3, game.Offers.Count);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Ration);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Hide);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Fence);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("weighs your pack before your face"));
    }

    [Fact]
    public void TheRoadsBread_SellsToAnyone()
    {
        var game = new Game(42);
        game.Player.Coin = 20;
        NpcTests.BumpNpc(game, game.World.Peddler);
        game.ApplyKey(OfferDigit(game, TradeGood.Ration));
        Assert.Equal(20 - Peddling.RationPrice, game.Player.Coin);
        Assert.Equal(1, game.Player.Rations);
        game.ApplyKey(' ');

        // A named thief, barred from the larder (D-086), is still fed by the road.
        ShameTests.RobDoors(game, 3);
        Assert.True(game.LarderBarred);
        NpcTests.BumpNpc(game, game.World.Peddler);
        game.ApplyKey(OfferDigit(game, TradeGood.Ration));
        Assert.Equal(20 - 2 * Peddling.RationPrice, game.Player.Coin);
        Assert.Equal(5, game.Player.Rations);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("it prices bread, not conduct"));
    }

    [Fact]
    public void ShortCoin_RunsNoSlate()
    {
        var game = new Game(1);
        game.Player.Coin = Peddling.RationPrice - 1;
        NpcTests.BumpNpc(game, game.World.Peddler);

        game.ApplyKey(OfferDigit(game, TradeGood.Ration));

        Assert.Equal(Peddling.RationPrice - 1, game.Player.Coin);
        Assert.Equal(0, game.Player.Rations);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("does not run a slate"));
    }

    [Fact]
    public void ThePilfer_PocketsAThingWithAPast()
    {
        var game = new Game(42);
        ShameTests.RobDoors(game, 1);

        Assert.Equal(1, game.Player.Trinket);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("small thing follows from the mantel"));
    }

    [Fact]
    public void TheFullPack_TakesNoTrinketEither()
    {
        var game = new Game(42);
        game.Player.Rations = Game.RationCap; // the arithmetic refuses the whole take
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2));
        game.Apply(Command.Grab);

        Assert.Equal(0, game.Player.Trinket);
        Assert.Empty(game.World.PilferedHouses);
    }

    [Fact]
    public void TheFence_BuysWhatHasAPast_AndAsksNothing()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        ShameTests.RobDoors(game, 2);
        Assert.Equal(2, game.Player.Trinket);
        int shame = game.Shame;
        NpcTests.BumpNpc(game, game.World.Peddler);
        int turn = game.Turn;

        game.ApplyKey(OfferDigit(game, TradeGood.Fence));

        Assert.Equal(0, game.Player.Trinket);
        Assert.Equal(2 * Peddling.TrinketPrice, game.Player.Coin);
        Assert.Equal(turn, game.Turn);   // a sale, not a march
        Assert.Equal(shame, game.Shame); // the cart tells no one
        Assert.True(game.InTalkMenu);
        Assert.True(game.World.Facts.Exists("secret", "fenced_goods"));
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("asks nothing"));
    }

    [Fact]
    public void TheFence_WithNothingToShow()
    {
        var game = new Game(1);
        game.Player.Coin = 10;
        NpcTests.BumpNpc(game, game.World.Peddler);

        game.ApplyKey(OfferDigit(game, TradeGood.Fence));

        Assert.Equal(10, game.Player.Coin);
        Assert.False(game.World.Facts.Exists("secret", "fenced_goods"));
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("nothing of the kind today"));
    }

    [Fact]
    public void TheFencesLabel_ReadsThePack()
    {
        var game = new Game(42);
        ShameTests.RobDoors(game, 2);
        NpcTests.BumpNpc(game, game.World.Peddler);
        Assert.Contains(game.Offers, o => o.Label.Contains($"2 at {Peddling.TrinketPrice}c"));

        game.ApplyKey(OfferDigit(game, TradeGood.Fence));

        Assert.Contains(game.Offers, o => o.Label.Contains("nothing in your pack has one"));
    }

    [Fact]
    public void TheHides_FetchTheRoadsCoin()
    {
        var game = new Game(1);
        game.Player.Hide = 2;
        game.Player.Coin = 0;
        NpcTests.BumpNpc(game, game.World.Peddler);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Hide
            && o.Label.Contains($"at {game.HidePrice + Peddling.HideBonus}c"));

        game.ApplyKey(OfferDigit(game, TradeGood.Hide));

        Assert.Equal(0, game.Player.Hide);
        Assert.Equal(2 * (game.HidePrice + Peddling.HideBonus), game.Player.Coin);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Hide && o.Label.Contains("none cured yet"));
    }

    [Fact]
    public void TheCartsOwnPocket_IsNotAMark()
    {
        var game = new Game(1);
        var peddler = game.World.Peddler;
        var beside = Directions.All8
            .Select(d => peddler.Pos.Plus(d.dx, d.dy))
            .First(p => game.World.Overworld.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);

        game.ApplyKey('p');

        Assert.Empty(game.World.LiftedNpcs);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("No one stands near enough to brush against"));
    }

    [Fact]
    public void TheCrossing_CarriesThePastAlong()
    {
        var game = new Game(42);
        ShameTests.RobDoors(game, 1);
        Assert.Equal(1, game.Player.Trinket);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The trinket crossed on the body, like the hides; the new world's cart
        // is as uncurious as the last one's, and its books are just as blank.
        Assert.Equal(1, game.Player.Trinket);
        Assert.False(game.World.Facts.Exists("secret", "fenced_goods"));
        int coin = game.Player.Coin;
        NpcTests.BumpNpc(game, game.World.Peddler);
        game.ApplyKey(OfferDigit(game, TradeGood.Fence));
        Assert.Equal(0, game.Player.Trinket);
        Assert.Equal(coin + Peddling.TrinketPrice, game.Player.Coin);
        Assert.True(game.World.Facts.Exists("secret", "fenced_goods"));
    }

    private static char OfferDigit(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));
}
