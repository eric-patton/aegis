namespace Aegis.Core;

/// <summary>
/// A fact about the future (D-132, plan 2026-07 A1): the world learns to hold
/// what has not happened yet. Each entry is a thing that WILL happen, pinned
/// to a coarse tick (the same clock the factions act on, D-079): it may speak
/// ahead of itself (the foreshadow: an omen the stead can read and the bearer
/// can act on), it may be cancelled by the world changing under it (the
/// designed exit, D-023's rule extended forward in time), and when its tick
/// comes it fires. This is KDM's timeline translated into Aegis's idiom
/// (research/12): the future as visible, mutable data, so the bearer leaves
/// for a deep site worried about home for reasons they can name and act on.
/// Entries are runtime state rebuilt by replay (scheduled either from the
/// world's own seed or from replayed deeds), never serialized, and they are
/// the World bucket's: the crossing clears the calendar with the ledgers.
/// </summary>
public sealed class ScheduledFact
{
    /// <summary>Stable key: what this future is called in code, facts, and tests.</summary>
    public required string Key { get; init; }

    /// <summary>The coarse tick (counted from the world's start) when it lands.</summary>
    public int DueTick { get; set; }

    /// <summary>The tick the omen speaks, if any; -1 for futures announced at scheduling.</summary>
    public int ForeshadowTick { get; init; } = -1;

    /// <summary>
    /// Checked each tick before firing: true unschedules the future, narrated
    /// by Cancelled. This is what makes the calendar mutable rather than a fuse:
    /// a foreshadowed threat the bearer can act against is the machinery's point.
    /// </summary>
    public Func<Game, bool>? CancelWhen { get; init; }

    /// <summary>
    /// Checked at the due tick: true holds the firing to the next tick without
    /// unscheduling it (a den under attack defends its own; a muster waits).
    /// </summary>
    public Func<Game, bool>? HoldWhen { get; init; }

    /// <summary>The omen: narration plus an omen fact, so the future is perceivable ahead of itself.</summary>
    public Action<Game>? Foreshadow { get; init; }

    /// <summary>The firing: every scheduled future lands narrated and written (the mandatory hook).</summary>
    public required Action<Game> Fire { get; init; }

    /// <summary>The cancellation, narrated: a future that quietly fails to happen teaches nothing.</summary>
    public Action<Game>? Cancelled { get; init; }

    /// <summary>Whether the omen has spoken (foreshadow fires once).</summary>
    internal bool ForeshadowSpoken;
}
