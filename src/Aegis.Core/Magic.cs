namespace Aegis.Core;

/// <summary>
/// The workings (D-091): words recovered from graven stones in the deep places,
/// said with Focus rather than wind. Magic v1 activates the two attributes that
/// waited on it: Mind drives a working's weight (see Player.SpellBonus) and
/// Will holds the pool and the grip (Player.MaxFocus, and the hold that keeps
/// a wound-up word through a wound). Workings are knowledge, like lessons:
/// death never takes them and they cross the waygate whole.
/// </summary>
public enum SpellId { Spark, Levin, Ward, Veilsight }

/// <summary>One working's shape: what it is called, what it asks, and whether it is held a turn before it lands.</summary>
public sealed record SpellDef(SpellId Id, string Name, string Short, int Focus, bool WindUp, string FoundLine);

public static class SpellCatalog
{
    /// <summary>Indexed by SpellId; the cast menu lists only what the bearer carries, in learn order.</summary>
    public static readonly IReadOnlyList<SpellDef> All =
    [
        new(SpellId.Spark, "the spark", "spark", 1, WindUp: false,
            "A small word, quick as struck flint: fire that flies where it is told, and boards are no answer to it."),
        new(SpellId.Levin, "the levin", "levin", 2, WindUp: true,
            "A heavy word, held one breath before it is said: a blow called down on marked ground, and everything can see where it will fall."),
        new(SpellId.Ward, "the ward", "ward", 2, WindUp: false,
            "A patient word: the air about the speaker thickens against blows, a while."),
        new(SpellId.Veilsight, "the veilsight", "veilsight", 2, WindUp: false,
            "A quiet word: the dark forgets how to hold its shapes, and what lives on a floor is known for what it is."),
    ];

    public static SpellDef Def(SpellId id) => All[(int)id];

    public static string IdOf(SpellId id) => Def(id).Short;

    /// <summary>
    /// What a site's stone teaches first (D-091): each kind of old fabric leans
    /// toward its own word, and a stone gives the first of its leaning the
    /// bearer does not yet carry. Decided at the reading, never at worldgen,
    /// so generation stays blind to the character.
    /// </summary>
    public static IReadOnlyList<SpellId> StonePreference(SiteKind kind) => kind switch
    {
        SiteKind.GoblinCamp => [SpellId.Spark, SpellId.Ward, SpellId.Veilsight, SpellId.Levin],
        SiteKind.Barrow => [SpellId.Veilsight, SpellId.Ward, SpellId.Levin, SpellId.Spark],
        SiteKind.Quarry => [SpellId.Levin, SpellId.Spark, SpellId.Ward, SpellId.Veilsight],
        SiteKind.Hall => [SpellId.Ward, SpellId.Levin, SpellId.Veilsight, SpellId.Spark],
        SiteKind.Ringfort => [SpellId.Levin, SpellId.Ward, SpellId.Spark, SpellId.Veilsight],
        _ => [SpellId.Veilsight, SpellId.Levin, SpellId.Ward, SpellId.Spark],
    };
}
