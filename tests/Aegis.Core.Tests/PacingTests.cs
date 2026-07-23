using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The teller's bounded season-deck authority (D-145, D-160).</summary>
public class PacingTests
{
    [Fact]
    public void TheLiveDeck_DeclaresEveryCardElastic()
    {
        Assert.Empty(Game.Debug_ValidateSteadDeck());
        var cards = Game.Debug_SteadDeckPacing();
        Assert.Equal(7, cards.Count);
        Assert.All(cards, card => Assert.Equal(DeckPacingClass.Elastic, card.Pacing));
    }

    [Fact]
    public void MissingOrInvalidPacing_FailsClosedAndFailsValidation()
    {
        var missing = Card("missing", null);
        var invalid = Card("invalid", (DeckPacingClass)99);

        Assert.False(SteadDeckValidation.IsElastic(missing));
        Assert.False(SteadDeckValidation.IsElastic(invalid));
        var failures = SteadDeckValidation.Validate([missing, invalid]);
        Assert.Contains(failures, f => f.Contains("has no pacing classification"));
        Assert.Contains(failures, f => f.Contains("invalid pacing classification 99"));
    }

    [Fact]
    public void TheTeller_WatchesEveryTickAndConsumesOneCadenceRoll()
    {
        var game = QuietGame();
        Wait(game, SteadRaids.TickTurns * 2);

        Assert.Equal(2, game.Teller.Readings.Count);
        Assert.Equal(2, game.Teller.DeckCadenceRolls);
        Assert.All(game.Teller.Readings, r => Assert.Equal(PacingCall.Steady, r.Call));
    }

    [Fact]
    public void Raids_KeepTheirExistingHeatByActualTake()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        Wait(game, SteadRaids.TickTurns * 2);

