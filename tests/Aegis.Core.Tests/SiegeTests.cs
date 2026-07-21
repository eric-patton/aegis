using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The Long Siege (D-130): the pool's sixth template, bound to the fen-leaguer
/// (tier 6+). The stead's telling has the leaguer penning something hungry on
/// the holm; the holm's bare turf flips it (the besieged were the stead's own
/// founders, out across one winter's ice, and the penned-thing tale was
/// theirs), and the lifting with the truth in hand holds the moment: carry
/// the tally down, or leave the founders their fear.
/// </summary>
public class SiegeTests
{
    /// <summary>Master seed whose cycle-6 world (the first tier-6 world) tells the Long Siege.</summary>
    private const ulong SiegeMaster = 2;

    [Fact]
    public void Tier6_Selection_IsDeterministic_AndTheSiegeOccurs()
    {
        var seen = new HashSet<string>();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var a = WorldGen.Generate(seed, tier: 6);
            var b = WorldGen.Generate(seed, tier: 6);
            string story = a.Facts.OfType("story").Single().Subject;
            Assert.Equal(story, b.Facts.OfType("story").Single().Subject);
            seen.Add(story);

            if (story == LongSiegeTemplate.Id)
            {
                Assert.True(a.Facts.Exists("role", "fisher"));
                Assert.True(a.Facts.Exists("history", "mere_penned"));
                Assert.False(a.Facts.Exists("role", "plaintiff"));
                Assert.False(a.Facts.Exists("history", "mound_curse"));
                Assert.False(a.Facts.Exists("history", "seat_taken"));
                Assert.False(a.Facts.Exists("history", "schism_stead"));
                Assert.False(a.Facts.Exists("history", "pit_left"));
            }
            else
            {
                Assert.False(a.Facts.Exists("role", "fisher"));
                Assert.False(a.Facts.Exists("history", "mere_penned"));
            }
        }
        Assert.Contains(LongSiegeTemplate.Id, seen);
        Assert.Equal(6, seen.Count);
    }

    [Fact]
    public void Siege_Plea_WritesThePromise_AndSnapshotNamesTheStory()
    {
        var game = CrossedSiegeGame();
        Assert.Equal(LongSiegeTemplate.Id, game.TakeSnapshot().StoryTemplate);

        NpcTests.BumpNpc(game, Fisher(game));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("rather be eaten"));
        Assert.True(game.World.Facts.Exists("promise", "lift_the_leaguer"));
        game.ApplyKey(' ');
    }

    [Fact]
    public void Siege_AcceptedHistory_IsVoicedNearHouses_BeforeTheDeed()
    {
        var game = CrossedSiegeGame();

        for (int i = 0; i < 400 && !game.Log.Entries.Any(e => e.Text.Contains("Never a boat on the black mere")); i++)
            ShameTests.StepStillNearAHouse(game);

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Never a boat on the black mere"));
    }

    [Fact]
    public void Siege_EndingWithoutEvidence_LeavesTheFearStanding()
    {
        var game = CrossedSiegeGame();
        game.Debug_ClearSite(SiteKind.Leaguer);

        // With nothing read there is nothing to choose: plain lines.
        Assert.False(game.InScene);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("unread in the reeds"));
        Assert.True(game.World.Facts.Exists("story_complete", LongSiegeTemplate.Id));
        Assert.True(game.World.Facts.Exists("coda", "fear_stands"));
        Assert.False(game.World.Facts.Exists("coda", "founding_carried"));
        Assert.False(game.World.Facts.Exists("withheld", "mere_truth"));
    }

    [Fact]
    public void Siege_EndingWithTheTruth_OpensTheScene_Unchecked()
    {
        var game = EvidencedSiegeGame();
        game.Debug_ClearSite(SiteKind.Leaguer);

        Assert.True(game.InScene);
        Assert.Equal("The ice and the tally", game.SceneTitle);
        Assert.Equal(2, game.SceneChoices.Count);
        Assert.All(game.SceneChoices, c => Assert.Equal("", c.Tag));
        Assert.False(game.World.Facts.Exists("story_complete", LongSiegeTemplate.Id));
    }

    [Fact]
    public void Siege_TallyCarriedDown_TurnsGratitudeTrue()
    {
        var game = EvidencedSiegeGame();
        game.Debug_ClearSite(SiteKind.Leaguer);
        AnswerTheBanks(game, '1');

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the quarry got out"));
        Assert.True(game.World.Facts.Exists("story_complete", LongSiegeTemplate.Id));
        Assert.True(game.World.Facts.Exists("coda", "founding_carried"));
        Assert.False(game.World.Facts.Exists("coda", "fear_stands"));
        Assert.False(game.World.Facts.Exists("withheld", "mere_truth"));
    }

    [Fact]
    public void Siege_TruthKept_AndTheSilenceCrosses()
    {
        var game = EvidencedSiegeGame();
        game.Debug_ClearSite(SiteKind.Leaguer);
        AnswerTheBanks(game, '2');

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("holds the dark a little closer"));
        Assert.True(game.World.Facts.Exists("coda", "fear_stands"));
        Assert.False(game.World.Facts.Exists("coda", "founding_carried"));
        Assert.True(game.World.Facts.Exists("withheld", "mere_truth"));
        string siegeWorld = game.World.Name;

        // The unsaid crosses on its own legs (D-120): the penned-thing tale
        // left standing travels through the arch as a silence fact.
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        var silence = Assert.Single(game.World.Facts.OfType("silence"));
        Assert.Equal("mere_truth", silence.Subject);
        Assert.Contains(siegeWorld, silence.Detail);
        Assert.Contains("walled under its holm", silence.Detail);
    }

    [Fact]
    public void Siege_StoryEndsOnce_LateTally_DoesNotRewriteTheEnding()
    {
        var game = CrossedSiegeGame();
        game.Debug_ClearSite(SiteKind.Leaguer);
        Assert.True(game.World.Facts.Exists("coda", "fear_stands"));

        ReadTheTally(game);
        Assert.True(game.World.Facts.Exists("evidence", "mere_truth"));
        game.Debug_SetMode(MapMode.Overworld);

        game.Debug_ClearCamp();
        Assert.False(game.InScene);
        Assert.False(game.World.Facts.Exists("coda", "founding_carried"));
    }

    [Fact]
    public void Siege_KeptPromise_PaysEssence()
    {
        var game = CrossedSiegeGame();

        NpcTests.BumpNpc(game, Fisher(game));
        game.ApplyKey(' ');
        Assert.True(game.World.Facts.Exists("promise", "lift_the_leaguer"));

        game.Debug_ClearSite(SiteKind.Leaguer);
        int before = game.Player.Essence;
        NpcTests.BumpNpc(game, Fisher(game));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("a boat on it by summer"));
        Assert.Equal(before + 3, game.Player.Essence);
        game.ApplyKey(' ');
    }

    /// <summary>Crosses the siege master into its cycle-6 world, which tells the Long Siege.</summary>
    private static Game CrossedSiegeGame()
    {
        var game = new Game(SiegeMaster);
        for (int i = 0; i < 5; i++)
        {
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        Assert.Equal(6, game.Cycle);
        Assert.Equal(LongSiegeTemplate.Id, game.World.Facts.OfType("story").Single().Subject);
        return game;
    }

    /// <summary>A crossed siege game that has stood on the holm and read the tally.</summary>
    private static Game EvidencedSiegeGame()
    {
        var game = CrossedSiegeGame();
        ReadTheTally(game);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("its own grandmothers"));
        return game;
    }

    /// <summary>Stills the warders without writing the deed, then steps onto the holm.</summary>
    private static void ReadTheTally(Game game)
    {
        foreach (var warder in game.Monsters.Where(m => m.SiteId == "leaguer"))
            warder.Hp = 0;
        game.Debug_SetPlayerPos(game.World.LeaguerSite!.OverworldPos);
        game.Apply(Command.Enter);

        // The causeway runs along the mere's midline into the holm's west edge.
        game.Debug_SetPlayerPos(new Pos(WorldGen.HolmMinX - 1, WorldGen.LeaguerH / 2));
        game.ApplyKey('l');
        Assert.True(game.World.Facts.Exists("evidence", "mere_truth"), "the holm never surfaced the tally");
    }

    /// <summary>Answers the open ice-and-tally scene and closes its leaf.</summary>
    private static void AnswerTheBanks(Game game, char answer)
    {
        Assert.True(game.InScene, "the banks scene never opened");
        game.ApplyKey(answer);
        Assert.True(game.InScene);
        game.ApplyKey(' ');
        Assert.False(game.InScene);
    }

    private static Npc Fisher(Game game)
    {
        string id = game.World.Facts.Find("role", "fisher")!.Object;
        return game.World.Npcs.First(n => n.Id == id);
    }
}
