using Aegis.Core;

namespace Aegis.Core.Tests;

public class TemplateTests
{
    private static Npc Plaintiff(Game game)
    {
        string id = game.World.Facts.Find("role", "plaintiff")!.Object;
        return game.World.Npcs.First(n => n.Id == id);
    }

    [Fact]
    public void PlaintiffRole_IsCastDeterministically_AndVariesAcrossSeeds()
    {
        var holders = new HashSet<string>();
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            var roleA = a.Facts.Find("role", "plaintiff");
            Assert.NotNull(roleA);
            Assert.Equal(roleA.Object, b.Facts.Find("role", "plaintiff")!.Object);
            Assert.Contains(a.Npcs, n => n.Id == roleA.Object);
            Assert.Equal(3, a.StoryStorylets.Count);

            holders.Add(roleA.Object);
        }
        // The cast varies: different worlds hand the grievance to different roles.
        Assert.True(holders.Count >= 2, "plaintiff was the same role slot in all 30 seeds");
    }

    [Fact]
    public void Plea_FiresOnlyFromThePlaintiff_AndWritesThePromise()
    {
        var game = new Game(42);
        var plaintiff = Plaintiff(game);

        var bystander = game.World.Npcs.First(n => n.Id != plaintiff.Id);
        NpcTests.BumpNpc(game, bystander);
        Assert.False(LogContains(game, "grips your arm"));
        Assert.False(game.World.Facts.Exists("promise", "end_the_raids"));
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, plaintiff);
        Assert.True(LogContains(game, "grips your arm"));
        Assert.True(game.World.Facts.Exists("promise", "end_the_raids"));
    }

    [Fact]
    public void KeptPromiseChain_PleaThenDeedThenThanks_PaysEssence()
    {
        var game = new Game(42);
        var plaintiff = Plaintiff(game);

        NpcTests.BumpNpc(game, plaintiff);
        Assert.True(game.World.Facts.Exists("promise", "end_the_raids"));
        game.ApplyKey(' ');

        game.Debug_ClearCamp();
        Assert.True(LogContains(game, "will hear of this by nightfall"));
        Assert.True(game.World.Facts.Exists("story_complete", RaidedSteadTemplate.Id));

        int essenceBefore = game.Player.Essence;
        NpcTests.BumpNpc(game, plaintiff);
        Assert.True(LogContains(game, "My house does not forget"));
        Assert.Equal(essenceBefore + 3, game.Player.Essence);
    }

    [Fact]
    public void ColdPath_NoPleaMeansNoKeptPromise()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();

        // The ending is still witnessed (that beat needs no promise) ...
        Assert.True(LogContains(game, "will hear of this by nightfall"));

        // ... but the personal payoff never opens: you were never asked.
        var plaintiff = Plaintiff(game);
        int essenceBefore = game.Player.Essence;
        NpcTests.BumpNpc(game, plaintiff);
        Assert.False(LogContains(game, "My house does not forget"));
        Assert.Equal(essenceBefore, game.Player.Essence);
    }

    [Fact]
    public void WitnessedEnding_AnswersTheCampDeedOnly_NotTheBarrows()
    {
        // Found live: clearing the barrow first made the plaintiff "hear of" a deed
        // nobody asked for. The ending beat must answer its own story's deed.
        // Master 42's second world tells the stead again (D-112 remap).
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        string witnessLine = $"{Plaintiff(game).Name} will hear of this by nightfall";
        game.Debug_ClearSite(SiteKind.Barrow);
        Assert.False(LogContains(game, witnessLine));
        Assert.False(game.World.Facts.Exists("story_complete", RaidedSteadTemplate.Id));

        game.Debug_ClearCamp();
        Assert.True(LogContains(game, witnessLine));
        Assert.True(game.World.Facts.Exists("story_complete", RaidedSteadTemplate.Id));
    }

    [Fact]
    public void NewWorld_RecastsTheStory_AndItPlaysAgain()
    {
        // Master 42: its second world tells the stead again (D-112 remap).
        var game = new Game(42);
        string firstPlaintiff = Plaintiff(game).Name;

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // World 2 has its own cast and its own live story.
        var plaintiff = Plaintiff(game);
        Assert.False(game.World.Facts.Exists("promise", "end_the_raids"));

        NpcTests.BumpNpc(game, plaintiff);
        Assert.True(LogContains(game, "grips your arm"));
        Assert.True(game.World.Facts.Exists("promise", "end_the_raids"));
        Assert.Contains(plaintiff.Name, game.Log.Recent(6).Select(e => e.Text).First(t => t.Contains("grips your arm")));
    }

    private static bool LogContains(Game game, string fragment)
        => game.Log.Recent(50).Any(e => e.Text.Contains(fragment));
}
