using Aegis.Core;

namespace Aegis.Core.Tests;

public class BarrowTests
{
    [Fact]
    public void TierBands_Tier1HasNoBarrow_Tier2PlusHasOne()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var t1 = WorldGen.Generate(seed);
            Assert.Null(t1.BarrowSite);
            // A first world's valley holds the camp, the songhall (D-054), and
            // the harrow (D-114), and no den deeper; the road and the fells
            // keep their own sites off the valley's count (D-138, D-146).
            Assert.Equal(3, t1.Sites.Count(s => s.Area == Area.Valley));
            Assert.False(t1.Facts.Exists("site", "barrow"));

            var t2 = WorldGen.Generate(seed, tier: 2);
            var barrow = t2.BarrowSite;
            Assert.NotNull(barrow);
            Assert.Equal(Terrain.BarrowEntrance, t2.Overworld[barrow.OverworldPos]);
            Assert.NotEqual(barrow.OverworldPos, t2.CampPos);
            Assert.NotEqual(barrow.OverworldPos, t2.GatePos);
            Assert.True(Reachable(t2.Overworld, t2.ShrinePos, barrow.OverworldPos),
                $"seed {seed}: barrow unreachable from shrine");
            Assert.True(t2.Facts.Exists("site", "barrow"));

            // The band scales as a generation input: count and stats, never a multiplier.
            Assert.Equal(2, barrow.Spawns.Count);
            Assert.All(barrow.Spawns, s => Assert.Equal(MonsterKind.Wight, s.Kind));
            Assert.All(barrow.Spawns, s => Assert.Equal(12, s.Hp));

