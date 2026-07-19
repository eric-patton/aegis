namespace Aegis.Core;

/// <summary>
/// The use-based half of the build (D-042, the first slice of D-014/D-016):
/// the skills today's verbs can actually feed. Weapon families split by what
/// is truly in hand; Warding is armor-craft, fed only by blows the worn iron
/// turns; Ranged (D-050) is the bow's craft, fed only by shafts that find a body;
/// Hunting (D-070) is woodcraft, fed only by game brought down in the wilds;
/// Cooking (D-073) is the first craft, fed by raw meat turned to rations at a fire;
/// Survival (D-074) is the wider wilderness lore, fed for now by foraging the wood;
/// Spellcraft (D-091) is the said words' craft, fed only by workings that did work.
/// </summary>
public enum SkillId { Blades, Hafted, Brawling, Warding, Ranged, Hunting, Cooking, Survival, Spellcraft }

/// <summary>
/// Counted uses are the only state; levels are derived, never granted. A skill
/// therefore only ever reflects what the bearer actually did (D-016: skills
/// never respec), and every counted use already cost something real, so growth
/// is cost-gated by construction (D-014).
/// </summary>
public sealed class SkillSet
{
    public const int Count = 9;

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
        _ => id.ToString(),
    };
}
