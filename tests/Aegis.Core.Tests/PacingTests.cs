using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The storyteller's read-only season (D-145, plan 2026-07 D1): a teller
/// above the coarse tick that watches every tick night, makes its call
/// before the night from carried state (Space after hard beats, Press when
/// the run coasts), then records what actually happened. It draws no RNG,
/// writes no facts, and narrates nothing, so the tests hold the watching,
/// the calls, the heat arithmetic, the disagreement counters, the crossing's
/// cool-down, and the book's sameness under the same seed.
/// </summary>
public class PacingTests
{
    [Fact]
    public void TheTeller_WatchesEveryTickNight()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();
        Assert.Empty(game.Teller.Readings);

        Wait(game, SteadRaids.TickTurns * 2);
        Assert.Equal(2, game.Teller.Readings.Count);
        Assert.All(game.Teller.Readings, r => Assert.Equal(PacingCall.Steady, r.Call));
    }

    [Fact]
    public void TheRaid_Heats_ByItsTake()
    {
        // The camp stands and the dens raid from the first tick: a plain
        // night heats one, and the emboldened night after takes two and
        // heats two. Winter is never due before tick 3, so both are clean.
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        Wait(game, SteadRaids.TickTurns * 2);

        Assert.Equal(1, game.Teller.Readings[0].Heat);
        Assert.Equal(2, game.Teller.Readings[1].Heat);
        Assert.False(game.Teller.Readings[0].NightClaimed);
    }

    [Fact]
    public void TheClaimedNight_Heats()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();
        int due = game.Upcoming.Single().DueTick;

        Wait(game, SteadRaids.TickTurns * due);
        var winterNight = game.Teller.Readings[due - 1];
        Assert.True(winterNight.NightClaimed);
        Assert.Equal(Storyteller.ClaimedHeat, winterNight.Heat);
    }

    [Fact]
    public void TheDeaths_CallForAir_BeforeTheNextNight()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();

        // Two deaths between ticks: six heat, hotter than SpaceAt. The call
        // lands on the NEXT night, because the teller decides before the
        // page is written, and that quiet night is no cooler a subject.
        for (int i = 0; i < 2; i++)
        {
            game.Debug_HurtPlayer(999);
            game.Debug_ForceDeathCheck();
        }
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(2 * Storyteller.DeathHeat, game.Teller.Readings[^1].Heat);

        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(PacingCall.Space, game.Teller.Readings[^1].Call);
        Assert.True(game.Teller.SpaceCalls >= 1);
    }

    [Fact]
    public void TheQuietRun_CallsForTheScrew()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();
        int due = game.Upcoming.Single().DueTick;

        // Let the winter land (its night is heat, which resets the streak),
        // then coast: three heatless ticks make the fourth night a Press
        // call, and with the deck held it stays quiet, so the counter that
        // audits unanswered pressure moves too.
        Wait(game, SteadRaids.TickTurns * due);
        Wait(game, SteadRaids.TickTurns * (Storyteller.PressAfter + 1));

        Assert.Equal(PacingCall.Press, game.Teller.Readings[^1].Call);
        Assert.True(game.Teller.PressCalls >= 1);
        Assert.True(game.Teller.QuietUnderPress >= 1);
    }

    [Fact]
    public void TheDeal_UnderACallForAir_IsCounted()
    {
        // The disagreement counters, held at the unit: a hot book calls for
        // air, and the season deals straight through it.
        var teller = new Storyteller();
        teller.NewWorld(0);
        teller.Observe(turn: 160, deathsNow: 2, nightClaimed: false, deckDealt: false,
            raidDelta: 0, raidTake: 0);
        teller.Observe(turn: 320, deathsNow: 2, nightClaimed: false, deckDealt: true,
            raidDelta: 0, raidTake: 0);

        Assert.Equal(1, teller.SpaceCalls);
        Assert.Equal(1, teller.DealtUnderSpace);
        Assert.Equal(0, teller.QuietUnderPress);
    }

    [Fact]
    public void ThePressedNight_AnsweredByTheSeason_IsNoComplaint()
    {
        var teller = new Storyteller();
        teller.NewWorld(0);
        for (int i = 0; i < Storyteller.PressAfter; i++)
            teller.Observe(160 * (i + 1), 0, false, false, 0, 0);
        teller.Observe(160 * 4, 0, nightClaimed: false, deckDealt: true, 0, 0);

        Assert.Equal(1, teller.PressCalls);
        Assert.Equal(0, teller.QuietUnderPress); // the deck answered the press itself
    }

    [Fact]
    public void TheCrossing_CoolsTheCarry_ButKeepsTheBook()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Wait(game, SteadRaids.TickTurns);
        int watched = game.Teller.Readings.Count;
        Assert.True(game.Teller.Readings[^1].Temperature >= Storyteller.SpaceAt);

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The book spans the run; the carried temperature does not: the new
        // world's first night is Steady, however hot the old one ended.
        Assert.Equal(watched, game.Teller.Readings.Count);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(watched + 1, game.Teller.Readings.Count);
        Assert.Equal(PacingCall.Steady, game.Teller.Readings[^1].Call);
    }

    [Fact]
    public void TheSameSeed_KeepsTheSameBook()
    {
        var one = new Game(42);
        var two = new Game(42);
        for (int i = 0; i < SteadRaids.TickTurns * 4; i++)
        {
            one.Apply(Command.Wait);
            two.Apply(Command.Wait);
        }
        Assert.Equal(one.Teller.Readings, two.Teller.Readings);
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
