using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The Gold Rush (D-121): the pool's fifth template, bound to the old quarry
/// (tier 3+). The stead's greed-tale has the graven figures as the punished
/// crew; the survey at the working face flips it (the crew struck the seam,
/// read what it runs through, and authored the tale as a fence), and the
/// hushing with the truth in hand holds the moment: mend the fence with the
/// truth, or leave the founders' kind lie standing alone.
/// </summary>
public class RushTests
{
    /// <summary>Master seed whose cycle-3 world (the first tier-3 world) selects the Gold Rush.</summary>
    private const ulong RushMaster = 2;

    [Fact]
    public void Tier3_Selection_IsDeterministic_AndTheRushOccurs()
    {
        var seen = new HashSet<string>();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var a = WorldGen.Generate(seed, tier: 3);
            var b = WorldGen.Generate(seed, tier: 3);
            string story = a.Facts.OfType("story").Single().Subject;
            Assert.Equal(story, b.Facts.OfType("story").Single().Subject);
            seen.Add(story);

            if (story == GoldRushTemplate.Id)
            {
                Assert.True(a.Facts.Exists("role", "prospector"));
                Assert.True(a.Facts.Exists("history", "pit_left"));
                Assert.False(a.Facts.Exists("role", "plaintiff"));
                Assert.False(a.Facts.Exists("history", "mound_curse"));
                Assert.False(a.Facts.Exists("history", "seat_taken"));
                Assert.False(a.Facts.Exists("history", "schism_stead"));
            }
            else
            {
                Assert.False(a.Facts.Exists("role", "prospector"));
                Assert.False(a.Facts.Exists("history", "pit_left"));
            }
        }
        Assert.Contains(GoldRushTemplate.Id, seen);
        Assert.Equal(5, seen.Count);
    }

    [Fact]
    public void Rush_Plea_WritesThePromise_AndSnapshotNamesTheStory()
    {
        var game = CrossedRushGame();
        Assert.Equal(GoldRushTemplate.Id, game.TakeSnapshot().StoryTemplate);

        NpcTests.BumpNpc(game, Prospector(game));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("Mine first"));
        Assert.True(game.World.Facts.Exists("promise", "open_the_pit"));
        game.ApplyKey(' ');
    }

    [Fact]
    public void Rush_AcceptedHistory_IsVoicedNearHouses_BeforeTheDeed()
    {
        var game = CrossedRushGame();

        for (int i = 0; i < 400 && !game.Log.Entries.Any(e => e.Text.Contains("Greedy hands, closed hand")); i++)
            ShameTests.StepStillNearAHouse(game);

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Greedy hands, closed hand"));
    }

    [Fact]
    public void Rush_EndingWithoutEvidence_LeavesTheFenceAlone()
    {
        var game = CrossedRushGame();
        game.Debug_ClearSite(SiteKind.Quarry);

        // With nothing read there is nothing to choose: plain lines.
        Assert.False(game.InScene);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("only stone after all"));
        Assert.True(game.World.Facts.Exists("story_complete", GoldRushTemplate.Id));
        Assert.True(game.World.Facts.Exists("coda", "fence_alone"));
        Assert.False(game.World.Facts.Exists("coda", "pit_fenced_true"));
        Assert.False(game.World.Facts.Exists("withheld", "pit_truth"));
    }

    [Fact]
    public void Rush_EndingWithTheTruth_OpensTheScene_Unchecked()
    {
        var game = EvidencedRushGame();
        game.Debug_ClearSite(SiteKind.Quarry);

        Assert.True(game.InScene);
        Assert.Equal("The fence and the seam", game.SceneTitle);
        Assert.Equal(2, game.SceneChoices.Count);
        Assert.All(game.SceneChoices, c => Assert.Equal("", c.Tag));
        Assert.False(game.World.Facts.Exists("story_complete", GoldRushTemplate.Id));
    }

    [Fact]
    public void Rush_SurveyCarriedDown_MendsTheFence()
    {
        var game = EvidencedRushGame();
        game.Debug_ClearSite(SiteKind.Quarry);
        AnswerTheBrink(game, '1');

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("true posts"));
        Assert.True(game.World.Facts.Exists("story_complete", GoldRushTemplate.Id));
        Assert.True(game.World.Facts.Exists("coda", "pit_fenced_true"));
        Assert.False(game.World.Facts.Exists("coda", "fence_alone"));
        Assert.False(game.World.Facts.Exists("withheld", "pit_truth"));
    }

    [Fact]
    public void Rush_TruthKept_AndTheSilenceCrosses()
    {
        var game = EvidencedRushGame();
        game.Debug_ClearSite(SiteKind.Quarry);
        AnswerTheBrink(game, '2');

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("somebody's kindness first"));
        Assert.True(game.World.Facts.Exists("coda", "fence_alone"));
        Assert.False(game.World.Facts.Exists("coda", "pit_fenced_true"));
        Assert.True(game.World.Facts.Exists("withheld", "pit_truth"));
        string rushWorld = game.World.Name;

        // The unsaid crosses on its own legs (D-120): the greed-tale left
        // standing travels through the arch as a silence fact.
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        var silence = Assert.Single(game.World.Facts.OfType("silence"));
        Assert.Equal("pit_truth", silence.Subject);
        Assert.Contains(rushWorld, silence.Detail);
        Assert.Contains("ate its greedy crew", silence.Detail);
    }

    [Fact]
    public void Rush_StoryEndsOnce_LateSurvey_DoesNotRewriteTheEnding()
    {
        var game = CrossedRushGame();
        game.Debug_ClearSite(SiteKind.Quarry);
        Assert.True(game.World.Facts.Exists("coda", "fence_alone"));

        ReadTheFace(game);
        Assert.True(game.World.Facts.Exists("evidence", "pit_truth"));
        game.Debug_SetMode(MapMode.Overworld);

        game.Debug_ClearCamp();
        Assert.False(game.InScene);
        Assert.False(game.World.Facts.Exists("coda", "pit_fenced_true"));
    }

    [Fact]
    public void Rush_KeptPromise_PaysEssence()
    {
        var game = CrossedRushGame();

        NpcTests.BumpNpc(game, Prospector(game));
        game.ApplyKey(' ');
        Assert.True(game.World.Facts.Exists("promise", "open_the_pit"));

        game.Debug_ClearSite(SiteKind.Quarry);
        int before = game.Player.Essence;
        NpcTests.BumpNpc(game, Prospector(game));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("you are the one who opened it"));
        Assert.Equal(before + 3, game.Player.Essence);
        game.ApplyKey(' ');
    }

    /// <summary>Crosses the rush master into its cycle-3 world, which tells the Gold Rush.</summary>
    private static Game CrossedRushGame()
    {
        var game = new Game(RushMaster);
        for (int i = 0; i < 2; i++)
        {
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        Assert.Equal(3, game.Cycle);
        Assert.Equal(GoldRushTemplate.Id, game.World.Facts.OfType("story").Single().Subject);
        return game;
    }

    /// <summary>A crossed rush game that has stood at the working face and read the survey.</summary>
    private static Game EvidencedRushGame()
    {
        var game = CrossedRushGame();
        ReadTheFace(game);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("greed-tale on themselves"));
        return game;
    }

    /// <summary>Stills the graven without writing the deed, then walks to the working face.</summary>
    private static void ReadTheFace(Game game)
    {
        foreach (var graven in game.Monsters.Where(m => m.SiteId == "quarry"))
            graven.Hp = 0;
        game.Debug_SetPlayerPos(game.World.QuarrySite!.OverworldPos);
        game.Apply(Command.Enter);

        // Step onto a deep-face tile: a walkable pair straddling the depth line.
        var map = game.World.QuarrySite.Map;
        for (int y = 1; y < WorldGen.QuarryH - 1; y++)
        {
            var near = new Pos(24 - 1, y);
            var deep = new Pos(24, y);
            if (!map.Walkable(near) || !map.Walkable(deep)) continue;
            game.Debug_SetPlayerPos(near);
            game.ApplyKey('l');
            break;
        }
        Assert.True(game.World.Facts.Exists("evidence", "pit_truth"), "the working face never surfaced the survey");
    }

    /// <summary>Answers the open fence-and-seam scene and closes its leaf.</summary>
    private static void AnswerTheBrink(Game game, char answer)
    {
        Assert.True(game.InScene, "the brink scene never opened");
        game.ApplyKey(answer);
        Assert.True(game.InScene);
        game.ApplyKey(' ');
        Assert.False(game.InScene);
    }

    private static Npc Prospector(Game game)
    {
        string id = game.World.Facts.Find("role", "prospector")!.Object;
        return game.World.Npcs.First(n => n.Id == id);
    }
}
