using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The hunt (D-070): the wilds at tier 2+, the fleeing hart (grazes far, bolts near,
/// runs at the bearer's own speed, gone through a run in the treeline), and the yield
/// a felled hart pays, meat, hide, and the new Hunting skill, but no essence and no coin.
/// </summary>
public class HuntingTests
{
    [Fact]
    public void Wilds_ExistsAtTierTwoPlus_HoldsFleeingHarts_Reachable_Deterministic()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.Null(WorldGen.Generate(seed, tier: 1).WildsSite);

            foreach (int tier in (int[])[2, 5])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var wilds = a.WildsSite;
                Assert.NotNull(wilds);
                Assert.Equal(Terrain.WildsEntrance, a.Overworld[wilds!.OverworldPos]);
                Assert.Equal(wilds.OverworldPos, b.WildsSite!.OverworldPos);
                Assert.True(Reachable(a.Overworld, a.ShrinePos, wilds.OverworldPos),
                    $"seed {seed} tier {tier}: the wilds is unreachable");

                Assert.NotEmpty(wilds.Spawns);
                Assert.All(wilds.Spawns, s => Assert.Equal(MonsterKind.Hart, s.Kind));
                // The game grazes deep, off the near edge, and every one is catchable on foot.
                foreach (var s in wilds.Spawns)
                    Assert.True(Reachable(wilds.Map, wilds.EntryPos, s.Pos),
                        $"seed {seed} tier {tier}: a hart is walled in");
                Assert.True(a.Facts.Exists("site", "wilds"));
            }
        }
    }

    [Fact]
    public void Hart_GrazesFar_ButFlees_WhenStalkedClose()
    {
        var game = EnterWilds();
        var hart = FindHart(game);

        // Far off, it grazes: a wait does not move it.
        game.Debug_SetPlayerPos(WalkableAtChebyshev(game, hart.Pos, 7));
        var grazing = hart.Pos;
        game.ApplyKey('.');
        Assert.Equal(grazing, hart.Pos);

        // Stalked close, it breaks: within a few cells it gives ground or bolts.
        game.Debug_SetPlayerPos(WalkableAtChebyshev(game, hart.Pos, 3));
        var start = hart.Pos;
        for (int i = 0; i < 8 && hart.Alive && hart.Pos == start; i++) game.ApplyKey('.');
        Assert.True(!hart.Alive || hart.Pos != start, "a stalked hart neither fled nor bolted");
    }

    [Fact]
    public void Hart_BroughtDown_PaysInHideMeatAndHunting_NotEssenceOrCoin()
    {
        var game = EnterWilds();
        var hart = FindHart(game);
        hart.Hp = 1;
        game.Player.Hide = 0;
        game.Player.Rations = 0;
        game.Player.RawMeat = 0;
        int essenceBefore = game.Player.Essence;
        int coinBefore = game.Player.Coin;
        int huntingBefore = game.Player.Skills.Uses(SkillId.Hunting);

        var spot = AdjacentOpen(game, hart.Pos);
        game.Debug_SetPlayerPos(spot);
        game.ApplyKey(BumpKey(spot, hart.Pos)); // the bearer acts first: a kill before the flee.

        Assert.False(hart.Alive);
        Assert.Equal(1, game.Player.Hide);                 // one hide at Hunting 0
        Assert.Equal(1, game.Player.RawMeat);              // raw meat for the pot (D-073), not a ready ration
        Assert.Equal(0, game.Player.Rations);              // the hunt no longer hands over cooked food
        Assert.Equal(essenceBefore, game.Player.Essence);  // game carries no essence
        Assert.Equal(coinBefore, game.Player.Coin);        // and no purse
        Assert.True(game.Player.Skills.Uses(SkillId.Hunting) > huntingBefore, "the hunt taught nothing");
    }

    [Fact]
    public void HuntingSkill_FattensTheHideYield()
    {
        var game = EnterWilds();
        var hart = FindHart(game);
        hart.Hp = 1;
        while (game.Player.Skills.Level(SkillId.Hunting) < 2) game.Player.Skills.AddUse(SkillId.Hunting);
        Assert.Equal(1, game.Player.Skills.Bonus(SkillId.Hunting));
        game.Player.Hide = 0;

        var spot = AdjacentOpen(game, hart.Pos);
        game.Debug_SetPlayerPos(spot);
        game.ApplyKey(BumpKey(spot, hart.Pos));

        Assert.Equal(2, game.Player.Hide); // 1 + the Hunting bonus
    }

    [Fact]
    public void Hart_ThatReachesARun_IsGone_AndLeavesNothing()
    {
        var game = EnterWilds();
        var map = game.CurrentSite!.Map;
        var hart = FindHart(game);

        // A run in the treeline (a walkable border cell), its interior neighbour, and the
        // cell beyond that: stand the hart on the neighbour and the bearer beyond it, so the
        // step away is out through the run. Block the two lateral cells (with spare harts,
        // where they are open) so the run is the one step that gains the hart distance: the
        // flee metric ties lateral and forward, so the run must be made the sole way out.
        var (gap, inside, beyond) = FindRun(map);
        var inward = new Pos(inside.X - gap.X, inside.Y - gap.Y);
        var laterals = (Pos[])[inside.Plus(inward.Y, inward.X), inside.Plus(-inward.Y, -inward.X)];
        var spares = game.Monsters.Where(m => m.Kind == MonsterKind.Hart && m.Alive && m != hart).ToList();
        int s = 0;
        foreach (var lat in laterals)
            if (map.Walkable(lat) && s < spares.Count) spares[s++].Pos = lat;
        foreach (var m in game.Monsters.Where(m => m != hart && m.Alive && !laterals.Contains(m.Pos)
                                                     && (m.Pos == inside || m.Pos == beyond || m.Pos == gap)))
            m.Hp = 0;
        hart.Pos = inside;
        game.Debug_SetPlayerPos(beyond);
        game.Player.Hide = 0;

        game.ApplyKey('.');

        Assert.False(hart.Alive);
        Assert.Equal(gap, hart.Pos);
        Assert.Equal(0, game.Player.Hide); // an escaped hart leaves no hide
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("run in the treeline"));
    }

    [Fact]
    public void TheGlade_Clears_WhenTheGameIsGone_WithoutTheHollowsDeed()
    {
        var game = EnterWilds();
        var harts = game.Monsters.Where(m => m.Kind == MonsterKind.Hart && m.SiteId == "wilds" && m.Alive).ToList();
        // Down all but one out of play, then take the last by hand so the clear runs.
        for (int i = 1; i < harts.Count; i++) harts[i].Hp = 0;
        var last = harts[0];
        last.Hp = 1;
        var spot = AdjacentOpen(game, last.Pos);
        game.Debug_SetPlayerPos(spot);
        game.ApplyKey(BumpKey(spot, last.Pos));

        Assert.True(game.World.WildsSite!.Cleared);
        Assert.False(game.World.Facts.Exists("deed", "severed_laid"), "the wilds must not write the hollow's deed");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("The glade goes still"));
    }

    // ---- helpers ----

    /// <summary>Master 42 crossed once: world 2, tier 2, the first wilds world.</summary>
    private static Game TierTwoGame()
    {
        var game = new Game(42);
        Cross(game);
        Assert.Equal(2, game.Cycle);
        Assert.NotNull(game.World.WildsSite);
        return game;
    }

    private static Game EnterWilds()
    {
        var game = TierTwoGame();
        game.Debug_SetPlayerPos(game.World.WildsSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("wilds", game.CurrentSite!.Id);
        return game;
    }

    private static Monster FindHart(Game game) =>
        game.Monsters.First(m => m.Kind == MonsterKind.Hart && m.SiteId == "wilds" && m.Alive);

    private static Pos AdjacentOpen(Game game, Pos target)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p)) return p;
        }
        throw new InvalidOperationException("no open cell beside the hart");
    }

    /// <summary>A walkable interior cell (no monster) at exactly the given Chebyshev distance from a center.</summary>
    private static Pos WalkableAtChebyshev(Game game, Pos center, int dist)
    {
        var map = game.CurrentSite!.Map;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (p.Chebyshev(center) == dist && map.Walkable(p)
                    && !game.Monsters.Any(m => m.Alive && m.Pos == p))
                    return p;
            }
        throw new InvalidOperationException($"no walkable cell at Chebyshev {dist}");
    }

    /// <summary>A run (walkable border cell), its inward neighbour, and the cell one further in.</summary>
    private static (Pos Gap, Pos Inside, Pos Beyond) FindRun(GameMap map)
    {
        for (int x = 0; x < map.Width; x++)
            foreach (int y in (int[])[0, map.Height - 1])
            {
                var gap = new Pos(x, y);
                if (!map.Walkable(gap)) continue;
                int inward = y == 0 ? 1 : -1;
                var inside = new Pos(x, y + inward);
                var beyond = new Pos(x, y + 2 * inward);
                if (map.Walkable(inside) && map.Walkable(beyond)) return (gap, inside, beyond);
            }
        for (int y = 0; y < map.Height; y++)
            foreach (int x in (int[])[0, map.Width - 1])
            {
                var gap = new Pos(x, y);
                if (!map.Walkable(gap)) continue;
                int inward = x == 0 ? 1 : -1;
                var inside = new Pos(x + inward, y);
                var beyond = new Pos(x + 2 * inward, y);
                if (map.Walkable(inside) && map.Walkable(beyond)) return (gap, inside, beyond);
            }
        throw new InvalidOperationException("no run found in the treeline");
    }

    private static char BumpKey(Pos from, Pos to) => (Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y)) switch
    {
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => '.',
    };

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
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
                var n = p.Plus(dx, dy);
                if ((map.Walkable(n) || n == to) && seen.Add(n)) queue.Enqueue(n);
            }
        }
        return false;
    }
}