        Assert.Equal(1, game.Teller.Readings[0].Heat);
        Assert.Equal(2, game.Teller.Readings[1].Heat);
        Assert.False(game.Teller.Readings[0].NightClaimed);
    }

    [Fact]
    public void DeathHeat_ShapesTheFollowingCall()
    {
        var game = QuietGame();
        game.Debug_HoldTheDeck();
        for (int i = 0; i < 2; i++)
        {
            game.Debug_HurtPlayer(999);
            game.Debug_ForceDeathCheck();
        }

        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(2 * Storyteller.DeathHeat, game.Teller.Readings[^1].Heat);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(PacingCall.Space, game.Teller.Readings[^1].Call);
    }

    [Fact]
    public void Press_PromotesAMissedRollIntoAnEligibleDeal()
    {
        var game = QuietGame();
        game.Debug_SetPacingCarry(temperature: 0, quietTicks: Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(false);

        Wait(game, SteadRaids.TickTurns);

        var reading = game.Teller.Readings[^1];
        Assert.Equal(PacingCall.Press, reading.Call);
        Assert.False(reading.CadenceSucceeded);
        Assert.Equal(PacingDeckOutcome.PressForcedDeal, reading.DeckOutcome);
        Assert.NotNull(reading.CardKey);
        Assert.Equal(1, game.Teller.PressForcedDeals);
        Assert.Equal(0, game.Teller.NaturalDeals);
    }

    [Fact]
    public void Press_NaturalSuccessRemainsNatural()
    {
        var game = QuietGame();
        game.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(true);

        Wait(game, SteadRaids.TickTurns);

        var reading = game.Teller.Readings[^1];
        Assert.Equal(PacingCall.Press, reading.Call);
        Assert.Equal(PacingDeckOutcome.NaturalDeal, reading.DeckOutcome);
        Assert.Equal(1, game.Teller.NaturalDeals);
        Assert.Equal(0, game.Teller.PressForcedDeals);
    }

    [Fact]
    public void AnElasticDeal_ResetsPressureForThreeNewQuietNights()
    {
        var teller = new Storyteller();
        teller.NewWorld(0);
        teller.DebugSetCarry(0, Storyteller.PressAfter);

        Observe(teller, cadence: false, PacingDeckOutcome.PressForcedDeal, "one");
        for (int i = 0; i < Storyteller.PressAfter; i++)
            Assert.Equal(PacingCall.Steady, Observe(teller, false, PacingDeckOutcome.CadenceMiss).Call);
        Assert.Equal(PacingCall.Press, teller.BeginTick());
    }

    [Fact]
    public void Space_SuppressesOneSuccessThenLetsTheEpisodeDealNaturally()
    {
        var game = QuietGame();
        game.Debug_SetPacingCarry(temperature: 6, quietTicks: 0);
        game.Debug_QueueDeckCadence(true, true);

        Wait(game, SteadRaids.TickTurns);
        var first = game.Teller.Readings[^1];
        Assert.Equal(PacingCall.Space, first.Call);
        Assert.Equal(PacingDeckOutcome.SpaceSuppressed, first.DeckOutcome);
        Assert.Null(first.CardKey);

        Wait(game, SteadRaids.TickTurns);
        var second = game.Teller.Readings[^1];
        Assert.Equal(PacingCall.Space, second.Call);
        Assert.True(second.SpaceAllowanceSpentAtCall);
        Assert.Equal(PacingDeckOutcome.NaturalDeal, second.DeckOutcome);
        Assert.Equal(1, game.Teller.SpaceSuppressions);
        Assert.Equal(1, game.Teller.SpaceCallsAfterAllowanceSpent);
    }

    [Fact]
    public void Space_DoesNotSpendItsSuppressionOnAMissedRoll()
    {
        var game = QuietGame();
        game.Debug_SetPacingCarry(temperature: 6, quietTicks: 0);
        game.Debug_QueueDeckCadence(false, true);

        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(PacingDeckOutcome.CadenceMiss, game.Teller.Readings[^1].DeckOutcome);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(PacingDeckOutcome.SpaceSuppressed, game.Teller.Readings[^1].DeckOutcome);
        Assert.False(game.Teller.Readings[^1].SpaceAllowanceSpentAtCall);
    }

    [Fact]
    public void Press_WithNoEligibleHandCreatesNothing()
    {
        var game = QuietGame();
        game.Debug_SetSeason(WorldSeason.Winter);
        game.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(false);

        Wait(game, SteadRaids.TickTurns);

        Assert.Equal(PacingDeckOutcome.NoEligibleHand, game.Teller.Readings[^1].DeckOutcome);
        Assert.Equal(1, game.Teller.PressCallsWithNoEligibleHand);
        Assert.Equal(0, game.Teller.PressForcedDeals);
    }

    [Fact]
    public void AClaimedNight_BlocksPressWithoutMovingTheFuture()
    {
        var game = QuietGame();
        int due = game.Upcoming.Single().DueTick;
        Wait(game, SteadRaids.TickTurns * (due - 1));
        game.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(false);

        Wait(game, SteadRaids.TickTurns);

        var reading = game.Teller.Readings[^1];
        Assert.Equal(PacingCall.Press, reading.Call);
        Assert.True(reading.NightClaimed);
        Assert.Equal(Storyteller.ClaimedHeat, reading.Heat);
        Assert.Equal(PacingDeckOutcome.ProtectedNight, reading.DeckOutcome);
        Assert.Equal(1, game.Teller.PressBlockedByProtectedNights);
        Assert.DoesNotContain(game.Upcoming, future => future.Key == "hard_winter");
    }

    [Fact]
    public void AnElasticCardFuture_BecomesProtectedImmediately()
    {
        var game = QuietGame();
        game.Debug_SetSeason(WorldSeason.Spring);
        game.Debug_SetWeather(ClimateBand.Lowlands, WeatherFamily.Calm);
        game.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(false);
        Wait(game, SteadRaids.TickTurns);

        Assert.Equal("fords_washout", game.Teller.Readings[^1].CardKey);
        Assert.Contains(game.Upcoming, future => future.Key == "fords_washout");

        game.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(false);
        Wait(game, SteadRaids.TickTurns);

        Assert.Equal(PacingDeckOutcome.ProtectedNight, game.Teller.Readings[^1].DeckOutcome);
        Assert.True(game.Teller.Readings[^1].NightClaimed);
        Assert.DoesNotContain(game.Upcoming, future => future.Key == "fords_washout");
    }

    [Fact]
    public void Space_SelectsNoCardAndCarriesNoBacklogAcrossASeasonGate()
    {
        var game = QuietGame();
        game.Debug_SetPacingCarry(6, 0);
        game.Debug_QueueDeckCadence(true);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(PacingDeckOutcome.SpaceSuppressed, game.Teller.Readings[^1].DeckOutcome);
        Assert.Null(game.Teller.Readings[^1].CardKey);

        game.Debug_SetSeason(WorldSeason.Winter);
        game.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        game.Debug_QueueDeckCadence(false);
        Wait(game, SteadRaids.TickTurns);

        Assert.Equal(PacingDeckOutcome.NoEligibleHand, game.Teller.Readings[^1].DeckOutcome);
        Assert.Empty(game.Teller.CardCounts);
    }

    [Fact]
    public void IdenticalState_KeepsWeightedSelectionAndTheWholeBookStable()
    {
        var one = QuietGame(99);
        var two = QuietGame(99);
        one.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        two.Debug_SetPacingCarry(0, Storyteller.PressAfter);
        one.Debug_QueueDeckCadence(false);
        two.Debug_QueueDeckCadence(false);

        Wait(one, SteadRaids.TickTurns);
        Wait(two, SteadRaids.TickTurns);

        Assert.Equal(one.Teller.Readings, two.Teller.Readings);
        Assert.Equal(one.Teller.CardCounts, two.Teller.CardCounts);
    }

    [Fact]
    public void JournalReplay_RebuildsSteeringAndDiagnosticsInSaveV96()
    {
        const ulong seed = 1234;
        string keys = "0" + new string('.', SteadRaids.TickTurns * 9);
        var live = new Game(seed, firstWake: true);
        foreach (char key in keys) live.ApplyKey(key);
        var replay = SaveCodec.Replay(seed, keys);

        Assert.Equal(98, SaveCodec.Version);
        Assert.Equal(live.Stores, replay.Stores);
        Assert.Equal(live.Upcoming, replay.Upcoming);
        Assert.Equal(live.Teller.Readings, replay.Teller.Readings);
        Assert.Equal(live.Teller.CardCounts, replay.Teller.CardCounts);
    }

    [Fact]
    public void Crossing_ResetsHeatQuietAndSpaceAuthorityButKeepsTheBook()
    {
        var game = QuietGame();
        game.Debug_SetPacingCarry(6, 0, spaceSuppressionSpent: true);
        game.Debug_QueueDeckCadence(true);
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(PacingCall.Space, game.Teller.Readings[^1].Call);
        int watched = game.Teller.Readings.Count;

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        game.Debug_ClearCamp();
        Wait(game, SteadRaids.TickTurns);
        Assert.Equal(watched + 1, game.Teller.Readings.Count);
        Assert.Equal(PacingCall.Steady, game.Teller.Readings[^1].Call);
        Assert.False(game.Teller.Readings[^1].SpaceAllowanceSpentAtCall);
    }

    [Fact]
    public void HeatAndGapDiagnostics_RecordCompletedNights()
    {
        var teller = new Storyteller();
        teller.NewWorld(0);
        Observe(teller, true, PacingDeckOutcome.NaturalDeal, "one");
        Observe(teller, false, PacingDeckOutcome.CadenceMiss);
        Observe(teller, true, PacingDeckOutcome.NaturalDeal, "one");
        Observe(teller, false, PacingDeckOutcome.CadenceMiss);
        Observe(teller, false, PacingDeckOutcome.CadenceMiss);
        Observe(teller, true, PacingDeckOutcome.NaturalDeal, "two");

        Assert.Equal(2, teller.MinimumDealGap);
        Assert.Equal(3, teller.MaximumDealGap);
        Assert.Equal(2, teller.LongestQuietStretch);
        Assert.Equal(new PacingCardCount(2, 0), teller.CardCounts["one"]);
        Assert.Equal(new PacingCardCount(1, 0), teller.CardCounts["two"]);
    }

    private static SteadEvent Card(string key, DeckPacingClass? pacing) => new()
    {
        Key = key,
        Pacing = pacing,
        When = _ => true,
        Draw = _ => { },
    };

    private static Game QuietGame(ulong seed = 42)
    {
        var game = new Game(seed);
        game.Debug_ClearCamp();
        return game;
    }

    private static PacingReading Observe(
        Storyteller teller,
        bool cadence,
        PacingDeckOutcome outcome,
        string? card = null)
    {
        var call = teller.BeginTick();
        teller.Observe(
            turn: (teller.Readings.Count + 1) * SteadRaids.TickTurns,
            call,
            deathsNow: 0,
            nightClaimed: false,
            cadenceSucceeded: cadence,
            deckOutcome: outcome,
            cardKey: card,
            raidDelta: 0,
            raidTake: 0);
        return teller.Readings[^1];
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }
}
