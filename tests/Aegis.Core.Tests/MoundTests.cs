using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The third faction (D-106): the long mound's unquiet dead, and the relation
/// matrix's second edge. Grave-goods carried out of an unstilled barrow start
/// the mound's grudge: riled wights strike a point the harder, the mound
/// raises its own slain again on the coarse tick (capped), and the stead
/// speaks of the taller lights at its doors. The designed exit is the
/// stilling itself: dead laid to rest keep no ledgers.
/// </summary>
public class MoundTests
{
    [Fact]
    public void TheLadder_AndTheRiledBlow()
    {
        Assert.Equal(0, MoundGrudge.RungFor(0));
        Assert.Equal(1, MoundGrudge.RungFor(1));
        Assert.Equal("", MoundGrudge.TitleOf(0));
        Assert.Equal("marked by the long mound", MoundGrudge.TitleOf(1));
        Assert.Equal(3, MoundGrudge.Riled(0, 3)); // no grudge, no anger
        Assert.Equal(4, MoundGrudge.Riled(1, 3)); // the dead strike harder
    }

    [Fact]
    public void RobbingTheWalkingDead_StartsTheGrudge()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);

        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);

        Assert.True(barrow.ChestLooted);
        Assert.Equal(1, game.Grudge);
        Assert.True(game.World.Facts.Exists("grudge", "grave_goods"));
        var log = game.Log.Recent(8).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("marked whose pack it left in"));
        Assert.Contains(log, t => t.Contains("The dead keep short ledgers"));
        var snap = game.TakeSnapshot();
        Assert.Equal(1, snap.Grudge);
        Assert.Equal("marked by the long mound", snap.GrudgeTitle);
    }

    [Fact]
    public void AStilledBarrow_KeepsNoLedger()
    {
        var game = CrossedGame(42);
        game.Debug_ClearSite(SiteKind.Barrow);
        var barrow = EnterBarrow(game);

        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);

        Assert.True(barrow.ChestLooted);
        Assert.Equal(0, game.Grudge);
        Assert.False(game.World.Facts.Exists("grudge", "grave_goods"));
    }

    [Fact]
    public void TheMound_RaisesItsSlain_OnTheTick()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);
        var fallen = game.Monsters.First(m => m.Alive && m.SiteId == barrow.Id && m.Kind == MonsterKind.Wight);
        fallen.Hp = 0;
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        game.Debug_SetMode(MapMode.Overworld);

        Wait(game, SteadRaids.TickTurns);

        Assert.True(fallen.Alive); // whole again, at its grave's own strength
        Assert.Equal(barrow.Spawns.First(s => s.Kind == MonsterKind.Wight).Hp, fallen.Hp);
        Assert.True(game.World.Facts.Exists("event", "mound_restless"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("walks them again"));
    }

    [Fact]
    public void TheRising_StopsAtTheCap()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);
        var fallen = game.Monsters.First(m => m.Alive && m.SiteId == barrow.Id && m.Kind == MonsterKind.Wight);
        fallen.Hp = 0;
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        game.Debug_SetMode(MapMode.Overworld);

        for (int i = 0; i < MoundGrudge.RisenCap + 2; i++)
        {
            Wait(game, SteadRaids.TickTurns);
            if (fallen.Alive) fallen.Hp = 0; // cut down again; the mound answers only so many times
        }

        Assert.Equal(MoundGrudge.RisenCap, game.Log.Entries.Count(e => e.Text.Contains("walks them again")));
    }

    [Fact]
    public void TheStilling_SettlesTheGrudge()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        Assert.Equal(1, game.Grudge);
        game.Debug_SetMode(MapMode.Overworld);

        game.Debug_ClearSite(SiteKind.Barrow);

        Assert.Equal(0, game.Grudge);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("grudge goes out of the ground"));

        // And nothing rises from settled ground.
        Wait(game, SteadRaids.TickTurns);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("walks them again"));
    }

    [Fact]
    public void TheStead_SpeaksOfTheLights_WhileTheGrudgeBurns()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);
        game.Monsters.First(m => m.Alive && m.SiteId == barrow.Id && m.Kind == MonsterKind.Wight).Hp = 0;
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        game.Debug_SetMode(MapMode.Overworld);
        Wait(game, SteadRaids.TickTurns); // the rising writes mound_restless

        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2)); // the lane, among the doors
        ShameTests.StepStillNearAHouse(game);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("no tar for that"));
    }

    [Fact]
    public void TheTalkOfTheLights_StillsWithTheStilling()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);
        game.Monsters.First(m => m.Alive && m.SiteId == barrow.Id && m.Kind == MonsterKind.Wight).Hp = 0;
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        game.Debug_SetMode(MapMode.Overworld);
        Wait(game, SteadRaids.TickTurns);
        Assert.True(game.World.Facts.Exists("event", "mound_restless"));

        game.Debug_ClearSite(SiteKind.Barrow); // the grudge settles; the fact stays history

        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2)); // the lane, among the doors
        ShameTests.StepStillNearAHouse(game);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("no tar for that"));
    }

    [Fact]
    public void TheMoundTopic_ReadsTheGrudgeAloud()
    {
        // The mound follow-on (D-106, delivered D-113): while the mark stands,
        // the villagers' long-mound topic escalates from the old unease to the
        // pacing lights and the mound's tally. The bearer is never named; the
        // dogs are the only ones in the stead who know more.
        var game = CrossedGame(42);
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);

        NpcTests.BumpNpc(game, villager);
        string before = game.Topics.Single(t => t.Label == "The long mound").Answer;
        Assert.Contains("Of late there are lights", before);
        Assert.DoesNotContain("pacing a fence line", before);
        game.ApplyKey(' ');

        var barrow = EnterBarrow(game);
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        Assert.Equal(1, game.Grudge);
        game.Debug_SetMode(MapMode.Overworld);

        NpcTests.BumpNpc(game, villager);
        string during = game.Topics.Single(t => t.Label == "The long mound").Answer;
        Assert.Contains("pacing a fence line", during);
        Assert.Contains("keeps the count", during);
        game.ApplyKey(' ');

        // The stilling settles the grudge, and the talk goes back to a debt owed.
        game.Debug_ClearSite(SiteKind.Barrow);
        NpcTests.BumpNpc(game, villager);
        string after = game.Topics.Single(t => t.Label == "The long mound").Answer;
        Assert.Contains("Quiet up there now", after);
        Assert.DoesNotContain("pacing a fence line", after);
    }

    [Fact]
    public void TheCrossing_LeavesTheGrudgeBehind()
    {
        var game = CrossedGame(42);
        var barrow = EnterBarrow(game);
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        Assert.Equal(1, game.Grudge);
        game.Debug_SetMode(MapMode.Overworld);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(3, game.Cycle);

        Assert.Equal(0, game.Grudge);
        var snap = game.TakeSnapshot();
        Assert.Equal(0, snap.Grudge);
        Assert.Equal("", snap.GrudgeTitle);
    }

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

    private static Site EnterBarrow(Game game)
    {
        var barrow = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal(MapMode.Site, game.Mode);
        return barrow;
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
