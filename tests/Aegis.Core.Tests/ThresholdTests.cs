using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The threshold (D-039, design/story/aegis-arc.md sec 6 cycle 5 and sec 8): the
/// last stair at tier 5+, the door that answers to flags, the staged approach,
/// the keeping's choice, the mechanics-identical guardrail, the register swaps,
/// and the mandated local epilogue.
/// </summary>
public class ThresholdTests
{
    [Fact]
    public void LastStair_ExistsAtTierFivePlus_Deterministic_AndReachable()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.Null(WorldGen.Generate(seed, tier: 4).ThresholdSite);

            foreach (int tier in (int[])[5, 7])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var stair = a.ThresholdSite;
                Assert.NotNull(stair);
                Assert.Equal(Terrain.ThresholdEntrance, a.Overworld[stair!.OverworldPos]);
                Assert.Equal(stair.OverworldPos, b.ThresholdSite!.OverworldPos);
                Assert.True(Reachable(a.Overworld, a.ShrinePos, stair.OverworldPos),
                    $"seed {seed} tier {tier}: stair unreachable");

                // The final stage holds no foes and no loot: only the room.
                Assert.Empty(stair.Spawns);
                Assert.True(stair.ChestLooted);

                var hearth = FindHearth(stair.Map);
                Assert.True(Reachable(stair.Map, stair.EntryPos, hearth),
                    $"seed {seed} tier {tier}: hearth unreachable from entry");
                Assert.True(a.Facts.Exists("site", "threshold"));
            }
        }
    }

    [Fact]
    public void Door_StaysShut_UntilTheCommission()
    {
        // World 5, tier 5, but the ladder was never climbed: the stair is there
        // and the door is not for this bearer yet.
        var game = new Game(42);
        Cross(game);
        Cross(game);
        Cross(game);
        Cross(game);
        Assert.Equal(5, game.Cycle);
        Assert.False(game.Player.CommissionHeard);

        var stair = game.World.ThresholdSite!;
        game.Debug_SetPlayerPos(stair.OverworldPos);
        game.Apply(Command.Enter);

        Assert.Equal(MapMode.Overworld, game.Mode);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("no argument to be had with it"));
    }

    [Fact]
    public void TheApproach_SpeaksInOrder_AndTheKeepingOpensAtTheHearth()
    {
        var game = AtTheHearth();

        Assert.Equal(1, Count(game, "held the chisels"));
        Assert.Equal(1, Count(game, "the one law left this deep"));
        Assert.Equal(1, Count(game, "its own dust"));
        Assert.True(game.InThresholdMenu);
        Assert.Equal(Resolution.None, game.Player.Resolution);

        // Stepping back leaves the question open, and the room lets you return.
        game.ApplyKey(' ');
        Assert.False(game.InThresholdMenu);
        Assert.Equal(Resolution.None, game.Player.Resolution);
        game.ApplyKey('h');
        game.ApplyKey('l');
        Assert.True(game.InThresholdMenu);

        // The approach beats never repeat on the way back in.
        Assert.Equal(1, Count(game, "its own dust"));
        game.ApplyKey(' ');
    }

    [Fact]
    public void Kept_ResolvesOnce_AndTheKeepingNeverReopens()
    {
        var game = AtTheHearth();
        game.ApplyKey('1');

        Assert.Equal(Resolution.Kept, game.Player.Resolution);
        Assert.False(game.InThresholdMenu);
        Assert.Equal(1, Count(game, "ledger changing hands"));
        Assert.EndsWith(",kept", game.TakeSnapshot().ArcProgress);

        // The room stays; the question does not.
        game.ApplyKey('h');
        game.ApplyKey('l');
        Assert.False(game.InThresholdMenu);
        Assert.Equal(1, Count(game, "ledger changing hands"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("The count is warm to the touch"));

        // The one permitted long thread: the unfinished argument, in this branch's key.
        game.Debug_SetMode(MapMode.Overworld);
        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Contains(game.Topics, t => t.Label == "The argument");
        game.ApplyKey(' ');
    }

    [Fact]
    public void Refused_ResolvesOnce_AndOwnsTheWalkingOn()
    {
        var game = AtTheHearth();
        game.ApplyKey('2');

        Assert.Equal(Resolution.Refused, game.Player.Resolution);
        Assert.False(game.InThresholdMenu);
        Assert.Equal(1, Count(game, "Half my road"));
        Assert.EndsWith(",refused", game.TakeSnapshot().ArcProgress);

        game.ApplyKey('h');
        game.ApplyKey('l');
        Assert.False(game.InThresholdMenu);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Fires never do"));

        game.Debug_SetMode(MapMode.Overworld);
        NpcTests.BumpNpc(game, game.World.Unbinder);
        Assert.Contains(game.Topics, t => t.Label == "The argument");
        game.ApplyKey(' ');
    }

    [Fact]
    public void TheChoice_NeverTouchesASingleMechanicalNumber()
    {
        // The arc's guardrail (sec 8), enforced field by field: two identical
        // games answer the threshold differently, then live identical lives.
        var kept = AtTheHearth();
        var refused = AtTheHearth();
        kept.ApplyKey('1');
        refused.ApplyKey('2');
        AssertMechanicallyIdentical(kept, refused);

        // Walk out, cross to the next world, rest at its shrine: still identical.
        foreach (var game in (Game[])[kept, refused])
        {
            Cross(game);
            game.ApplyKey('r');
            game.ApplyKey(' ');
        }
        AssertMechanicallyIdentical(kept, refused);
    }

    [Fact]
    public void DeathLines_ClimbTheRegisters()
    {
        Assert.Equal(AegisVoice.FirstDeathLine, AegisVoice.DeathLine(1, register: 3));
        Assert.NotEqual(AegisVoice.DeathLine(2, register: 1), AegisVoice.DeathLine(2, register: 2));
        Assert.NotEqual(AegisVoice.DeathLine(2, register: 2), AegisVoice.DeathLine(2, register: 3));

        // Integration, register two: the ledger known, the second fall worried.
        var game = new Game(42);
        game.Player.LedgerHeard = true;
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the best of me or the worst"));

        // Integration, final register: the threshold answered, the fall candid.
        var resolved = AtTheHearth();
        resolved.ApplyKey('1');
        resolved.Debug_HurtPlayer(999);
        resolved.Debug_ForceDeathCheck();
        resolved.Debug_HurtPlayer(999);
        resolved.Debug_ForceDeathCheck();
        Assert.Contains(resolved.Log.Entries, e => e.Text.Contains("only the arithmetic"));
    }

    [Fact]
    public void Crossings_AfterResolution_SpeakTheFinalRegister_PerAnswer()
    {
        var kept = AtTheHearth();
        kept.ApplyKey('1');
        Cross(kept);
        Assert.Equal(1, Count(kept, "wood on the fire"));
        Assert.Equal(1, Count(kept, "carry you either way"));

        var refused = AtTheHearth();
        refused.ApplyKey('2');
        Cross(refused);
        Assert.Equal(1, Count(refused, "no one's errand but ours"));
    }

    [Fact]
    public void TheMorningAfter_ClosesOnTheStead_NotTheMystery()
    {
        var game = AtTheHearth();
        game.ApplyKey('1');

        var steadholder = game.World.Npcs.First(n => n.Id == "npc_steadholder");
        StepNearHouse(game);

        Assert.Equal(1, Count(game, "The morning goes on with you in it"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains(steadholder.Name) && e.Text.Contains("warm bread"));

        // The closing note is local and plain, not the Aegis's.
        var closing = game.Log.Entries.Last(e => e.Text.Contains("The morning goes on with you in it"));
        Assert.Equal(LogTone.Info, closing.Tone);

        // Once ever, like every rung.
        StepNearHouse(game);
        Assert.Equal(1, Count(game, "The morning goes on with you in it"));
    }

    [Fact]
    public void Presenter_DrawsTheStair_AndTheKeepingMenu()
    {
        var game = new Game(42);
        Cross(game);
        Cross(game);
        Cross(game);
        Cross(game);
        var stairPos = game.World.ThresholdSite!.OverworldPos;
        game.Debug_SetPlayerPos(stairPos.Plus(-1, 0));

        var frame = Presenter.Render(game);
        bool stairGlyph = false;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (frame[x, y] is { Ch: 'v', Fg: Hue.Magenta }) stairGlyph = true;
        Assert.True(stairGlyph, "the deep stair is not drawn on the overworld");

        var menu = Presenter.Render(AtTheHearth()).ToTextLines();
        Assert.Contains(menu, line => line.Contains("The Keeping"));
        Assert.Contains(menu, line => line.Contains("1) Take up the keeping"));
        Assert.Contains(menu, line => line.Contains("2) Lay the commission down and walk on"));
    }

    // ---- helpers ----

    /// <summary>Master 42 walked up every rung to the commission: world 5, tier 5.</summary>
    private static Game CommissionedGame()
    {
        var game = new Game(42);
        Cross(game);                                    // world 2
        EnterHollow(game);
        FellSevered(game);                              // truth
        Cross(game);                                    // guilt, world 3
        game.ApplyKey('r');                             // vision
        game.ApplyKey(' ');
        Cross(game);                                    // ledger, world 4
        NpcTests.BumpNpc(game, game.World.SeveredNpc!); // peace
        game.ApplyKey(' ');
        StepOnto(game, game.World.HollowSite!.OverworldPos); // cost
        NpcTests.BumpNpc(game, game.World.Unbinder);    // tier 1
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, game.World.Unbinder);    // tier 2
        game.ApplyKey(' ');
        Cross(game);                                    // commission, world 5
        Assert.True(game.Player.CommissionHeard);
        return game;
    }

    /// <summary>Commissioned, descended, and walked to the Hearth: the keeping is open.</summary>
    private static Game AtTheHearth()
    {
        var game = CommissionedGame();
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.ThresholdSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("threshold", game.CurrentSite!.Id);

        for (int i = 0; i < 40 && !game.InThresholdMenu; i++) game.ApplyKey('l');
        Assert.True(game.InThresholdMenu, "the walk down the corridor never reached the Hearth");
        return game;
    }

    /// <summary>Walks one real step onto a tile beside a house, so NearHouse fires.</summary>
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
                StepOnto(game, p);
                return;
            }
        Assert.Fail("no walkable tile beside a house");
    }

    private static void AssertMechanicallyIdentical(Game a, Game b)
    {
        var sa = a.TakeSnapshot();
        var sb = b.TakeSnapshot();
        foreach (var prop in typeof(Snapshot).GetProperties())
        {
            if (prop.Name is nameof(Snapshot.ArcProgress) or nameof(Snapshot.RecentMessages)) continue;
            Assert.True(Equals(prop.GetValue(sa), prop.GetValue(sb)),
                $"the threshold choice leaked into mechanics: {prop.Name} differs ({prop.GetValue(sa)} vs {prop.GetValue(sb)})");
        }
    }

    private static int Count(Game game, string marker) =>
        game.Log.Entries.Count(e => e.Text.Contains(marker));

    private static Pos FindHearth(GameMap map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map[new Pos(x, y)] == Terrain.Hearth) return new Pos(x, y);
        Assert.Fail("no hearth in the threshold site");
        return default;
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

    private static void FellSevered(Game game)
    {
        var severed = game.Monsters.First(m => m.Kind == MonsterKind.Severed && m.SiteId == "hollow" && m.Alive);
        severed.Hp = 1;
        game.Debug_SetPlayerPos(severed.Pos.Plus(-1, 0));
        game.ApplyKey('l');
        Assert.False(severed.Alive);
    }

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