            var t5 = WorldGen.Generate(seed, tier: 5);
            Assert.Equal(5, t5.BarrowSite!.Spawns.Count);
            Assert.All(t5.BarrowSite.Spawns, s => Assert.Equal(18, s.Hp));
        }
    }

    [Fact]
    public void BarrowLayout_IsDeterministic_AndChestReachableFromEntry()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var a = WorldGen.Generate(seed, tier: 2).BarrowSite!;
            var b = WorldGen.Generate(seed, tier: 2).BarrowSite!;
            Assert.Equal(a.Map.ContentHash(), b.Map.ContentHash());
            Assert.Equal(a.Spawns, b.Spawns);
            Assert.Equal(a.ChestPos, b.ChestPos);

            Assert.Equal(Terrain.ExitLadder, a.Map[a.EntryPos]);
            Assert.True(Reachable(a.Map, a.EntryPos, a.ChestPos), $"seed {seed}: grave goods unreachable");
            Assert.All(a.Spawns, s => Assert.True(a.Map.Walkable(s.Pos), $"seed {seed}: wight in a wall"));
            Assert.Equal(a.Spawns.Count, a.Spawns.Select(s => s.Pos).Distinct().Count());
        }
    }

    [Fact]
    public void Wights_AreGraveSlow_SteppingOnlyOnEvenTurns()
    {
        var game = CrossedGame(42);
        var barrow = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal(MapMode.Site, game.Mode);

        List<Pos> WightPositions() =>
            game.Monsters.Where(m => m.Kind == MonsterKind.Wight && m.Alive).Select(m => m.Pos).ToList();

        // Approach a chamber and watch from two tiles out; wights path unerringly
        // through their own halls, but only every other turn.
        var quarry = game.Monsters.First(m => m.Kind == MonsterKind.Wight && m.Alive);
        for (int i = 0; i < 60 && WightPositions().All(p => p.Chebyshev(game.Player.Pos) > 2); i++)
        {
            char? key = StepToward(game, barrow.Map, quarry.Pos,
                p => barrow.Map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p));
            game.ApplyKey(key ?? '.'); // a wight in the passage blocks the path: stand and let it come
        }
        Assert.True(WightPositions().Any(p => p.Chebyshev(game.Player.Pos) <= 2), "never reached a chamber mouth");

        bool anyMove = false;
        for (int i = 0; i < 16 && game.Mode == MapMode.Site; i++)
        {
            var before = WightPositions();

            // Kite when grasped at: step to the neighbor farthest from the pack. A slow
            // pursuer that can be stepped away from is the family's whole tactical point.
            char key = '.';
            if (before.Any(p => p.Chebyshev(game.Player.Pos) == 1))
            {
                var options = Directions.All8
                    .Select(d => game.Player.Pos.Plus(d.dx, d.dy))
                    .Where(p => barrow.Map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
                    .ToList();
                if (options.Count > 0)
                {
                    var flee = options.OrderByDescending(p => before.Min(w => w.Chebyshev(p))).First();
                    key = KeyFor(flee.X - game.Player.Pos.X, flee.Y - game.Player.Pos.Y);
                }
            }

            game.ApplyKey(key);
            var after = WightPositions();
            if (game.Turn % 2 == 1)
                Assert.Equal(before, after); // odd turns: the dead hold their ground
            else if (!before.SequenceEqual(after))
                anyMove = true;
        }
        Assert.True(anyMove, "no wight ever stepped; the watch proved nothing");
    }

    [Fact]
    public void ClearingBarrow_WritesDeed_ButOnlyTheCampDeedOpensTheGate()
    {
        var game = CrossedGame(42);
        Assert.NotNull(game.World.BarrowSite);

        game.Debug_ClearSite(SiteKind.Barrow);
        Assert.True(game.World.BarrowSite!.Cleared);
        Assert.True(game.World.Facts.Exists("deed", "barrow_stilled"));
        Assert.Contains(game.Log.Recent(5), e => e.Text.Contains("no one is holding it now"));

        // The waygate answers to the camp deed alone; the barrow is optional depth.
        Assert.False(game.CampCleared);
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        game.Debug_ClearCamp();
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(3, game.Cycle);
    }

    [Fact]
    public void StillnessRepaid_FiresNearHouses_AfterTheBarrowDeed()
    {
        var game = CrossedGame(42);
        game.Debug_ClearSite(SiteKind.Barrow);

        var (a, b) = FindHouseAdjacentPair(game);
        for (int i = 0; i < 30 && !game.World.Facts.Exists("boon", "grave_token"); i++)
        {
            game.Debug_SetPlayerPos(i % 2 == 0 ? a : b);
            StepBetween(game, i % 2 == 0 ? b : a);
        }

        Assert.True(game.World.Facts.Exists("boon", "grave_token"));
        Assert.Contains(game.Log.Recent(80), e => e.Text.Contains("bent silver pin"));
    }

    [Fact]
    public void GraveGoods_AreTheirOwnChest_IndependentOfTheCamp()
    {
        var game = CrossedGame(42);
        var barrow = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow.OverworldPos);
        game.Apply(Command.Enter);

        int coinBefore = game.Player.Coin;
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);

        Assert.True(barrow.ChestLooted);
        Assert.False(game.World.CampSite.ChestLooted);
        Assert.InRange(game.Player.Coin - coinBefore, 15, 26);
        // The grab also hands out the grave-iron blade (D-041) and, taken from
        // under walking dead, starts the mound's grudge (D-106): wider window.
        Assert.Contains(game.Log.Recent(12), e => e.Text.Contains("Grave-gold"));
    }

    /// <summary>
    /// The save contract through the new content: a key-driven bot crosses and raids
    /// the barrow, and its whole journal must replay bit-identically (D-028). Victory
    /// is not required, only real wight combat: deaths are journaled state like any other.
    /// </summary>
    [Fact]
    public void KeyDrivenBarrowRaid_JournalReplaysIdentically()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            // The parity proof runs through the real wake (D-092): fate answers the asking.
            var game = new Game(seed, firstWake: true);
            var journal = new List<char>();
            game.KeyApplied += journal.Add;
            game.ApplyKey('0');

            for (int i = 0; i < 4000 && game.Running; i++)
            {
                if (game.Cycle >= 2 && (game.World.BarrowSite?.Cleared ?? false)) break;
                char? key = NextBotKey(game);
                if (key is null) break;
                game.ApplyKey(key.Value);
            }

            bool foughtWights = game.Cycle >= 2 && game.Monsters.Any(m => m.Kind == MonsterKind.Wight && m.Hp < 12);
            if (!foughtWights) continue;

            var replayed = SaveCodec.Replay(seed, new string(journal.ToArray()));

            Assert.Equal(game.Cycle, replayed.Cycle);
            Assert.Equal(game.World.BarrowSite!.Cleared, replayed.World.BarrowSite!.Cleared);
            Assert.Equal(game.Player.Pos, replayed.Player.Pos);
            Assert.Equal(game.Player.Hp, replayed.Player.Hp);
            Assert.Equal(game.Player.Essence, replayed.Player.Essence);
            Assert.Equal(game.Player.Deaths, replayed.Player.Deaths);
            Assert.Equal(game.Turn, replayed.Turn);
            Assert.Equal(game.World.Facts.All.Count, replayed.World.Facts.All.Count);
            Assert.Equal(
                game.Monsters.Select(m => (m.Kind, m.Pos, m.Hp)),
                replayed.Monsters.Select(m => (m.Kind, m.Pos, m.Hp)));
            return; // one real key-driven raid is the proof
        }

        Assert.Fail("No seed in 1..20 got the bot into wight combat within the key budget.");
    }

    // --- helpers ---

    /// <summary>A game standing at the start of its second world (tier 2).</summary>
    private static Game CrossedGame(ulong seed)
    {
        var game = new Game(seed);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        return game;
    }

    private static (Pos A, Pos B) FindHouseAdjacentPair(Game game)
    {
        var map = game.World.Overworld;
        bool NearHouse(Pos p) => Directions.All8.Any(d =>
            map.InBounds(p.Plus(d.dx, d.dy)) && map[p.Plus(d.dx, d.dy)] == Terrain.House);
        bool Free(Pos p) => map.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p);

        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var a = new Pos(x, y);
                if (!Free(a) || !NearHouse(a)) continue;
                foreach (var (dx, dy) in Directions.Cardinal)
                {
                    var b = a.Plus(dx, dy);
                    if (Free(b) && NearHouse(b)) return (a, b);
                }
            }
        throw new InvalidOperationException("no adjacent pair of house-side tiles");
    }

    private static void StepBetween(Game game, Pos target)
    {
        var d = (target.X - game.Player.Pos.X, target.Y - game.Player.Pos.Y);
        game.ApplyKey(KeyFor(d.Item1, d.Item2));
    }

    /// <summary>Greedy bot: clear the camp, cross, then raid the barrow. Deaths are setbacks.</summary>
    private static char? NextBotKey(Game game)
    {
        if (game.InShrineMenu || game.InTalkMenu) return ' ';

        if (game.Mode == MapMode.Site)
        {
            var site = game.CurrentSite!;
            var target = game.Monsters
                .Where(m => m.Alive && m.SiteId == site.Id)
                .OrderBy(m => m.Pos.Manhattan(game.Player.Pos)).FirstOrDefault();
            if (target is null)
            {
                if (site.Map[game.Player.Pos] == Terrain.ExitLadder) return '<';
                return StepToward(game, site.Map, site.EntryPos, p => site.Map.Walkable(p));
            }
            if (target.Pos.Chebyshev(game.Player.Pos) == 1)
                return KeyFor(target.Pos.X - game.Player.Pos.X, target.Pos.Y - game.Player.Pos.Y);
            return StepToward(game, site.Map, target.Pos,
                p => site.Map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.SiteId == site.Id && m.Pos == p && m != target));
        }

        Pos goal = game.Cycle == 1
            ? (!game.CampCleared ? game.World.CampPos : game.World.GatePos)
            : game.World.BarrowSite!.OverworldPos;
        if (game.Player.Pos == goal) return '>';
        return StepToward(game, game.World.Overworld, goal,
            p => game.World.Overworld.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
    }

    private static char? StepToward(Game game, GameMap map, Pos goal, Func<Pos, bool> passable)
    {
        var from = game.Player.Pos;
        var cameFrom = new Dictionary<Pos, Pos> { [from] = from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == goal) break;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                if ((passable(next) || next == goal) && map.InBounds(next) && !cameFrom.ContainsKey(next))
                {
                    cameFrom[next] = p;
                    queue.Enqueue(next);
                }
            }
        }
        if (!cameFrom.ContainsKey(goal)) return null;

        var step = goal;
        while (cameFrom[step] != from) step = cameFrom[step];
        return KeyFor(step.X - from.X, step.Y - from.Y);
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
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

    private static bool Reachable(GameMap map, Pos from, Pos to)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == to) return true;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                if (map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }
}
