namespace Aegis.Core;

public enum LessonId
{
    CleanDressing,
    TendedIron,
    Gleaning,
    Stillcraft,
}

/// <summary>One lesson's catalog entry: the full name for prose, the short for the sheet.</summary>
public sealed record LessonDef(LessonId Id, string Name, string Short, int Price);

/// <summary>
/// The proficiency half of D-016 (D-052), in the game's own register: lessons,
/// discrete know-how put into the bearer's hands by the stead's own people.
/// Never trained and never chosen at a threshold: shown, once, by someone who
/// knows, and kept for good. Lessons live on the Player like skills and gear:
/// death never takes them, and they cross waygates whole.
/// </summary>
public static class LessonCatalog
{
    public static readonly IReadOnlyList<LessonDef> All =
    [
        // Priced at 0: the herbwife teaches through her hands, not her purse.
        // The mend the bearer bought is the price of watching it done.
        new(LessonId.CleanDressing, "the clean dressing", "clean dressing", 0),
        new(LessonId.TendedIron, "the tended iron", "tended iron", 15),
        new(LessonId.Gleaning, "the gleaning", "gleaning", 10),
        // The stillroom's craft (D-090): the fourth lesson, the one D-087 held
        // out for a worthwhile effect. Its keep is independence: a taught bearer
        // steeps their own draught at any shrine in any world, herbwife or none.
        new(LessonId.Stillcraft, "the stillcraft", "stillcraft", 12),
    ];

    public static LessonDef Def(LessonId id) => All.First(l => l.Id == id);

    /// <summary>Stable snake_case ids for snapshots, pilots, and offer args.</summary>
    public static string IdOf(LessonId id) => id switch
    {
        LessonId.CleanDressing => "clean_dressing",
        LessonId.TendedIron => "tended_iron",
        LessonId.Gleaning => "gleaning",
        LessonId.Stillcraft => "stillcraft",
        _ => id.ToString().ToLowerInvariant(),
    };

    public static LessonId FromId(string id) => All.First(l => IdOf(l.Id) == id).Id;
}
