namespace Aegis.Core;

/// <summary>
/// The storyteller's call for a tick night (D-145, plan 2026-07 D1): what the
/// pacing layer would have done with the night, had it authority. Steady lets
/// the night run as the causes wrote it; Space is a call for air after hard
/// beats (it would have held the season's deck and any other non-causal
/// pressure); Press is a call for the screw when the run coasts (it would
/// have hastened an eligible card or storylet beat). Read-only for now: the
/// call is recorded, never obeyed.
/// </summary>
public enum PacingCall { Steady, Space, Press }

/// <summary>
/// One night's line in the teller's book (D-145): the call made before the
/// night, and what the night then actually held. Heat is the night's own
/// charge; Temperature is the carried charge after cooling. NightClaimed and
/// DeckDealt are the observed facts the disagreement counters read from.
/// </summary>
public readonly record struct PacingReading(
    int Turn, PacingCall Call, int Heat, int Temperature, bool NightClaimed, bool DeckDealt);

/// <summary>
/// The storyteller above the coarse tick (D-145, plan 2026-07 D1), read-only
/// first as the plan demands: it watches every tick night, makes its
/// editorial call BEFORE the night's events (from carried state, the way an
/// editor decides before the page is written), then observes what the causes
/// actually did and records the disagreement. It draws no RNG, writes no
/// facts, and adds no narration, so its presence cannot move replay, the
/// deck's stream, or a single baseline key: the whole point of the read-only
/// season is that the ledger is auditable across the sweep seeds before the
/// layer is ever allowed to steer. The raids' causal clock and the calendar's
/// scheduled facts are observations here, never subjects: the parked
/// authority question (which classes it may one day delay or hasten) stays
/// parked until this book argues for an answer.
/// </summary>
public sealed class Storyteller
{
    /// <summary>Heat per death since the last tick: the hardest beat the run has.</summary>
    public const int DeathHeat = 3;

    /// <summary>Heat for a night a scheduled future claimed whole (winter, washout, muster).</summary>
    public const int ClaimedHeat = 2;

    /// <summary>Cooling per tick: yesterday's drama fades a point a night.</summary>
    public const int Cooling = 1;

    /// <summary>Carried temperature at or above this calls Space: the run needs air.</summary>
    public const int SpaceAt = 4;

    /// <summary>Consecutive heatless ticks at or above this call Press: the run coasts.</summary>
    public const int PressAfter = 3;

    private readonly List<PacingReading> _readings = [];
    private int _temperature;
    private int _quietTicks;
    private int _lastDeaths;

    /// <summary>The whole book, oldest first: one reading per tick night watched.</summary>
    public IReadOnlyList<PacingReading> Readings => _readings;

    /// <summary>How often the teller called for air (Space).</summary>
    public int SpaceCalls { get; private set; }

    /// <summary>How often the teller called for the screw (Press).</summary>
    public int PressCalls { get; private set; }

    /// <summary>Nights the season dealt a card straight through a call for air.</summary>
    public int DealtUnderSpace { get; private set; }

    /// <summary>Pressed nights that stayed quiet anyway: no heat, no card.</summary>
    public int QuietUnderPress { get; private set; }

    /// <summary>
    /// A new world starts cool (D-145): the carried temperature, the quiet
    /// streak, and the deaths baseline reset at the crossing with the rest of
    /// the World bucket, but the book itself spans the run, because the run
    /// is what the audit reads.
    /// </summary>
    internal void NewWorld(int deathsNow)
    {
        _temperature = 0;
        _quietTicks = 0;
        _lastDeaths = deathsNow;
    }

    /// <summary>
    /// One tick night observed (D-145). The call is made from the carried
    /// state alone, before this night's heat is admitted: an editorial
    /// decision precedes the events it would have shaped. A raid heats by its
    /// take (a bold, unblunted night burns hotter than a plain one), a
    /// claimed night by the claim, a death hardest of all; then the carry
    /// cools a point and the night's heat joins it.
    /// </summary>
    internal void Observe(int turn, int deathsNow, bool nightClaimed, bool deckDealt,
        int raidDelta, int raidTake)
    {
        var call = _temperature >= SpaceAt ? PacingCall.Space
            : _quietTicks >= PressAfter ? PacingCall.Press
            : PacingCall.Steady;

        int deaths = Math.Max(0, deathsNow - _lastDeaths);
        _lastDeaths = deathsNow;
        int heat = deaths * DeathHeat
            + (nightClaimed ? ClaimedHeat : 0)
            + raidDelta
            + Math.Max(0, raidTake - 1);

        _temperature = Math.Max(0, _temperature - Cooling) + heat;
        _quietTicks = heat == 0 ? _quietTicks + 1 : 0;

        if (call == PacingCall.Space)
        {
            SpaceCalls++;
            if (deckDealt) DealtUnderSpace++;
        }
        else if (call == PacingCall.Press)
        {
            PressCalls++;
            if (heat == 0 && !deckDealt) QuietUnderPress++;
        }

        _readings.Add(new PacingReading(turn, call, heat, _temperature, nightClaimed, deckDealt));
    }
}
