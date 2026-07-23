namespace Aegis.Core;

/// <summary>
/// The permanent-ish marks the Death's Toll leaves (D-098, paying D-009): rare,
/// characterful, mechanically real. Bearer-state like the skills: death made
/// them and death never clears them, they cross waygates whole, and they are
/// rebuilt by replay, never serialized. Each will have its own costly road back
/// to parity (stage 2); until walked, the mark is simply carried.
/// </summary>
public enum ScarId { TakenEye, CrushedHand, HauntedLook }

/// <summary>
/// The Death's Toll (D-098, paying D-009): a visible count that fills on each
/// death and drains a point a turn. Die while it stands at the line or above
/// and that death converts to a scar, no roll: the fairness is legible before
/// the consequence lands. Routine, spaced deaths never scar; the first death of
/// a cluster warns, the next one collects. The waygate wipes the count clean
/// (the crossing is the Aegis's whole act); the scars cross with the body that
/// carries them.
/// </summary>
public static class DeathsToll
{
    /// <summary>What an ordinary death adds to the count.</summary>
    public const int Fill = 100;

    /// <summary>
    /// What a death under a boss-tier hand adds: the same shape, more of it
    /// (D-011's rule), so the window a great foe's death opens stands longer.
    /// </summary>
    public const int HeavyFill = 160;

    /// <summary>Die with the count at the line or above, and the death scars.</summary>
    public const int Line = 20;

    /// <summary>
    /// The fill a given death actually adds: Will above the baseline steels the
    /// soul against the count (the Toll resilience D-015 promised Will), a tenth
    /// of the fill per point, never below a floor that keeps clustering real.
    /// </summary>
    public static int TierContribution(int tier) => Math.Min(40, 10 * Math.Max(0, tier - 4));

    public static int FillFor(bool heavy, int will, int tier = 1) =>
        Math.Max(40, (heavy ? HeavyFill : Fill) + TierContribution(tier)
            - 10 * Math.Max(0, will - AttributeSet.Baseline));

    /// <summary>
    /// The scar a death's shape asks for (matched to the death, D-098): the
    /// uncanny kinds haunt, the thrown and lofted deaths take the eye, and any
    /// death under iron close in crushes the hand. A shapeless death (no known
    /// hand) matches nothing and falls to the fixed order.
    /// </summary>
    public static ScarId? Match(MonsterKind? kind, IntentKind? windup)
    {
        if (kind is null) return null;
        if (kind is MonsterKind.Wight or MonsterKind.Severed or MonsterKind.Graven or MonsterKind.Hart)
            return ScarId.HauntedLook;
        if (windup is IntentKind.HurledStone or IntentKind.LoftedStone)
            return ScarId.TakenEye;
        return ScarId.CrushedHand;
    }

    // The cure roads (D-098 stage 2, D-009's "surgeon, pilgrimage, salve"):
    // each scar's own way back, dear enough that the mark is lived with first.
    // The eye is the stillroom's longest work; the hand takes the smith's
    // brace (the superior-prosthetic hook, its mechanical edge still deferred);
    // what haunts is sung to rest at the songhall, paid in essence because it
    // is soul-stuff, and the walk out to the hall is the pilgrimage.

    public const int EyeCureCoin = 30;
    public const int BraceCoin = 24;
    public const int LayingEssence = 8;

    public static string NameOf(ScarId id) => id switch
    {
        ScarId.TakenEye => "the taken eye",
        ScarId.CrushedHand => "the crushed hand",
        _ => "the haunted look",
    };

    public static string IdOf(ScarId id) => id switch
    {
        ScarId.TakenEye => "taken_eye",
        ScarId.CrushedHand => "crushed_hand",
        _ => "haunted_look",
    };

    /// <summary>What the mark costs, said plainly the moment it lands (D-009's legibility).</summary>
    public static string CostOf(ScarId id) => id switch
    {
        ScarId.TakenEye => "The world's wind-ups read a shade slower now: what was keen is only read, and what was read is a blur.",
        ScarId.CrushedHand => "The knuckles never set right. Every swing costs a breath more wind.",
        _ => "Something followed you back, and the stead sees it looking out of you. Warmth comes harder now, and bread dearer.",
    };
}
