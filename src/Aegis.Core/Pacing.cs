namespace Aegis.Core;

/// <summary>The storyteller's call for one coarse-tick night (D-145, D-160).</summary>
public enum PacingCall { Steady, Space, Press }

/// <summary>
/// What pacing may do with a deck card (D-160). Null or an undefined value
/// fails closed as protected. Every live card must still declare a valid value.
/// </summary>
public enum DeckPacingClass { Protected, Elastic }

/// <summary>The deck-side result of one pacing call.</summary>
public enum PacingDeckOutcome
{
    CadenceMiss,
    NaturalDeal,
    PressForcedDeal,
    SpaceSuppressed,
    ProtectedNight,
    NoEligibleHand,
}

/// <summary>Natural and Press-forced arrivals for one elastic card.</summary>
public readonly record struct PacingCardCount(int Natural, int Forced);

/// <summary>
/// One line in the teller's run-wide diagnostic book. The call and Space
/// allowance are fixed before the night's systems advance. The remaining
/// fields record the completed night without exposing pacing in ordinary play.
/// </summary>
public readonly record struct PacingReading(
    int Turn,
    PacingCall Call,
    int Heat,
    int Temperature,
    bool NightClaimed,
    bool DeckDealt,
    bool CadenceSucceeded,
    PacingDeckOutcome DeckOutcome,
    string? CardKey,
    bool SpaceAllowanceSpentAtCall);

/// <summary>
/// The bounded storyteller above the coarse tick (D-145, D-160). It makes one
/// call from carried state before the tick advances, may steer only explicitly
/// elastic cards, draws no RNG, and observes protected systems without moving
/// them. World-scoped carry resets at a crossing while the diagnostic book and
/// its run-wide totals remain available to the journey harness.
/// </summary>
public sealed class Storyteller
{
    public const int DeathHeat = 3;
    public const int ClaimedHeat = 2;
    public const int Cooling = 1;
    public const int SpaceAt = 4;
    public const int PressAfter = 3;

    private readonly List<PacingReading> _readings = [];
    private readonly Dictionary<string, PacingCardCount> _cards = [];
    private int _temperature;
    private int _quietTicks;
    private int _lastDeaths;
    private bool _spaceSuppressionSpent;
    private PacingCall? _pendingCall;
    private bool _pendingSpaceSpent;
    private int _ticksSinceDeal = -1;

    public IReadOnlyList<PacingReading> Readings => _readings;
    public IReadOnlyDictionary<string, PacingCardCount> CardCounts => _cards;
    public int SpaceCalls { get; private set; }
    public int PressCalls { get; private set; }
    public int NaturalDeals { get; private set; }
    public int PressForcedDeals { get; private set; }
    public int SpaceSuppressions { get; private set; }
    public int PressBlockedByProtectedNights { get; private set; }
    public int PressCallsWithNoEligibleHand { get; private set; }
    public int SpaceCallsAfterAllowanceSpent { get; private set; }
    public int LongestQuietStretch { get; private set; }
    public int MinimumDealGap { get; private set; }
    public int MaximumDealGap { get; private set; }
    public int DeckCadenceRolls { get; private set; }

    /// <summary>Compatibility readings retained from D-145's original audit.</summary>
    public int DealtUnderSpace { get; private set; }
    public int QuietUnderPress { get; private set; }

    internal void NewWorld(int deathsNow)
    {
        _temperature = 0;
        _quietTicks = 0;
        _lastDeaths = deathsNow;
        _spaceSuppressionSpent = false;
        _pendingCall = null;
        _pendingSpaceSpent = false;
        _ticksSinceDeal = -1;
    }

    /// <summary>Fixes this tick's call before weather, schedules, or cadence act.</summary>
    internal PacingCall BeginTick()
    {
        if (_pendingCall is not null)
            throw new InvalidOperationException("The prior pacing call has not been observed.");

        var call = _temperature >= SpaceAt ? PacingCall.Space
            : _quietTicks >= PressAfter ? PacingCall.Press
            : PacingCall.Steady;
        if (call != PacingCall.Space) _spaceSuppressionSpent = false;

        _pendingCall = call;
        _pendingSpaceSpent = _spaceSuppressionSpent;
        return call;
    }

