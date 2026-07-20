using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The cross-world withheld consumer (D-120): a truth kept at its saying-moment
/// leaves the wrong story standing, and the wrong story crosses arches the way
/// any good story does. The crossing presses each withheld fact into the next
/// world as a silence fact, silences carry forward whole at every later arch
/// (and through a hushed one: what was never said cannot be hushed), and near
/// the houses of a later world the bearer hears the story retold for true.
/// </summary>
public class SilenceTests
{
    [Fact]
    public void AKeptTruth_CrossesAsASilence()
    {
        var game = new Game(42);
        string firstWorld = game.World.Name;
        game.World.Facts.Add("withheld", "mound_truth", game.World.SettlementName,
            "At the stilling the bearer kept the mound's history.");
        Cross(game);

        var silence = Assert.Single(game.World.Facts.OfType("silence"));
        Assert.Equal("mound_truth", silence.Subject);
        Assert.Contains(firstWorld, silence.Detail);
        Assert.Contains("outlasted a barrow's grudge", silence.Detail);
    }

    [Fact]
    public void ACleanCrossing_CarriesNoSilence()
    {
        var game = new Game(42);
        Cross(game);

        Assert.Empty(game.World.Facts.OfType("silence"));
    }

    [Fact]
    public void Silences_CarryForward_AndAccumulate()
    {
        var game = new Game(42);
        game.World.Facts.Add("withheld", "mound_truth", game.World.SettlementName, "kept");
        Cross(game);
        game.World.Facts.Add("withheld", "seat_truth", game.World.SettlementName, "kept");
        Cross(game);
        Cross(game);

        Assert.Equal(2, game.World.Facts.OfType("silence").Count());
        Assert.True(game.World.Facts.Exists("silence", "mound_truth"));
        Assert.True(game.World.Facts.Exists("silence", "seat_truth"));
    }

    [Fact]
    public void AHushedArch_DoesNotStillTheSilence()
    {
        var game = new Game(42);
        game.World.Facts.Add("withheld", "founding_truth", game.World.SettlementName, "kept");
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('7'); // the hushed name
        game.ApplyKey('>');

        // The songs about the bearer are stilled; the story that stood in the
        // truth's place was never one of those, and crosses anyway.
        Assert.Empty(game.World.Facts.OfType("echo"));
        Assert.True(game.World.Facts.Exists("silence", "founding_truth"));
    }

    [Fact]
    public void TheSilenceRetold_IsHeardNearTheHouses_OncePerWorld()
    {
        var game = new Game(42);
        game.World.Facts.Add("withheld", "seat_truth", game.World.SettlementName, "kept");
        Cross(game);

        for (int i = 0; i < 40 && !game.Log.Entries.Any(e => e.Text.Contains("the count travels")); i++)
            ShameTests.StepStillNearAHouse(game);

        // The wrong story in a stranger's mouth, and the one reader standing by.
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("avenged"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the count travels"));

        for (int i = 0; i < 6; i++) ShameTests.StepStillNearAHouse(game);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("the count travels"));
    }

    private static void Cross(Game game)
    {
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }
}
