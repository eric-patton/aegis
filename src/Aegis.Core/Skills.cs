namespace Aegis.Core;

/// <summary>
/// The use-based half of the build (D-042, the first slice of D-014/D-016):
/// the skills today's verbs can actually feed. Weapon families split by what
/// is truly in hand; Warding is armor-craft, fed only by blows the worn iron
/// turns; Ranged (D-050) is the bow's craft, fed only by shafts that find a body;
/// Hunting (D-070) is woodcraft, fed only by game brought down in the wilds;
/// Cooking (D-073) is the first craft, fed by raw meat turned to rations at a fire;
/// Survival (D-074) is the wider wilderness lore, fed for now by foraging the wood;
/// Spellcraft (D-091) is the said words' craft, fed only by workings that did work;
/// Sleight (D-107) is the light hand's craft, the crime family's first skill, fed
/// only by lifts that came away unseen.
/// </summary>
public enum SkillId { Blades, Hafted, Brawling, Warding, Ranged, Hunting, Cooking, Survival, Spellcraft, Sleight }

/// <summary>
/// Counted uses are the only state; levels are derived, never granted. A skill
/// therefore only ever reflects what the bearer actually did (D-016: skills
/// never respec), and every counted use already cost something real, so growth
/// is cost-gated by construction (D-014).
/// </summary>
public sealed class SkillSet
{
    public const int Count = 10;

    private readonly int[] _uses = new int[Count];

    public int Uses(SkillId id) => _uses[(int)id];

    public void AddUse(SkillId id) => _uses[(int)id]++;

    /// <summary>
    /// Total uses a level asks for: 8, 20, 36, 56, 80... each level costing
    /// four more uses than the last (diminishing, never zero, returns).
    /// </summary>
    public static int UsesForLevel(int level) => 2 * level * level + 6 * level;

    public int Level(SkillId id)
    {
        int level = 0;
        while (UsesForLevel(level + 1) <= Uses(id)) level++;
        return level;
    }

    /// <summary>
    /// Flat combat good: +1 per two levels. Skill seasons a build; attributes
    /// and gear still carry it, so the three tracks stay comparable in weight.
    /// </summary>
    public int Bonus(SkillId id) => Level(id) / 2;

    public static string NameOf(SkillId id) => id switch
    {
        SkillId.Blades => "Blades",
        SkillId.Hafted => "Hafted",
        SkillId.Brawling => "Brawling",
        SkillId.Warding => "Warding",
        SkillId.Ranged => "Ranged",
        SkillId.Hunting => "Hunting",
        SkillId.Cooking => "Cooking",
        SkillId.Survival => "Survival",
        SkillId.Spellcraft => "Spellcraft",
        SkillId.Sleight => "Sleight",
        _ => id.ToString(),
    };
}

/// <summary>
/// The lift itself (D-107): picking a pocket, the crime family's second verb
/// after pilfering (D-086). The odds ride the Sleight skill alone: a green
/// hand is caught as often as not, a practiced one rarely, and no hand is
/// ever safe, because the cap keeps the last risk real. The take is small on
/// purpose: at stead scale crime pays in craft and in trouble, not in wealth.
/// </summary>
public static class Lifting
{
    /// <summary>What a lifted purse yields: uniform in [TakeMin, TakeMaxExclusive).</summary>
    public const int TakeMin = 2;
    public const int TakeMaxExclusive = 5;

    /// <summary>Odds a lift comes away unseen: half for a green hand, a twentieth per Sleight level, capped short of certainty.</summary>
    public static double ChanceFor(int sleightLevel) => Math.Min(0.85, 0.5 + 0.05 * sleightLevel);
}

/// <summary>
/// The lock itself (D-122): the crime family's third verb, and the first one
/// with no wronged party breathing. Old iron in the fighting deeps answers
/// the same light hand a pocket does, but argues harder: a green hand opens
/// roughly a third of what it tries, and no hand opens everything, because
/// the cap keeps old iron's last word real. One try per lock per world; a
/// lock that has taken a hand's measure does not give a second sitting.
/// </summary>
public static class Locks
{
    /// <summary>What an opened coffer yields: uniform in [TakeMin, TakeMaxExclusive).</summary>
    public const int TakeMin = 7;
    public const int TakeMaxExclusive = 15;

    /// <summary>Odds a lock gives: a third and change for a green hand, better per Sleight level, capped short of certainty.</summary>
    public static double ChanceFor(int sleightLevel) => Math.Min(0.85, 0.35 + 0.06 * sleightLevel);
}

/// <summary>
/// The crossed sill (D-127): burglary proper, crime's last named verb. The
/// latch is kinder than old iron but the house behind it is lived in, so the
/// odds sit between the pocket and the coffer, and the check is the whole of
/// it: in, through the dark, and out again with nobody woken. The take beats
/// the sill-reach and the pocket and stays under the guilt-free coffer,
/// because a home's kist holds a home's savings, not a payroll. One try per
/// door per world; a house that has heard the step listens harder after.
/// </summary>
public static class Burglary
{
    /// <summary>What a burgled kist yields: uniform in [TakeMin, TakeMaxExclusive).</summary>
    public const int TakeMin = 4;
    public const int TakeMaxExclusive = 10;

    /// <summary>Odds the house stays asleep: two in five for a green hand, a twentieth per Sleight level, capped short of certainty.</summary>
    public static double ChanceFor(int sleightLevel) => Math.Min(0.85, 0.4 + 0.05 * sleightLevel);
}
