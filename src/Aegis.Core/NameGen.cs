namespace Aegis.Core;

/// <summary>Tiny syllable-weave name generator for the slice. Real culture-aware naming comes later.</summary>
public static class NameGen
{
    private static readonly string[] Openers =
        ["Bel", "Cal", "Dun", "Fen", "Gar", "Hal", "Ker", "Lor", "Mar", "Nor", "Or", "Pel", "Rav", "Sel", "Thal", "Vor", "Wen", "Yar"];

    private static readonly string[] Middles =
        ["a", "e", "i", "o", "u", "ar", "en", "il", "or", "un"];

    private static readonly string[] Closers =
        ["brook", "crag", "dale", "fell", "ford", "garde", "hollow", "mark", "mere", "moor", "reach", "stead", "vale", "watch", "wick"];

    private static readonly string[] WorldClosers =
        ["dor", "gard", "heim", "lund", "mora", "rath", "reth", "sara", "thas", "vane"];

    public static string Settlement(ref Rng rng)
        => rng.Pick(Openers) + rng.Pick(Middles) + rng.Pick(Closers);

    public static string World(ref Rng rng)
        => rng.Pick(Openers) + rng.Pick(Middles) + rng.Pick(WorldClosers);

    private static readonly string[] PersonClosers =
        ["da", "dric", "ga", "lin", "mund", "na", "ric", "rin", "sa", "wyn"];

    public static string Person(ref Rng rng)
        => rng.Pick(Openers) + rng.Pick(Middles) + rng.Pick(PersonClosers);
}
