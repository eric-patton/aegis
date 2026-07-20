using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Template selection and the Creeping Blight (D-035): one story per world chosen
/// among eligible templates, tier-1 worlds unchanged, and the blight's new contract
/// features: an accepted-history fact flipped by found evidence, and endings that
/// branch on whether the truth was found before the deed.
/// </summary>
public class BlightTests
{
    /// <summary>Master seed whose cycle-2 world selects the Creeping Blight.</summary>
    // D-112's third template remapped the cycle-2 draws: 42 now tells the stead
    // again and 41 the blight.
    private const ulong BlightMaster = 41;

    [Fact]
    public void Tier1_AlwaysTellsTheRaidedStead()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var world = WorldGen.Generate(seed);
            Assert.Equal("raided-stead", world.Facts.OfType("story").Single().Subject);
            Assert.True(world.Facts.Exists("role", "plaintiff"));
            Assert.False(world.Facts.Exists("history", "mound_curse"));
            Assert.False(world.Facts.Exists("history", "seat_taken"));
        }
    }

    [Fact]
    public void Tier2_Selection_IsDeterministic_AndBothTemplatesOccur()
    {
        var seen = new HashSet<string>();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var a = WorldGen.Generate(seed, tier: 2);
            var b = WorldGen.Generate(seed, tier: 2);
            string story = a.Facts.OfType("story").Single().Subject;
            Assert.Equal(story, b.Facts.OfType("story").Single().Subject);
            seen.Add(story);

            // The chosen template's cast is present; the others' is absent.
            if (story == "creeping-blight")
            {
                Assert.True(a.Facts.Exists("role", "afflicted"));
                Assert.True(a.Facts.Exists("history", "mound_curse"));
                Assert.False(a.Facts.Exists("role", "plaintiff"));
                Assert.False(a.Facts.Exists("role", "claimant"));
            }
            else if (story == "usurped-throne")
            {
                Assert.True(a.Facts.Exists("role", "teller"));
                Assert.True(a.Facts.Exists("role", "claimant"));
                Assert.True(a.Facts.Exists("history", "seat_taken"));
                Assert.False(a.Facts.Exists("role", "plaintiff"));
                Assert.False(a.Facts.Exists("history", "mound_curse"));
            }
            else
            {
                Assert.True(a.Facts.Exists("role", "plaintiff"));
                Assert.False(a.Facts.Exists("history", "mound_curse"));
                Assert.False(a.Facts.Exists("history", "seat_taken"));
            }
        }
        Assert.Equal(["creeping-blight", "raided-stead", "usurped-throne"], seen.Order());
    }

    [Fact]
    public void Blight_Plea_WritesThePromise_AndSnapshotNamesTheStory()
    {
        var game = CrossedBlightGame();
        Assert.Equal("creeping-blight", game.TakeSnapshot().StoryTemplate);

        var afflicted = Afflicted(game);
        NpcTests.BumpNpc(game, afflicted);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("still the source"));
        Assert.True(game.World.Facts.Exists("promise", "end_the_creep"));
        game.ApplyKey(' ');
    }

    [Fact]
    public void Blight_EndingWithoutEvidence_BuriesTheTruth()
    {
        var game = CrossedBlightGame();
        game.Debug_ClearSite(SiteKind.Barrow);

        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("good story"));
        Assert.True(game.World.Facts.Exists("story_complete", "creeping-blight"));
        Assert.True(game.World.Facts.Exists("coda", "truth_buried"));
        Assert.False(game.World.Facts.Exists("coda", "truth_published"));
    }

    [Fact]
    public void Blight_EvidenceDeepInTheBarrow_FlipsTheEnding()
    {
        var game = CrossedBlightGame();

        // Still the wights without writing the deed, then walk the passage deep
        // enough to read the stones.
        foreach (var wight in game.Monsters.Where(m => m.SiteId == "barrow"))
            wight.Hp = 0;
        game.Debug_SetPlayerPos(game.World.BarrowSite!.OverworldPos);
        game.Apply(Command.Enter);
        for (int i = 0; i < 20 && !game.World.Facts.Exists("evidence", "mound_truth"); i++)
            game.ApplyKey('l');

        Assert.True(game.World.Facts.Exists("evidence", "mound_truth"), "walking the passage never surfaced the evidence");
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("They were hired"));

        game.Debug_ClearSite(SiteKind.Barrow);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("debt found"));
        Assert.True(game.World.Facts.Exists("story_complete", "creeping-blight"));
        Assert.True(game.World.Facts.Exists("coda", "truth_published"));
        Assert.False(game.World.Facts.Exists("coda", "truth_buried"));
    }

    [Fact]
    public void Blight_StoryEndsOnce_LateEvidence_DoesNotRewriteTheEnding()
    {
        // The throne surfaced this hole (D-112): evidence read only after the
        // deed re-armed the truth ending on the next deed's hook. Same latent
        // shape here: stones read after the stilling must not rewrite it.
        var game = CrossedBlightGame();
        game.Debug_ClearSite(SiteKind.Barrow);
        Assert.True(game.World.Facts.Exists("coda", "truth_buried"));

        game.Debug_SetPlayerPos(game.World.BarrowSite!.OverworldPos);
        game.Apply(Command.Enter);
        for (int i = 0; i < 20 && !game.World.Facts.Exists("evidence", "mound_truth"); i++)
            game.ApplyKey('l');
        Assert.True(game.World.Facts.Exists("evidence", "mound_truth"));
        game.Debug_SetMode(MapMode.Overworld);

        game.Debug_ClearCamp();
        Assert.False(game.World.Facts.Exists("coda", "truth_published"));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("debt found"));
    }

    [Fact]
    public void Blight_KeptPromise_PaysEssence()
    {
        var game = CrossedBlightGame();

        NpcTests.BumpNpc(game, Afflicted(game));
        game.ApplyKey(' ');
        Assert.True(game.World.Facts.Exists("promise", "end_the_creep"));

        game.Debug_ClearSite(SiteKind.Barrow);
        int before = game.Player.Essence;
        NpcTests.BumpNpc(game, Afflicted(game));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("only a hill now"));
        Assert.Equal(before + 3, game.Player.Essence);
        game.ApplyKey(' ');
    }

    [Fact]
    public void Blight_AcceptedHistory_IsVoicedNearHouses_BeforeTheDeed()
    {
        var game = CrossedBlightGame();
        var (a, b) = FindHouseAdjacentPair(game);

        for (int i = 0; i < 400 && !game.Log.Entries.Any(e => e.Text.Contains("Oath-breakers under the turf")); i++)
            StepBetween(game, a, b);

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Oath-breakers under the turf"));
    }

    /// <summary>Crosses the blight master into its cycle-2 world, which tells the blight.</summary>
    private static Game CrossedBlightGame()
    {
        var game = new Game(BlightMaster);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal("creeping-blight", game.World.Facts.OfType("story").Single().Subject);
        return game;
    }

    private static Npc Afflicted(Game game)
    {
        string id = game.World.Facts.Find("role", "afflicted")!.Object;
        return game.World.Npcs.First(n => n.Id == id);
    }

    /// <summary>Two adjacent walkable tiles, at least one beside a house, clear of NPCs.</summary>
    private static (Pos A, Pos B) FindHouseAdjacentPair(Game game)
    {
        var map = game.World.Overworld;
        bool Free(Pos p) => map.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p);
        bool ByHouse(Pos p) => Directions.All8.Any(d =>
            map.InBounds(p.Plus(d.dx, d.dy)) && map[p.Plus(d.dx, d.dy)] == Terrain.House);

        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var p = new Pos(x, y);
                if (!Free(p) || !ByHouse(p)) continue;
                foreach (var (dx, dy) in Directions.Cardinal)
                {
                    var q = p.Plus(dx, dy);
                    if (Free(q)) return (p, q);
                }
            }
        throw new InvalidOperationException("no house-adjacent pair found");
    }

    private static void StepBetween(Game game, Pos a, Pos b)
    {
        var target = game.Player.Pos == a ? b : a;
        if (game.Player.Pos != a && game.Player.Pos != b)
        {
            game.Debug_SetPlayerPos(b);
            target = a;
        }
        var d = (target.X - game.Player.Pos.X, target.Y - game.Player.Pos.Y);
        game.ApplyKey(d switch
        {
            (0, -1) => 'k',
            (0, 1) => 'j',
            (-1, 0) => 'h',
            (1, 0) => 'l',
            _ => '.',
        });
    }
}
