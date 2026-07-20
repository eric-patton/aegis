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
        game.Stable.Add(new Mount { Kind = MountKind.FellPony, Pos = game.Player.Pos });
        game.Debug_SetPlayerPos(game.World.GatePos);

        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(2, game.Cycle);
        Assert.Null(game.Mount);
        Assert.Empty(game.Stable);
    }

    [Fact]
    public void TheLeans_SplitTheRoster_RacerBankerDelver()
    {
        // The courser alone takes the hills and the wood at the doubled pace.
        Assert.True(MountCatalog.Strides(MountKind.Courser, Terrain.Hills));
        Assert.True(MountCatalog.Strides(MountKind.Courser, Terrain.Forest));
        Assert.False(MountCatalog.Strides(MountKind.Mule, Terrain.Hills));
        Assert.True(MountCatalog.Strides(MountKind.Mule, Terrain.Grass));
        // Its bags are a racer's tack; the banker and the delver carry without end.
        Assert.Equal(MountCatalog.CourserBagsCap, MountCatalog.BagsCap(MountKind.Courser));
        Assert.Equal(int.MaxValue, MountCatalog.BagsCap(MountKind.Mule));
        // Only the fell pony stands an uncanny mouth.
        Assert.True(MountCatalog.Spooks(MountKind.Mule));
        Assert.True(MountCatalog.Spooks(MountKind.Courser));
        Assert.False(MountCatalog.Spooks(MountKind.FellPony));
        Assert.True(MountCatalog.UncannyMouth(SiteKind.Barrow));
        Assert.False(MountCatalog.UncannyMouth(SiteKind.GoblinCamp));
    }

    [Fact]
    public void TheRaidersCourser_IsTheDeedsOwnPrize_OncePerWorld()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        var holder = game.World.Npcs.First(n => n.Id == "npc_steadholder");

        // Higher-priority one-shot talk beats may claim the first talks; they
        // drain, and the courser keeps (the memorial's own retry pattern).
        for (int i = 0; i < 5 && game.Mount is null; i++)
        {
            NpcTests.BumpNpc(game, holder);
            game.ApplyKey(' ');
        }

        Assert.Equal(MountKind.Courser, game.Mount!.Kind);
        Assert.True(game.World.Facts.Exists("beast", "courser"));
    }

    [Fact]
    public void TheCourserBags_AreARacersTack_NotABankers()
    {
        var game = new Game(42);
        var (start, _, _) = OpenRun(game);
        game.Debug_SetPlayerPos(start);
        game.Debug_SetMount(new Mount { Kind = MountKind.Courser, Pos = start });
        LeadMuleTo(game, start);
        game.Player.Coin = MountCatalog.CourserBagsCap + 15;

        game.ApplyKey('o');

        Assert.Equal(MountCatalog.CourserBagsCap, game.Mount!.Bags);
        Assert.Equal(15, game.Player.Coin); // the rest stays the bearer's own risk
    }

    [Fact]
    public void TheSpookedBeast_ShedsTheBags_AndBoltsForHome()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        var barrow = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow.OverworldPos);
        game.Debug_SetMount(new Mount { Kind = MountKind.Mule, Pos = game.Player.Pos, Bags = 20 });
        game.Player.Coin = 0;

        game.Apply(Command.Enter);

        Assert.Null(game.Mount);
        Assert.Single(game.Stable);
        Assert.Equal(0, game.Stable[0].Bags); // nothing bolts to safety with the coin
        Assert.Equal(20, game.Player.Coin);   // the risk handed back to the bearer
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("will not stand this ground"));
    }

    [Fact]
    public void TheFellPony_StandsTheUncannyMouth()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        var barrow = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow.OverworldPos);
        game.Debug_SetMount(new Mount { Kind = MountKind.FellPony, Pos = game.Player.Pos, Bags = 20 });

        game.Apply(Command.Enter);

        Assert.NotNull(game.Mount);
        Assert.Equal(20, game.Mount!.Bags); // nerve held, bags held
        Assert.Empty(game.Stable);
    }

    [Fact]
    public void TheWildPony_IsWonWithBread_AndPatience()
    {
        var game = new Game(42);
        Assert.NotNull(game.World.WildPonyPos); // the high ground keeps one
        var wild = game.World.WildPonyPos!.Value;
        var beside = Directions.All8.Select(d => wild.Plus(d.dx, d.dy))
            .First(p => game.World.Overworld.Walkable(p));
        game.Debug_SetPlayerPos(beside);

        // Empty hands move nothing.
        game.Player.Rations = 0;
        game.ApplyKey('o');
        Assert.Equal(0, game.World.WildPonyFed);

        game.Player.Rations = MountCatalog.PonyFeedings;
        for (int i = 0; i < MountCatalog.PonyFeedings; i++) game.ApplyKey('o');

        Assert.Null(game.World.WildPonyPos);
        Assert.Equal(0, game.Player.Rations);
        Assert.Equal(MountKind.FellPony, game.Mount!.Kind);
        Assert.True(game.World.Facts.Exists("beast", "fell_pony"));
    }

    [Fact]
    public void TheStable_SwapsOneBeast_ForAnother()
    {
        var game = BuyTheMule();
        game.Stable.Add(new Mount { Kind = MountKind.Courser, Pos = game.Player.Pos });
        var woodward = game.World.Npcs.First(n => n.Id == "npc_woodward");

        NpcTests.BumpNpc(game, woodward);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey(BenchKey(game, TradeGood.Stable));

        Assert.Equal(MountKind.Courser, game.Mount!.Kind);
        Assert.Single(game.Stable);
        Assert.Equal(MountKind.Mule, game.Stable[0].Kind);

        // Pressed again, the round cycles back.
        game.ApplyKey(BenchKey(game, TradeGood.Stable));
        Assert.Equal(MountKind.Mule, game.Mount!.Kind);
    }

    [Fact]
    public void TheRaid_DoesNotReachIntoTheStable()
    {
        var game = BuyTheMule();
        game.Mount!.Bags = 30;
        var put = game.Mount;
        game.Stable.Add(put);
        game.Debug_SetMount(null);
        game.Debug_SetMode(MapMode.Site);

        game.Debug_Raid();

        Assert.Single(game.Stable);
        Assert.Equal(30, game.Stable[0].Bags);
        Assert.False(game.World.Facts.Exists("event", "beast_taken"));
    }
}
