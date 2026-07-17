using Aegis.Core;

namespace Aegis.Core.Tests;

public class StoryletTests
{
    /// <summary>Settlement center sits two tiles north of the shrine, ringed by houses.</summary>
    private static void StepTowardHouses(Game game) => game.ApplyKey('k');

    private static bool LogContains(Game game, string fragment)
        => game.Log.Recent(50).Any(e => e.Text.Contains(fragment));

    [Fact]
    public void ArrivalStorylet_FiresAtGameStart_OncePerCharacter()
    {
        var game = new Game(42);
        Assert.True(LogContains(game, "warm as held breath"));
        Assert.Equal(1, game.StoryletsFired);
    }

    [Fact]
    public void Grievance_FiresNearHouse_WritesMetFact_AndOnlyOnce()
    {
        var game = new Game(42);
        StepTowardHouses(game);

        Assert.True(LogContains(game, "A shutter opens a finger's width"));
        Assert.True(LogContains(game, game.World.Facts.Find("grievance", "goblin_camp")!.Detail));
        Assert.True(game.World.Facts.Exists("met", "worried_villager"));

        game.ApplyKey('j');
        StepTowardHouses(game);
        Assert.Equal(1, game.Log.Recent(50).Count(e => e.Text.Contains("A shutter opens a finger's width")));
    }

    [Fact]
    public void Gratitude_RequiresBothDeedAndPriorMeeting()
    {
        // Deed done but villager never met: neither grievance (forbidden) nor gratitude (unmet).
        var cold = new Game(42);
        cold.Debug_ClearCamp();
        StepTowardHouses(cold);
        Assert.False(LogContains(cold, "worn pouch"));
        Assert.False(LogContains(cold, "shutter opens a finger's width"));

        // Met first, then deed: the chain completes and pays.
        var warm = new Game(42);
        StepTowardHouses(warm);
        Assert.True(warm.World.Facts.Exists("met", "worried_villager"));
        warm.Debug_ClearCamp();
        int coinBefore = warm.Player.Coin;
        warm.ApplyKey('j');
        StepTowardHouses(warm);

        Assert.True(LogContains(warm, "worn pouch"));
        Assert.Equal(coinBefore + 5, warm.Player.Coin);
        Assert.True(warm.World.Facts.Exists("boon", "stead_pouch"));
    }

    [Fact]
    public void FirstTally_FiresOnFirstRestOnly_AndNeverAgainAfterCrossing()
    {
        var game = new Game(42);
        game.ApplyKey('r');
        Assert.True(LogContains(game, "The first tally is small"));
        game.ApplyKey(' '); // rise from the menu

        game.ApplyKey('r');
        int mentions = game.Log.Recent(50).Count(e => e.Text.Contains("The first tally is small"));
        Assert.Equal(1, mentions);
        game.ApplyKey(' ');

        // Character scope survives the crossing: resting in world 2 stays quiet.
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        game.ApplyKey('r');
        Assert.Equal(1, game.Log.Recent(200).Count(e => e.Text.Contains("The first tally is small")));
    }

    [Fact]
    public void ArchRemembered_FiresOnFirstGateTouch()
    {
        var game = new Game(42);
        var beside = FindWalkableNeighbor(game, game.World.GatePos);
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyToward(beside, game.World.GatePos));

        Assert.Equal(game.World.GatePos, game.Player.Pos);
        Assert.True(LogContains(game, "I know this arch"));
    }

    [Fact]
    public void EchoBallad_OnlyExistsAfterACrossing()
    {
        // Master 43: its second world tells the stead (repeat-weighting remap), so
        // the NearHouse pool here stays the one this test was written against.
        var game = new Game(43);
        StepTowardHouses(game);
        Assert.False(LogContains(game, "hums a tune"));

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // Two visits: grievance (reset by world scope) and the ballad are both
        // NearHouse candidates now; each visit fires at most one.
        StepTowardHouses(game);
        game.ApplyKey('j');
        StepTowardHouses(game);

        Assert.True(LogContains(game, "hums a tune"));
        Assert.True(LogContains(game, "Your deeds travel ahead of you"));
    }

    [Fact]
    public void AmbientStorylets_FireSparsely_OnOverworldTurns()
    {
        var game = new Game(42);
        for (int i = 0; i < 400 && game.Running; i++)
            game.ApplyKey('.');

        Assert.True(LogContains(game, "Wind combs") || LogContains(game, "bell no one rings"),
            "no ambient storylet in 400 overworld turns");
        // Cooldowns keep it sparse: 400 turns can hold at most 7 wind + 5 bell firings.
        Assert.InRange(game.StoryletsFired, 2, 13);
    }

    [Fact]
    public void StoryletFirings_ReplayIdentically_FromJournal()
    {
        var game = new Game(42);
        var journal = new List<char>();
        game.KeyApplied += journal.Add;

        // Meet the villager, rest, wander a while: a mix of gated and ambient firings.
        game.ApplyKey('k');
        game.ApplyKey('j');
        game.ApplyKey('r');
        game.ApplyKey(' ');
        for (int i = 0; i < 120; i++) game.ApplyKey('.');

        var replayed = SaveCodec.Replay(42, new string(journal.ToArray()));

        Assert.Equal(game.StoryletsFired, replayed.StoryletsFired);
        Assert.Equal(game.Player.Coin, replayed.Player.Coin);
        Assert.Equal(game.World.Facts.All.Count, replayed.World.Facts.All.Count);
        Assert.Equal(
            game.Log.Recent(20).Select(e => e.Text),
            replayed.Log.Recent(20).Select(e => e.Text));
    }

    private static Pos FindWalkableNeighbor(Game game, Pos target)
    {
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (game.World.Overworld.Walkable(p)) return p;
        }
        throw new InvalidOperationException("target has no walkable neighbor");
    }

    private static char KeyToward(Pos from, Pos to) => (to.X - from.X, to.Y - from.Y) switch
    {
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
