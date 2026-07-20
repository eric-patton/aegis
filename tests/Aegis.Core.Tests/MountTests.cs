using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The beasts of the road, stage 1: the stead's mule (D-100, the last niche
/// of D-024). Sold at the steadholder's bench to a friend of the stead, it
/// walks the open land at the bearer's side (open grass passes two strides
/// to a key), waits at a site's mouth through every delve, and its
/// saddlebags bank coin against the bearer's fall: at the price that a raid
/// landing while the bearer is below takes the beast whole. World-bound.
/// </summary>
public class MountTests
{
    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static char BenchKey(Game game, TradeGood good) =>
        (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == good));

    /// <summary>Clearing the camp earns regard 3: exactly the friend rung the mule asks for.</summary>
    private static Game BuyTheMule(ulong seed = 42)
    {
        var game = new Game(seed);
        game.Debug_ClearCamp();
        game.Player.Coin = MountCatalog.MuleCoin;
        var woodward = game.World.Npcs.First(n => n.Id == "npc_woodward");
        NpcTests.BumpNpc(game, woodward);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey(BenchKey(game, TradeGood.Beast));
        game.ApplyKey(' ');
        game.ApplyKey(' ');
        Assert.NotNull(game.Mount);
        return game;
    }

    /// <summary>A cell whose next two strides in some direction are all open grass, clear of everyone.</summary>
    private static (Pos start, int dx, int dy) OpenRun(Game game)
    {
        var map = game.World.Overworld;
        for (int x = 2; x < 60; x++)
            for (int y = 2; y < 60; y++)
                foreach (var (dx, dy) in Directions.All8)
                {
                    var a = new Pos(x, y);
                    var b = a.Plus(dx, dy);
                    var c = b.Plus(dx, dy);
                    if (!map.InBounds(c)) continue;
                    if (map[a] != Terrain.Grass || map[b] != Terrain.Grass || map[c] != Terrain.Grass) continue;
                    if (game.World.Npcs.Any(n => n.Pos == a || n.Pos == b || n.Pos == c)) continue;
                    return (a, dx, dy);
                }
        throw new InvalidOperationException("no open grass run on this overworld");
    }

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, -1) => 'y', (0, -1) => 'k', (1, -1) => 'u',
        (-1, 0) => 'h', (1, 0) => 'l',
        (-1, 1) => 'b', (0, 1) => 'j', _ => 'n',
    };

    /// <summary>Test surgery: stands the mule beside an anchor, off any cells the test needs clear.</summary>
    private static void LeadMuleTo(Game game, Pos anchor, params Pos[] avoid)
    {
        var map = game.World.Overworld;
        foreach (var (dx, dy) in Directions.All8)
        {
            var cell = anchor.Plus(dx, dy);
            if (!map.Walkable(cell) || cell == game.Player.Pos || avoid.Contains(cell)) continue;
            if (game.World.Npcs.Any(n => n.Pos == cell)) continue;
            game.Mount!.Pos = cell;
            return;
        }
        throw new InvalidOperationException("nowhere to stand the mule");
    }

    [Fact]
    public void TheStead_SellsItsBeast_ToAFriendOnly()
    {
        var game = new Game(42);
        game.Player.Coin = MountCatalog.MuleCoin;
        var woodward = game.World.Npcs.First(n => n.Id == "npc_woodward");

        // A stranger's coin does not move the answer.
        NpcTests.BumpNpc(game, woodward);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey(BenchKey(game, TradeGood.Beast));
        Assert.Null(game.Mount);
        Assert.Equal(MountCatalog.MuleCoin, game.Player.Coin);
        game.ApplyKey(' ');
        game.ApplyKey(' ');

        // The friend rung opens the byre. (Deed storylets may pay coin of
        // their own on the clearing, so the purse is set fresh.)
        game.Debug_ClearCamp();
        game.Player.Coin = MountCatalog.MuleCoin;
        NpcTests.BumpNpc(game, woodward);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey(BenchKey(game, TradeGood.Beast));
        Assert.NotNull(game.Mount);
        Assert.Equal(MountKind.Mule, game.Mount!.Kind);
        Assert.Equal(0, game.Player.Coin);
    }

    [Fact]
    public void TheMule_FollowsOnTheOpenLand()
    {
        var game = BuyTheMule();
        var (start, _, _) = OpenRun(game);
        game.Debug_SetPlayerPos(start);
        var map = game.World.Overworld;
        var off = Directions.All8.Select(d => start.Plus(3 * d.dx, 3 * d.dy))
            .First(p => map.InBounds(p) && map.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
        game.Mount!.Pos = off;

        game.ApplyKey('.');
        game.ApplyKey('.');
        game.ApplyKey('.');

        Assert.True(game.Mount!.Pos.Chebyshev(game.Player.Pos) <= 1);
    }

    [Fact]
    public void TheRiddenRoad_PassesTwoStridesToAKey_OverOpenGrass()
    {
        var game = BuyTheMule();
        var (start, dx, dy) = OpenRun(game);
        game.Debug_SetPlayerPos(start);
        LeadMuleTo(game, start, start.Plus(dx, dy), start.Plus(2 * dx, 2 * dy));

        game.ApplyKey(DirKey(dx, dy));

        Assert.Equal(start.Plus(2 * dx, 2 * dy), game.Player.Pos);
    }

    [Fact]
    public void TheWalkedRoad_IsOneStride_WithoutTheBeast()
    {
        var game = new Game(42);
        var (start, dx, dy) = OpenRun(game);
        game.Debug_SetPlayerPos(start);

        game.ApplyKey(DirKey(dx, dy));

        Assert.Equal(start.Plus(dx, dy), game.Player.Pos);
    }

    [Fact]
    public void TheSaddlebags_LoadAndUnload_ByTheOneKey()
    {
        var game = BuyTheMule();
        var (start, _, _) = OpenRun(game);
        game.Debug_SetPlayerPos(start);
        LeadMuleTo(game, start);
        game.Player.Coin = 25;

        game.ApplyKey('o');
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(25, game.Mount!.Bags);

        game.ApplyKey('o');
        Assert.Equal(25, game.Player.Coin);
        Assert.Equal(0, game.Mount!.Bags);
    }

    [Fact]
    public void TheFall_SparesTheSaddlebags_TheBeastRisksThemInstead()
    {
        var game = BuyTheMule();
        var (start, _, _) = OpenRun(game);
        game.Debug_SetPlayerPos(start);
        LeadMuleTo(game, start);
        game.Player.Coin = 30;
        game.ApplyKey('o'); // banked
        game.Player.Coin = 7; // walking money

        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        Assert.NotNull(game.Mount);
        Assert.Equal(30, game.Mount!.Bags); // what rode the beast did not fall
        Assert.Equal(7, game.Remnant!.Coin); // what rode the bearer did
    }

    [Fact]
    public void TheRaid_TakesTheTetheredBeast_WhileTheBearerIsBelow()
    {
        var game = BuyTheMule();
        game.Mount!.Bags = 30;
        game.Debug_SetMode(MapMode.Site);

        game.Debug_Raid();

        Assert.Null(game.Mount);
        Assert.True(game.World.Facts.Exists("event", "beast_taken"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("gone with the raiders"));
    }

    [Fact]
    public void TheRaid_SparesTheBeast_WhenTheBearerStandsBesideIt()
    {
        var game = BuyTheMule();

        game.Debug_Raid();

        Assert.NotNull(game.Mount);
        Assert.False(game.World.Facts.Exists("event", "beast_taken"));
    }

    [Fact]
    public void TheCrossing_LeavesTheBeast_ItsLandKeepsIt()
    {
        var game = BuyTheMule();
        game.Debug_SetPlayerPos(game.World.GatePos);

        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(2, game.Cycle);
        Assert.Null(game.Mount);
    }
}
