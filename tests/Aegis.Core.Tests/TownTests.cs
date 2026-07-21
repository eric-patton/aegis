using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The market town (D-140, plan 2026-07 B2's first cut): authored chunks
/// stitched per seed behind a gate at the east road's end, the world's first
/// peopled site, the market that pays the road's best prices, and Commerce,
/// the 12th skill, fed only by lots sold above the valley's own price. The
/// tests hold the stitch's determinism, the gate crossing both ways, the
/// stalls' arithmetic, the craft's cost-gating, and the replay.
/// </summary>
public class TownTests
{
    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    /// <summary>Walks the real road and gate: the mouth, then the arch.</summary>
    private static void EnterTown(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.True(game.OnRoad);
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
        Assert.Equal("town", game.CurrentSite!.Id);
    }

    /// <summary>Bumps a towner inside the town through the real key surface.</summary>
    private static Npc BumpTowner(Game game, string id)
    {
        var npc = game.World.Npcs.First(n => n.Id == id);
        var town = game.CurrentSite!.Map;
        var beside = Directions.All8
            .Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => town.Walkable(p) && !game.World.Npcs.Any(n => n.SiteId == "town" && n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
        return npc;
    }

    [Fact]
    public void TheWorldGrowsATown_Deterministically()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            var town = a.TownSite;
            Assert.True(town.OnRoad);
            Assert.Equal(Terrain.TownGate, a.Road[town.OverworldPos]);
            Assert.Equal(town.Map.ContentHash(), b.TownSite.Map.ContentHash());
            Assert.Equal(a.TownName, b.TownName);
            Assert.False(string.IsNullOrWhiteSpace(a.TownName));
            Assert.Equal(Terrain.ExitLadder, town.Map[town.EntryPos]);
            Assert.Empty(town.Spawns);

            // The whole cast stands, walkable and distinct, inside the walls.
            foreach (string id in (string[])["npc_provisioner", "npc_hidemonger", "npc_herbmonger", "npc_mootwarden"])
            {
                var npc = a.Npcs.First(n => n.Id == id);
                Assert.Equal("town", npc.SiteId);
                Assert.Equal(NpcKind.Towner, npc.Kind);
                Assert.True(town.Map.Walkable(npc.Pos), $"seed {seed}: {id} on unwalkable ground");
                // Every stall is reachable from the gate arch on foot: an
                // authored chunk must never wall its own counter off.
                Assert.True(Reachable(town.Map, town.EntryPos, npc.Pos), $"seed {seed}: {id} walled off from the gate");
            }
            Assert.True(a.Facts.Exists("site", "town"));
            Assert.True(a.Facts.Exists("person", "npc_mootwarden"));
        }
    }

    [Fact]
    public void TheGate_CrossesBothWays()
    {
        var game = new Game(42);
        EnterTown(game);
        Assert.Equal(game.World.TownSite.EntryPos, game.Player.Pos);
        Assert.Equal("town", game.CurrentMap.Id);

        game.ApplyKey('<');
        Assert.Equal(MapMode.Overworld, game.Mode);
        Assert.True(game.OnRoad); // the gate opens on the road, not the valley
        Assert.Equal(game.World.TownSite.OverworldPos, game.Player.Pos);
    }

    [Fact]
    public void TheMarket_PaysTheWalk_AndFeedsCommerce()
    {
        var game = new Game(42);
        game.Player.Hide = 3;
        game.Player.Herb = 2;
        EnterTown(game);

        // The hides at the market's price, the world's best rung (D-025's
        // ladder: bench 3, cart 4, town 5), and the lot counts one Commerce use.
        int coin = game.Player.Coin;
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(0, game.Player.Hide);
        Assert.Equal(coin + 3 * TownMarket.HidePrice, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Commerce));
        game.ApplyKey(' ');

        // The herbs at the stall over the stillroom's price, a second use.
        coin = game.Player.Coin;
        BumpTowner(game, "npc_herbmonger");
        game.ApplyKey(OfferKey(game, TradeGood.Herb));
        Assert.Equal(0, game.Player.Herb);
        Assert.Equal(coin + 2 * TownMarket.HerbPrice, game.Player.Coin);
        Assert.Equal(2, game.Player.Skills.Uses(SkillId.Commerce));

        // An empty pack sells nothing and counts nothing: the craft is fed
        // by lots that moved, never by standing at a counter (D-014).
        game.ApplyKey(OfferKey(game, TradeGood.Herb));
        Assert.Equal(2, game.Player.Skills.Uses(SkillId.Commerce));
    }

    [Fact]
    public void TheValleyBenches_FeedNoCommerce()
    {
        var game = new Game(42);
        game.Player.Herb = 2;
        var wife = game.World.Npcs.First(n => n.Id == "npc_herbwife");
        NpcTests.BumpNpc(game, wife);
        int trade = game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Trade);
        game.ApplyKey((char)('1' + game.Topics.Count + trade));
        Assert.True(game.InTradeMenu);
        int digit = game.TradeOffers.ToList().FindIndex(o => o.Good == TradeGood.Herb);
        game.ApplyKey((char)('1' + digit));

        // The stillroom pays its price and teaches no trade: the margin the
        // craft feeds on is the walk east, and this counter is a walk home.
        Assert.Equal(0, game.Player.Herb);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Commerce));
    }

    [Fact]
    public void ThePracticedTongue_AddsItsLevel()
    {
        var game = new Game(42);
        // A trader's hands, grown the only way hands grow (D-016): level 1 at 8 uses.
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Commerce);
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Commerce));

        game.Player.Hide = 2;
        EnterTown(game);
        int coin = game.Player.Coin;
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(coin + 2 * TownMarket.HidePrice + 1, game.Player.Coin);
    }

    [Fact]
    public void TheProvisioner_SellsTheMarketsLoaf()
    {
        var game = new Game(42);
        game.Player.Coin = TownMarket.RationPrice;
        int rations = game.Player.Rations;
        EnterTown(game);
        BumpTowner(game, "npc_provisioner");
        game.ApplyKey(OfferKey(game, TradeGood.Ration));

        Assert.Equal(rations + 1, game.Player.Rations);
        Assert.Equal(0, game.Player.Coin);

        // Short coin buys nothing, and the market runs no slates.
        game.ApplyKey(OfferKey(game, TradeGood.Ration));
        Assert.Equal(rations + 1, game.Player.Rations);
    }

    [Fact]
    public void TheWarden_SpeaksForTheTown_AndSellsNothing()
    {
        var game = new Game(42);
        EnterTown(game);
        BumpTowner(game, "npc_mootwarden");

        Assert.True(game.InTalkMenu);
        Assert.Empty(game.Offers);
        Assert.Contains(game.Topics, t => t.Label == "The moot");
        Assert.Contains(game.Topics, t => t.Label == "The guild");
        Assert.True(game.World.Facts.Exists("met", "npc_mootwarden"));
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
    }

    [Fact]
    public void TheTown_ReplaysLikeEverythingElse()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        var journal = new System.Text.StringBuilder();
        live.KeyApplied += k => journal.Append(k);
        live.Player.Hide = 2;

        EnterTown(live);
        BumpTowner(live, "npc_hidemonger");
        live.ApplyKey(OfferKey(live, TradeGood.Hide));
        live.ApplyKey(' ');

        var replayed = new Game(seed);
        replayed.Player.Hide = 2;
        replayed.Debug_SetPlayerPos(replayed.World.RoadMouthPos);
        int i = 0;
        foreach (char key in journal.ToString())
        {
            // The two debug teleports are not journaled; mirror them at the
            // same indices (after the mouth key and after the gate key).
            replayed.ApplyKey(key);
            i++;
            if (i == 1) replayed.Debug_SetPlayerPos(replayed.World.TownSite.OverworldPos);
            if (i == 2)
            {
                var npc = replayed.World.Npcs.First(n => n.Id == "npc_hidemonger");
                var town = replayed.CurrentSite!.Map;
                var beside = Directions.All8
                    .Select(d => npc.Pos.Plus(d.dx, d.dy))
                    .First(p => town.Walkable(p) && !replayed.World.Npcs.Any(n => n.SiteId == "town" && n.Pos == p));
                replayed.Debug_SetPlayerPos(beside);
            }
        }

        Assert.Equal(live.Player.Coin, replayed.Player.Coin);
        Assert.Equal(live.Player.Skills.Uses(SkillId.Commerce), replayed.Player.Skills.Uses(SkillId.Commerce));
        Assert.Equal(
            live.Log.Recent(8).Select(e => e.Text),
            replayed.Log.Recent(8).Select(e => e.Text));
    }

    private static bool Reachable(GameMap map, Pos from, Pos to)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == to) return true;
            foreach (var (dx, dy) in Directions.All8)
            {
                var next = p.Plus(dx, dy);
                if (!map.InBounds(next) || !map.Walkable(next) || seen.Contains(next)) continue;
                seen.Add(next);
                queue.Enqueue(next);
            }
        }
        return false;
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
