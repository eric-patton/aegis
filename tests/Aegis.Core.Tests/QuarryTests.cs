using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The deep band (D-040): the old quarry at tier 3+, the graven men (dormant until
/// seen, artillery awake, stone-slow afoot), line of sight and the pillars that
/// break it, the quarry deed, and template repeat-weighting.
/// </summary>
public class QuarryTests
{
    [Fact]
    public void Quarry_ExistsAtTierThreePlus_Deterministic_AndReachable()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.Null(WorldGen.Generate(seed, tier: 2).QuarrySite);

            foreach (int tier in (int[])[3, 6])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var quarry = a.QuarrySite;
                Assert.NotNull(quarry);
                Assert.Equal(Terrain.QuarryEntrance, a.Overworld[quarry!.OverworldPos]);
                Assert.Equal(quarry.OverworldPos, b.QuarrySite!.OverworldPos);
                Assert.True(Reachable(a.Overworld, a.ShrinePos, quarry.OverworldPos),
                    $"seed {seed} tier {tier}: quarry unreachable");

                Assert.Equal(Math.Min(3 + (tier - 3), 5), quarry.Spawns.Count);
                Assert.All(quarry.Spawns, s => Assert.Equal(MonsterKind.Graven, s.Kind));
                Assert.All(quarry.Spawns, s => Assert.Equal(18 + 2 * (tier - 3), s.Hp));

                // The open pit: chest and every sentinel stand where feet can reach,
                // despite the pillars (which, by the placement rule, never touch).
                Assert.True(Reachable(quarry.Map, quarry.EntryPos, quarry.ChestPos),
                    $"seed {seed} tier {tier}: toolcache unreachable");
                foreach (var s in quarry.Spawns)
                    Assert.True(Reachable(quarry.Map, quarry.EntryPos, s.Pos),
                        $"seed {seed} tier {tier}: a graven man is walled in");
                Assert.True(a.Facts.Exists("site", "quarry"));
            }
        }
    }

    [Fact]
    public void GravenMen_StandAsStatues_UntilSeenUpClose_OrStruck()
    {
        var game = EnterQuarry();
        var graven = game.Monsters.Where(m => m.Kind == MonsterKind.Graven).ToList();
        Assert.All(graven, m => Assert.True(m.Dormant));

        // From the entry ladder, nothing in the pit is near enough to notice you.
        var posBefore = graven.Select(m => m.Pos).ToList();
        for (int i = 0; i < 5; i++) game.ApplyKey('.');
        Assert.All(graven, m => Assert.True(m.Dormant));
        Assert.Equal(posBefore, graven.Select(m => m.Pos).ToList());
        Assert.All(graven, m => Assert.Null(m.Intent));

        // Stand beside one: it wakes, and then it is a foe like any other.
        var mark = graven[0];
        game.Debug_SetPlayerPos(AdjacentOpen(game, mark.Pos));
        game.ApplyKey('.');
        Assert.False(mark.Dormant);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the head grinds"));

        // Striking a sleeper wakes it too: stone remembers being hit.
        var second = graven.First(m => m.Dormant);
        game.Debug_SetPlayerPos(AdjacentOpen(game, second.Pos));
        StepInto(game, second.Pos);
        Assert.False(second.Dormant);
    }

    [Fact]
    public void HurledStone_TelegraphsTheCell_MissesTheSidestep_AndLandsOnTheStander()
    {
        var game = EnterQuarry();
        var mark = game.Monsters.First(m => m.Kind == MonsterKind.Graven);
        foreach (var other in game.Monsters.Where(m => m.Kind == MonsterKind.Graven && m != mark))
            other.Hp = 0;

        // Stand in its sight at throwing range and wait for the wind-up.
        game.Debug_SetPlayerPos(OpenCellInSight(game, mark.Pos, distance: 4));
        for (int i = 0; i < 30 && mark.Intent is null; i++) game.ApplyKey('.');
        Assert.NotNull(mark.Intent);
        Assert.Equal(IntentKind.HurledStone, mark.Intent!.Kind);
        Assert.Equal(game.Player.Pos, mark.Intent.TargetCell);
        Assert.False(mark.Dormant);

        // The stone lands on the cell, not the bearer: step off it and live whole.
        int hpBefore = game.Player.Hp;
        StepAnywhereElse(game, mark);
        Assert.Equal(hpBefore, game.Player.Hp);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("bursts on the floor where you stood"));

        // Stand on the telegraph instead and take it square.
        for (int i = 0; i < 30 && mark.Intent is null; i++) game.ApplyKey('.');
        Assert.NotNull(mark.Intent);
        hpBefore = game.Player.Hp;
        game.ApplyKey('.');
        Assert.True(game.Player.Hp < hpBefore, "the hurled stone landed on a stander and did nothing");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("takes you square"));
    }

    [Fact]
    public void Pillars_BreakTheThrowingSight()
    {
        var map = new GameMap("los-test", 9, 5, Terrain.Floor);
        map[new Pos(4, 2)] = Terrain.Wall;

        Assert.True(map.LineOfSight(new Pos(2, 1), new Pos(6, 1)));
        Assert.False(map.LineOfSight(new Pos(2, 2), new Pos(6, 2)));
        Assert.False(map.LineOfSight(new Pos(6, 2), new Pos(2, 2)));
        // Endpoints do not block themselves: adjacent to the pillar still sees past nothing.
        Assert.True(map.LineOfSight(new Pos(3, 2), new Pos(4, 1)));

        // And the quarry actually holds freestanding cover to hide behind.
        var quarry = WorldGen.Generate(11, tier: 3).QuarrySite!;
        bool pillar = false;
        for (int y = 2; y < WorldGen.QuarryH - 2 && !pillar; y++)
            for (int x = 2; x < WorldGen.QuarryW - 2 && !pillar; x++)
                if (quarry.Map[new Pos(x, y)] == Terrain.Wall) pillar = true;
        Assert.True(pillar, "the pit generated with no cover at all");
    }

    [Fact]
    public void TheDownedTools_SpeakOnceDeepInThePit()
    {
        var game = EnterQuarry();
        game.Debug_SetPlayerPos(new Pos(13, 8));
        game.ApplyKey('l');
        Assert.Equal(1, Count(game, "between one chisel-blow and the next"));

        // Once per world, like the barrow's shadow.
        game.ApplyKey('h');
        game.ApplyKey('l');
        Assert.Equal(1, Count(game, "between one chisel-blow and the next"));
    }

    [Fact]
    public void ClearingTheQuarry_WritesTheDeed_AndTheSteadAnswers()
    {
        var game = EnterQuarry();
        game.Apply(Command.Exit);
        game.Debug_ClearSite(SiteKind.Quarry);

        Assert.True(game.World.QuarrySite!.Cleared);
        Assert.True(game.World.Facts.Exists("deed", "quarry_hushed"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("never told to stop"));

        // NearHouse fires one candidate per visit; keep visiting until the news lands.
        for (int i = 0; i < 12 && !game.Log.Entries.Any(e => e.Text.Contains("old pit are down")); i++)
            StepNearHouse(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the figures in the old pit are down"));
    }

    [Fact]
    public void StorySelection_WeighsAgainstRepeats_AndOnlyWhenTheyAreInTheRunning()
    {
        int repeatWithPrev = 0, repeatWithoutPrev = 0;
        for (ulong seed = 1; seed <= 60; seed++)
        {
            ulong worldSeed = SeedTree.Derive(seed, "weighting-scan");

            // A previous story outside the eligible pool changes nothing at all.
            var baseline = WorldGen.Generate(worldSeed, tier: 2);
            var unaffected = WorldGen.Generate(worldSeed, tier: 2, prevStory: "no-such-template");
            string baselineStory = baseline.Facts.OfType("story").Single().Subject;
            Assert.Equal(baselineStory, unaffected.Facts.OfType("story").Single().Subject);

            // Determinism: the weighted draw is a pure function of its inputs.
            var a = WorldGen.Generate(worldSeed, tier: 2, prevStory: CreepingBlightTemplate.Id);
            var b = WorldGen.Generate(worldSeed, tier: 2, prevStory: CreepingBlightTemplate.Id);
            string weighted = a.Facts.OfType("story").Single().Subject;
            Assert.Equal(weighted, b.Facts.OfType("story").Single().Subject);

            if (weighted == CreepingBlightTemplate.Id) repeatWithPrev++;
            if (baselineStory == CreepingBlightTemplate.Id) repeatWithoutPrev++;
        }

        // Halved weight in a two-template pool: repeats near one third, not one half.
        Assert.True(repeatWithPrev < repeatWithoutPrev,
            $"weighting changed nothing: {repeatWithPrev} repeats vs {repeatWithoutPrev} baseline");
        Assert.InRange(repeatWithPrev, 8, 30);
    }

    [Fact]
    public void Crossing_HandsThePreviousStory_ToTheNextDraw()
    {
        // Master 43: world 2 tells the stead (pinned in TemplateTests). World 3's
        // draw must then be exactly what worldgen produces given that history.
        var game = new Game(43);
        Cross(game);
        string world2Story = game.TakeSnapshot().StoryTemplate;
        Assert.Equal(RaidedSteadTemplate.Id, world2Story);

        Cross(game);
        var expected = WorldGen.Generate(SeedTree.Derive(43, "cycle", 3), tier: 3, prevStory: world2Story);
        Assert.Equal(expected.Facts.OfType("story").Single().Subject, game.TakeSnapshot().StoryTemplate);
    }

    [Fact]
    public void Presenter_DrawsThePit_TheSleepers_AndTheSidebar()
    {
        var game = TierThreeGame();
        game.Debug_SetPlayerPos(game.World.QuarrySite!.OverworldPos.Plus(-1, 0));
        var overworld = Presenter.Render(game);
        bool rim = false;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (overworld[x, y] is { Ch: 'x', Fg: Hue.DarkYellow }) rim = true;
        Assert.True(rim, "the quarry rim is not drawn on the overworld");

        game.Debug_SetPlayerPos(game.World.QuarrySite.OverworldPos);
        game.Apply(Command.Enter);
        var inside = Presenter.Render(game);
        Assert.Contains(inside.ToTextLines(), line => line.Contains("The old quarry"));

        bool sleeper = false;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (inside[x, y] is { Ch: 'm', Fg: Hue.DarkGray }) sleeper = true;
        Assert.True(sleeper, "no sleeping graven man drawn in stone-gray");
    }

    // ---- helpers ----

    private static int Count(Game game, string marker) =>
        game.Log.Entries.Count(e => e.Text.Contains(marker));

    /// <summary>Master 42 crossed twice: world 3, tier 3, the first quarry world.</summary>
    private static Game TierThreeGame()
    {
        var game = new Game(42);
        Cross(game);
        Cross(game);
        Assert.Equal(3, game.Cycle);
        Assert.NotNull(game.World.QuarrySite);
        return game;
    }

    private static Game EnterQuarry()
    {
        var game = TierThreeGame();
        game.Debug_SetPlayerPos(game.World.QuarrySite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("quarry", game.CurrentSite!.Id);
        return game;
    }

    /// <summary>A walkable cell beside a target, free of other monsters.</summary>
    private static Pos AdjacentOpen(Game game, Pos target)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p)) return p;
        }
        Assert.Fail($"no open cell beside {target}");
        return default;
    }

    /// <summary>A walkable cell at a Chebyshev distance with clear sight of the mark.</summary>
    private static Pos OpenCellInSight(Game game, Pos mark, int distance)
    {
        var map = game.CurrentSite!.Map;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (p.Chebyshev(mark) != distance || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (map.LineOfSight(mark, p)) return p;
            }
        Assert.Fail($"no open cell in sight of {mark} at distance {distance}");
        return default;
    }

    private static void StepInto(Game game, Pos target)
    {
        int dx = Math.Sign(target.X - game.Player.Pos.X), dy = Math.Sign(target.Y - game.Player.Pos.Y);
        char key = (dx, dy) switch
        {
            (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', (0, 1) => 'j',
            (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', _ => 'n',
        };
        game.ApplyKey(key);
    }

    private static void StepAnywhereElse(Game game, Monster mark)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = game.Player.Pos.Plus(dx, dy);
            if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
            StepInto(game, p);
            Assert.Equal(p, game.Player.Pos);
            return;
        }
        Assert.Fail("nowhere to sidestep");
    }

    private static void StepNearHouse(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        var map = game.World.Overworld;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.World.Npcs.Any(n => n.Pos == p)) continue;
                if (!Directions.All8.Any(d => map.InBounds(p.Plus(d.dx, d.dy)) && map[p.Plus(d.dx, d.dy)] == Terrain.House)) continue;
                foreach (var (dx, dy, key) in ((int, int, char)[])[(-1, 0, 'l'), (1, 0, 'h'), (0, -1, 'j'), (0, 1, 'k')])
                {
                    var from = p.Plus(dx, dy);
                    if (!map.Walkable(from) || game.World.Npcs.Any(n => n.Pos == from)) continue;
                    game.Debug_SetPlayerPos(from);
                    game.ApplyKey(key);
                    if (game.Player.Pos == p) return;
                }
            }
        Assert.Fail("no walkable tile beside a house");
    }

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
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                if (map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }
}
