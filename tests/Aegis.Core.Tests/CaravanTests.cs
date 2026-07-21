using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The caravan leg (D-144, plan 2026-07 B3's second half): the economy's
/// first buy-to-resell trade, D-025's productive capital at its smallest.
/// The cart sells salt cheap (stock per world, grown with the tier), the
/// town's provisioner pays for the carrying, and the margin is the walk
/// east, made real freight. The tests hold the stock's deal, both ends of
/// the trade, the refusals, the law's reach, and the shared nine.
/// </summary>
public class CaravanTests
{
    [Fact]
    public void TheCart_DealsSalt_ByTheTier()
    {
        Assert.Equal(3, new Game(42).World.PeddlerSalt); // tier 1: two and the tier
        Assert.Equal(6, WorldGen.Generate(7, tier: 4).PeddlerSalt);
        Assert.Equal(6, WorldGen.Generate(7, tier: 8).PeddlerSalt); // never more than six
    }

    [Fact]
    public void TheSack_IsBought_UntilTheBoardsAreBare()
    {
        var game = new Game(42);
        game.Player.Coin = 20;
        NpcTests.BumpNpc(game, game.World.Peddler);

        for (int i = 0; i < 3; i++) game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Salt));
        Assert.Equal(3, game.Player.Salt);
        Assert.Equal(0, game.World.PeddlerSalt);
        Assert.Equal(20 - 3 * Peddling.SaltPrice, game.Player.Coin);

        // The cart sold through: the fourth press moves nothing.
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Salt));
        Assert.Equal(3, game.Player.Salt);
        Assert.Equal(5, game.Player.Coin);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("Sold through"));
    }

    [Fact]
    public void ShortCoin_LoadsNoSack()
    {
        var game = new Game(42);
        game.Player.Coin = Peddling.SaltPrice - 1;
        NpcTests.BumpNpc(game, game.World.Peddler);
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Salt));
        Assert.Equal(0, game.Player.Salt);
        Assert.Equal(Peddling.SaltPrice - 1, game.Player.Coin);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("no slates"));
    }

    [Fact]
    public void TheLeg_PaysAtTheTownCounter_AndFeedsTheCraft()
    {
        var game = new Game(42);
        game.Player.Salt = 2;
        NewsTests.EnterTown(game);
        int coin = game.Player.Coin;
        NewsTests.BumpTowner(game, "npc_provisioner");
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Salt));

        // Two sacks at the town's price, the margin over the cart's 5 being
        // the walk east; the lot feeds Commerce like every town lot (D-140).
        Assert.Equal(coin + 2 * TownMarket.SaltPrice, game.Player.Coin);
        Assert.Equal(0, game.Player.Salt);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Commerce));
    }

    [Fact]
    public void TheEmptyPack_SellsNothing()
    {
        var game = new Game(42);
        NewsTests.EnterTown(game);
        int coin = game.Player.Coin;
        NewsTests.BumpTowner(game, "npc_provisioner");
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Salt));
        Assert.Equal(coin, game.Player.Coin);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("has not brought any"));
    }

    [Fact]
    public void TheBarredHand_TradesNoFreight()
    {
        var game = new Game(42);
        game.Player.Salt = 2;
        NewsTests.EnterTown(game);
        game.Debug_RaiseTownBook(TownLaw.BarredRung);
        int coin = game.Player.Coin;
        NewsTests.BumpTowner(game, "npc_provisioner");
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Salt));
        Assert.Equal(2, game.Player.Salt);
        Assert.Equal(coin, game.Player.Coin);
    }

    [Fact]
    public void BothCounters_KeepTheSharedNine()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Peddler);
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
        game.ApplyKey(' ');

        NewsTests.EnterTown(game);
        NewsTests.BumpTowner(game, "npc_provisioner");
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
    }
}
