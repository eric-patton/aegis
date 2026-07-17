using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The arc ladder, rungs 2 and 3 (D-037, design/story/aegis-arc.md sec 6): the
/// hollow and its tenant, the flag-gated crossing scenes, the shrine vision, and
/// the Unbinder's first reveal tier. Rungs gate on flags, never cycle counts.
/// </summary>
public class ArcTests
{
    [Fact]
    public void Hollow_ExistsAtTierTwoPlus_Reachable_WithTenantAndRumors()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.Null(WorldGen.Generate(seed, tier: 1).HollowSite);

            foreach (int tier in (int[])[2, 4])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var hollow = a.HollowSite;
                Assert.NotNull(hollow);
                Assert.Equal(Terrain.HollowEntrance, a.Overworld[hollow!.OverworldPos]);
                Assert.Equal(hollow.OverworldPos, b.HollowSite!.OverworldPos);
                Assert.True(Reachable(a.Overworld, a.ShrinePos, hollow.OverworldPos),
                    $"seed {seed} tier {tier}: hollow unreachable");

                var spawn = Assert.Single(hollow.Spawns);
                Assert.Equal(MonsterKind.Severed, spawn.Kind);
                Assert.Equal(16 + 2 * (tier - 2), spawn.Hp);

                Assert.True(a.Facts.Exists("site", "hollow"));
                Assert.True(a.Facts.OfType("bearer_myth").Any());
            }
        }
    }

    [Fact]
    public void SeveredFight_PaysEssence_WritesDeed_AndTheTruthLandsOnceEver()
    {
        var game = CrossedGame(42);
        Assert.Equal(2, game.Cycle);

        EnterHollow(game);
        int essenceBefore = game.Player.Essence;
        FellSevered(game);

        Assert.True(game.World.HollowSite!.Cleared);
        Assert.True(game.World.Facts.Exists("deed", "severed_laid"));
        Assert.Equal(essenceBefore + 15, game.Player.Essence);
        Assert.True(game.Player.SeveredTruthHeard);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("grip on the old shield")));

        // A later world's tenant falls without re-paying the once-ever reveal.
        Cross(game);
        EnterHollow(game);
        FellSevered(game);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("grip on the old shield")));
    }

    [Fact]
    public void CrossingScenes_ClimbTheLadder_GuiltThenLedger_EachOnce()
    {
        var game = CrossedGame(42);
        EnterHollow(game);
        FellSevered(game);

        // Crossing after the truth: the guilt scene, once.
        Cross(game);
        Assert.True(game.Player.CrossingGuiltHeard);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("stranger-kind are bearers")));
        Assert.False(game.Player.LedgerHeard);

        // The vision at the next shrine rest, then the ledger at the next crossing.
        game.ApplyKey('r');
        Assert.True(game.Player.VisionSeen);
        game.ApplyKey(' ');

        Cross(game);
        Assert.True(game.Player.LedgerHeard);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("counted, and tithed")));

        // A further crossing repeats neither rung.
        Cross(game);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("stranger-kind are bearers")));
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("counted, and tithed")));
    }

    [Fact]
    public void UnbinderTierOne_UnlocksAfterTheVision_AndItsTopic()
    {
        var game = CrossedGame(42);

        // Before the vision, the confrontation cannot fire.
        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Equal(0, game.Player.UnbinderRevealTier);
        Assert.DoesNotContain(game.Topics, t => t.Label == "The long road");
        game.ApplyKey(' ');

        EnterHollow(game);
        FellSevered(game);
        Cross(game);
        game.ApplyKey('r');
        Assert.True(game.Player.VisionSeen);
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Equal(1, game.Player.UnbinderRevealTier);
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Contains(game.Topics, t => t.Label == "The long road");
        game.ApplyKey(' ');
    }

    [Fact]
    public void Ladder_Stalls_WhenTheHollowIsLeftAlone()
    {
        var game = CrossedGame(42);
        Cross(game);
        Cross(game);
        Assert.Equal(4, game.Cycle);

        Assert.False(game.Player.SeveredTruthHeard);
        Assert.False(game.Player.CrossingGuiltHeard);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("stranger-kind are bearers"));

        game.ApplyKey('r');
        Assert.False(game.Player.VisionSeen);
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Equal(0, game.Player.UnbinderRevealTier);
    }

    [Fact]
    public void StrangerOnTheRoad_PassesOnceInWorldOne_Only()
    {
        var game = new Game(42);
        for (int i = 0; i < 250 && !Fired(game, "little shield"); i++) game.ApplyKey('.');
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("little shield")));

        // Never again, in this world or any other.
        for (int i = 0; i < 250; i++) game.ApplyKey('.');
        Cross(game);
        for (int i = 0; i < 250; i++) game.ApplyKey('.');
        Assert.Equal(1, game.Log.Entries.Count(e =>
            e.Text.Contains("little shield") && e.Text.Contains("road")));
    }

    [Fact]
    public void ForgeName_ReturnsAtTheSecondWorld_Once()
    {
        var game = new Game(42);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains(AegisVoice.ForgeName));

        Cross(game);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains(AegisVoice.ForgeName)));

        Cross(game);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains(AegisVoice.ForgeName)));
    }

    // ---- rung 4 (D-038): the argument ----

    [Fact]
    public void Hermit_ExistsAtTierThreePlus_Reachable_AndDeterministic()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.Null(WorldGen.Generate(seed, tier: 1).SeveredNpc);
            Assert.Null(WorldGen.Generate(seed, tier: 2).SeveredNpc);

            foreach (int tier in (int[])[3, 5])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var calm = a.SeveredNpc;
                Assert.NotNull(calm);
                Assert.Equal(NpcKind.Severed, calm!.Kind);
                Assert.Equal(calm.Pos, b.SeveredNpc!.Pos);
                Assert.True(a.Overworld.Walkable(calm.Pos), $"seed {seed} tier {tier}: hermit on unwalkable ground");
                Assert.True(Reachable(a.Overworld, a.ShrinePos, calm.Pos), $"seed {seed} tier {tier}: hermit unreachable");
                Assert.True(a.Facts.Exists("person", "npc_severed_calm"));
            }
        }
    }

    [Fact]
    public void PeaceBeat_WaitsForTheLedger()
    {
        var game = CrossedGame(42);
        Cross(game);
        Cross(game); // world 4, tier 4: the hermit is there, but no rung is ready.

        NpcTests.BumpNpc(game, game.World.SeveredNpc!);
        Assert.False(game.Player.SeveredPeaceHeard);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("both eyes open"));
        Assert.Contains(game.Topics, t => t.Label == "Their peace");
        Assert.DoesNotContain(game.Topics, t => t.Label == "The cutting");
        game.ApplyKey(' ');
    }

    [Fact]
    public void PeaceBeat_LandsOnce_AndUnlocksTheCuttingTopic()
    {
        var game = LedgeredGame();

        NpcTests.BumpNpc(game, game.World.SeveredNpc!);
        Assert.True(game.Player.SeveredPeaceHeard);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("both eyes open")));
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, game.World.SeveredNpc!);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("both eyes open")));
        Assert.Contains(game.Topics, t => t.Label == "The cutting");
        game.ApplyKey(' ');
    }

    [Fact]
    public void CostBeat_IsWitnessedFromTheThreshold_WithoutAFight()
    {
        // Before the ledger, the threshold stones are only stones.
        var early = CrossedGame(42);
        StepOnto(early, early.World.HollowSite!.OverworldPos);
        Assert.False(early.Player.SeveredCostSeen);

        var game = LedgeredGame();
        StepOnto(game, game.World.HollowSite!.OverworldPos);
        Assert.True(game.Player.SeveredCostSeen);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("two bowls")));

        // The beat completed with the keeper alive: no fight was ever required.
        Assert.False(game.World.HollowSite!.Cleared);

        StepOnto(game, game.World.HollowSite!.OverworldPos);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("two bowls")));
    }

    [Fact]
    public void FirstBearer_RequiresBothWitnesses_OnTopOfTierOne()
    {
        var game = LedgeredGame();

        // First conversation since the vision: tier 1, never tier 2.
        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Equal(1, game.Player.UnbinderRevealTier);
        game.ApplyKey(' ');

        // One witness is not enough.
        NpcTests.BumpNpc(game, game.World.SeveredNpc!);
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Equal(1, game.Player.UnbinderRevealTier);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("proudest thing"));
        game.ApplyKey(' ');

        // Both witnesses: the confrontation lands, once, and its topic persists.
        StepOnto(game, game.World.HollowSite!.OverworldPos);
        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Equal(2, game.Player.UnbinderRevealTier);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("proudest thing")));
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Contains(game.Topics, t => t.Label == "The refusal");
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("proudest thing")));
        game.ApplyKey(' ');
    }

    [Fact]
    public void Commission_SpeaksAtTheNextCrossing_Once()
    {
        var game = LedgeredGame();
        NpcTests.BumpNpc(game, game.World.Unbinder);
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, game.World.SeveredNpc!);
        game.ApplyKey(' ');
        StepOnto(game, game.World.HollowSite!.OverworldPos);
        NpcTests.BumpNpc(game, game.World.Unbinder);
        game.ApplyKey(' ');
        Assert.Equal(2, game.Player.UnbinderRevealTier);
        Assert.False(game.Player.CommissionHeard);

        Cross(game);
        Assert.True(game.Player.CommissionHeard);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("carry you either way")));

        Cross(game);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("carry you either way")));

        Assert.Equal("truth,guilt,vision,ledger,peace,cost,tier2,commission",
            game.TakeSnapshot().ArcProgress);
    }

    [Fact]
    public void Presenter_MarksTheHermitApart_AndHeadsTheirMenu()
    {
        var game = CrossedGame(42);
        Cross(game); // world 3, tier 3

        var hermit = game.World.SeveredNpc!;
        NpcTests.BumpNpc(game, hermit); // stands the player beside them, opens the menu
        game.ApplyKey(' ');

        var frame = Presenter.Render(game);
        bool magentaP = false;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (frame[x, y] is { Ch: 'p', Fg: Hue.Magenta }) magentaP = true;
        Assert.True(magentaP, "hermit not drawn in their own hue beside the player");

        NpcTests.BumpNpc(game, hermit);
        var menu = Presenter.Render(game).ToTextLines();
        Assert.Contains(menu, line => line.Contains($"{hermit.Name}, hermit of no stead at all"));
        game.ApplyKey(' ');
    }

    // ---- helpers ----

    /// <summary>A game one crossing in: master 42's world 2, tier 2.</summary>
    private static Game CrossedGame(ulong master)
    {
        var game = new Game(master);
        Cross(game);
        return game;
    }

    /// <summary>Master 42 walked to the ledger: world 4, tier 4, flags truth/guilt/vision/ledger.</summary>
    private static Game LedgeredGame()
    {
        var game = CrossedGame(42);
        EnterHollow(game);
        FellSevered(game);   // truth
        Cross(game);         // guilt
        game.ApplyKey('r');  // vision, at the arrival shrine
        game.ApplyKey(' ');
        Cross(game);         // ledger
        return game;
    }

    /// <summary>Walks onto an overworld tile for real, so EnterTile storylets fire.</summary>
    private static void StepOnto(Game game, Pos target)
    {
        game.Debug_SetMode(MapMode.Overworld);
        foreach (var (dx, dy, key) in ((int, int, char)[])[(-1, 0, 'l'), (1, 0, 'h'), (0, -1, 'j'), (0, 1, 'k')])
        {
            var from = target.Plus(dx, dy);
            if (!game.World.Overworld.Walkable(from) || game.World.Npcs.Any(n => n.Pos == from)) continue;
            game.Debug_SetPlayerPos(from);
            game.ApplyKey(key);
            if (game.Player.Pos == target) return;
        }
        Assert.Fail($"no walkable approach to {target}");
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }

    private static void EnterHollow(Game game)
    {
        game.Debug_SetPlayerPos(game.World.HollowSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("hollow", game.CurrentSite!.Id);
    }

    /// <summary>Weakens the tenant to a sliver and lands the last blow for real.</summary>
    private static void FellSevered(Game game)
    {
        var severed = game.Monsters.First(m => m.Kind == MonsterKind.Severed && m.SiteId == "hollow" && m.Alive);
        severed.Hp = 1;
        game.Debug_SetPlayerPos(severed.Pos.Plus(-1, 0));
        game.ApplyKey('l');
        Assert.False(severed.Alive);
    }

    private static bool Fired(Game game, string marker) =>
        game.Log.Entries.Any(e => e.Text.Contains(marker));

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