    /// <summary>Whether this Space episode still owns its one suppression.</summary>
    internal bool CanSuppress(PacingCall call) =>
        _pendingCall == call && call == PacingCall.Space && !_spaceSuppressionSpent;

    /// <summary>
    /// Records the completed night and advances carried heat and quiet. The
    /// cadence result is supplied by the deck, which consumes exactly one roll.
    /// </summary>
    internal void Observe(
        int turn,
        PacingCall call,
        int deathsNow,
        bool nightClaimed,
        bool cadenceSucceeded,
        PacingDeckOutcome deckOutcome,
        string? cardKey,
        int raidDelta,
        int raidTake)
    {
        if (_pendingCall != call)
            throw new InvalidOperationException("The observed pacing call does not match the call made for this tick.");

        bool deckDealt = deckOutcome is PacingDeckOutcome.NaturalDeal or PacingDeckOutcome.PressForcedDeal;
        if (deckDealt != (cardKey is not null))
            throw new ArgumentException("A dealt card must have a key, and an undealt outcome must not.", nameof(cardKey));

        int deaths = Math.Max(0, deathsNow - _lastDeaths);
        _lastDeaths = deathsNow;
        int heat = deaths * DeathHeat
            + (nightClaimed ? ClaimedHeat : 0)
            + raidDelta
            + Math.Max(0, raidTake - 1);

        _temperature = Math.Max(0, _temperature - Cooling) + heat;
        _quietTicks = heat == 0 && !deckDealt ? _quietTicks + 1 : 0;
        LongestQuietStretch = Math.Max(LongestQuietStretch, _quietTicks);
        DeckCadenceRolls++;

        if (call == PacingCall.Space)
        {
            SpaceCalls++;
            if (_pendingSpaceSpent) SpaceCallsAfterAllowanceSpent++;
            if (deckDealt) DealtUnderSpace++;
        }
        else if (call == PacingCall.Press)
        {
            PressCalls++;
            if (heat == 0 && !deckDealt) QuietUnderPress++;
            if (deckOutcome == PacingDeckOutcome.ProtectedNight) PressBlockedByProtectedNights++;
            if (deckOutcome == PacingDeckOutcome.NoEligibleHand) PressCallsWithNoEligibleHand++;
        }

        if (deckOutcome == PacingDeckOutcome.SpaceSuppressed)
        {
            SpaceSuppressions++;
            _spaceSuppressionSpent = true;
        }

        if (deckDealt)
        {
            if (_ticksSinceDeal >= 0)
            {
                int gap = _ticksSinceDeal + 1;
                MinimumDealGap = MinimumDealGap == 0 ? gap : Math.Min(MinimumDealGap, gap);
                MaximumDealGap = Math.Max(MaximumDealGap, gap);
            }
            _ticksSinceDeal = 0;

            var old = _cards.GetValueOrDefault(cardKey!);
            if (deckOutcome == PacingDeckOutcome.PressForcedDeal)
            {
                PressForcedDeals++;
                _cards[cardKey!] = old with { Forced = old.Forced + 1 };
            }
            else
            {
                NaturalDeals++;
                _cards[cardKey!] = old with { Natural = old.Natural + 1 };
            }
        }
        else if (_ticksSinceDeal >= 0)
            _ticksSinceDeal++;

        _readings.Add(new PacingReading(turn, call, heat, _temperature, nightClaimed,
            deckDealt, cadenceSucceeded, deckOutcome, cardKey, _pendingSpaceSpent));
        _pendingCall = null;
    }

    internal void DebugSetCarry(int temperature, int quietTicks, bool spaceSuppressionSpent = false)
    {
        _temperature = Math.Max(0, temperature);
        _quietTicks = Math.Max(0, quietTicks);
        _spaceSuppressionSpent = spaceSuppressionSpent;
        _pendingCall = null;
    }
}
