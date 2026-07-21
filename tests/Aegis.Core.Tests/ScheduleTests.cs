using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Scheduled future facts (D-132, plan 2026-07 A1): the world's calendar. A
/// future foreshadows ahead of itself (the omen the stead can read), fires on
/// its coarse tick, and can be cancelled by the world changing under it, each
/// leg narrated. Two first uses: the hard winter (every valley, its tick from
/// the world's own seed, no cancelling weather) and the dens' muster (set by
/// the cull that teaches them dread, broken by the camp emptied first).
/// </summary>
public class ScheduleTests
{
    [Fact]
    public void EveryWorld_SetsItsWinter_FromItsOwnSeed()
    {
        var game = new Game(42);
        var (key, due) = Assert.Single(game.Upcoming);
        Assert.Equal("hard_winter", key);
        Assert.InRange(due, 3, 5);

        // Another world keeps its own calendar: same season, its own tick.
        var other = new Game(7);
        Assert.InRange(Assert.Single(other.Upcoming).DueTick, 3, 5);
    }

    [Fact]
    public void TheOmen_SpeaksATickAhead_AndTheWinterLands()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // a quiet season: the weather alone moves the stores
        int due = game.Upcoming.Single().DueTick;

        Wait(game, SteadRaids.TickTurns * (due - 1));
        Assert.True(game.World.Facts.Exists("omen", "hard_winter"));
        Assert.False(game.World.Facts.Exists("event", "hard_winter"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("A hard winter is coming"));
        Assert.Equal(SteadStores.Max, game.Stores); // the warning itself prices nothing

        int priceBefore = game.RationPrice;
        Wait(game, SteadRaids.TickTurns);
        Assert.True(game.World.Facts.Exists("event", "hard_winter"));
        Assert.Equal(SteadStores.Max - 2, game.Stores);
        Assert.Equal(priceBefore + 1, game.RationPrice); // bread rides the same stores the raids thin
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("comes down on"));
        // The valley's future has happened, and it put the tops' turn on the
        // calendar as it landed (D-149): the season climbs, one tick behind.
        Assert.Equal("wolf_winter", Assert.Single(game.Upcoming).Key);
    }

    [Fact]
    public void TheWinterNight_IsClaimedWhole_AndTheSeasonRecoversAfter()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        int due = game.Upcoming.Single().DueTick;

        Wait(game, SteadRaids.TickTurns * due);
        Assert.Equal(SteadStores.Max - 2, game.Stores); // no carts creak in behind the blizzard

        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(SteadStores.Max - 1, game.Stores); // the season resumes the next tick
    }

    [Fact]
    public void TheCull_SetsTheMusterOnTheCalendar_AndTheSteadSeesIt()
    {
        var game = WrathTests.ArrangeCamp(42);
        WrathTests.SlayNext(game);
        Assert.False(game.MusterLooms); // one dead raider is a grief, not yet an answer
        WrathTests.SlayNext(game);
        Assert.True(game.MusterLooms); // dread's rung: the hills begin to gather
        Assert.True(game.World.Facts.Exists("omen", "dens_muster"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("mustering over their dead"));

        // The raids topic reads the calendar from the doors.
        game.Debug_SetMode(MapMode.Overworld);
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);
        Assert.Contains("Mustering over their dead", game.Topics.First(t => t.Label == "The goblin raids").Answer);
    }

    [Fact]
    public void TheMuster_ComesDown_TwoTicksOut_GreedyByNumbers()
    {
        var game = WrathTests.ArrangeCamp(42);
        game.Debug_HoldTheDeck(); // choreographed ticks: the season's own deals stay in the box
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game);
        game.Debug_SetMode(MapMode.Overworld);

        // The cowed dens would not have raided at all: wrath holds them under
        // the raiding line, so the mustered night is unmistakably the answer.
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(0, game.Raids);
        Assert.True(game.MusterLooms);

        Wait(game, SteadRaids.TickTurns);
        Assert.False(game.MusterLooms);
        Assert.Equal(1, game.Raids);
        Assert.Equal(SteadStores.Max - SteadStores.BoldRaidTake, game.Stores); // greedy by numbers
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the mustered dens come down"));
    }

    [Fact]
    public void TheCampEmptied_BreaksTheMuster_Narrated()
    {
        var game = WrathTests.ArrangeCamp(42);
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game);
        Assert.True(game.MusterLooms);

        game.Debug_ClearCamp();
        game.Debug_SetMode(MapMode.Overworld);
        Wait(game, SteadRaids.TickTurns);

        Assert.False(game.MusterLooms);
        Assert.True(game.World.Facts.Exists("event", "muster_broken"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("broken up over"));
        Assert.Equal(0, game.Raids); // the raid that was coming never comes
    }

    [Fact]
    public void ADenUnderAttack_HoldsTheMusterNight()
    {
        var game = WrathTests.ArrangeCamp(42);
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game);
        // Quiet the rest of the camp without clearing it (the D-079 idiom), so
        // the bearer can stand in it past the due tick unbled.
        foreach (var m in game.Monsters.Where(m => m.Alive)) m.Hp = 0;

        Wait(game, SteadRaids.TickTurns * 2); // the due tick passes with the bearer inside
        Assert.True(game.MusterLooms);        // held, not fired, not lost
        Assert.Equal(0, game.Raids);

        game.Debug_SetMode(MapMode.Overworld);
        Wait(game, SteadRaids.TickTurns);
        Assert.False(game.MusterLooms); // the held night rides the next tick out
        Assert.Equal(1, game.Raids);
    }

    [Fact]
    public void TheCrossing_LeavesTheOldWorldItsFutures()
    {
        var game = WrathTests.ArrangeCamp(42);
        WrathTests.SlayNext(game);
        WrathTests.SlayNext(game);
        Assert.True(game.MusterLooms);
        game.Debug_ClearCamp();
        game.Debug_SetMode(MapMode.Overworld);

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.False(game.MusterLooms); // the old dens' answer stayed with the old dens
        var (key, due) = Assert.Single(game.Upcoming);
        Assert.Equal("hard_winter", key); // the new valley keeps its own calendar
        Assert.InRange(due, 3, 5);
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
