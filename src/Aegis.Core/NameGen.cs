namespace Aegis.Core;

/// <summary>
/// Syllable-weave name generator, second pass (D-049). Each kind keeps its own sound:
/// worlds weave from their own opener pool so no world sounds like its steadholder,
/// steads and persons share the folk openers so a Kereda can keep a Keriford, and
/// regions (D-143) borrow the world openers over the land's plain closers, so a
/// country sounds of its world and reads with "the" in front.
/// Every weave guards its seams (no doubled letter where syllables meet) and the
/// world weave rerolls against the names a character has already walked, so the
/// long song never sings the same world twice. Culture-aware pools keyed to the
/// race list (D-017) come later.
/// </summary>
public static class NameGen
{
    // Folk openers: steads and persons. Consonant-final so every join onto a
    // vowel-initial middle is clean.
    private static readonly string[] Openers =
        ["Bel", "Cal", "Dun", "Fen", "Gar", "Hal", "Ker", "Lor", "Mar", "Nor", "Or", "Pel", "Rav", "Sel", "Thal", "Vor", "Wen", "Yar"];

    // World openers: a separate pool, so worlds carry their own tongue.
    private static readonly string[] WorldOpeners =
        ["Ald", "Ash", "Bran", "Cor", "Dag", "Eld", "Ern", "Fal", "Gorm", "Hald", "Isen", "Jor", "Kol", "Lang", "Mund", "Nar", "Osk", "Rand", "Stur", "Tor", "Ulf", "Vand", "Wulf", "Thrum"];

    private static readonly string[] Middles =
        ["a", "e", "i", "o", "u", "ar", "en", "il", "or", "un", "al", "el", "in", "on", "ur"];

    private static readonly string[] Closers =
        ["brook", "crag", "dale", "fell", "ford", "garde", "hollow", "mark", "mere", "moor", "reach", "stead", "vale", "watch", "wick", "bourne", "combe", "dell", "hythe", "leigh", "thorn", "worth"];

    private static readonly string[] WorldClosers =
        ["dor", "gard", "heim", "lund", "mora", "rath", "reth", "sara", "thas", "vane", "dun", "fast", "mar", "stad", "vald", "wald"];

    private static readonly string[] PersonClosers =
        ["da", "dric", "ga", "lin", "mund", "na", "ric", "rin", "sa", "wyn", "dis", "gar", "hild", "red", "ulf", "wen"];

    // Region closers (D-143): the land's own plain words, spoken with "the" in
    // front (the Brancorwold, the Isenheath). Regions share the world openers,
    // so a country sounds of its world's tongue, and keep their own closer
    // pool, disjoint from every other kind's, so no region can ever share a
    // full name with a world, a stead, or a person (the D-049 rule).
    private static readonly string[] RegionClosers =
        ["wold", "heath", "shaw", "holt", "hurst", "gill", "scar", "side", "down", "how"];

    // Raider pools (D-110): the dens keep their own tongue, short and bitten
    // off. Vowel-final openers onto consonant closers, so every seam is clean
    // and no den-name sounds like a stead or a steadholder.
    private static readonly string[] RaiderOpeners =
        ["Gna", "Skra", "Vre", "Zhu", "Sna", "Ghu", "Kri", "Bru", "Ska", "Dro", "Mau", "Tcha"];

    private static readonly string[] RaiderClosers =
        ["rg", "sh", "tch", "kk", "zz", "rk", "dz", "ng", "gg", "x"];

    /// <summary>Stead name: full weave mostly, a plain compound (Fenford) two times in five.</summary>
    public static string Settlement(ref Rng rng)
        => Weave(ref rng, Openers, Closers, shortInFive: 2);

    /// <summary>
    /// World name, woven to be unlike every name in <paramref name="taken"/>
    /// (the character's walked worlds). The reroll only consumes the names stream,
    /// and the taken list is itself journal-derived, so replay stays exact.
    /// </summary>
    public static string World(ref Rng rng, IReadOnlyCollection<string>? taken = null)
        => Weave(ref rng, WorldOpeners, WorldClosers, shortInFive: 2, taken);

    /// <summary>Person name: full weave mostly, a short call-name (Marwyn) one time in five.</summary>
    public static string Person(ref Rng rng)
        => Weave(ref rng, Openers, PersonClosers, shortInFive: 1);

    /// <summary>
    /// Region name (D-143): the country itself, rerolled against its world's
    /// other countries so no world names two stretches of ground the same.
    /// </summary>
    public static string Region(ref Rng rng, IReadOnlyCollection<string>? taken = null)
        => Weave(ref rng, WorldOpeners, RegionClosers, shortInFive: 2, taken);

    /// <summary>
    /// Raider name (D-110): always the short bitten weave, no middle syllable,
    /// rerolled against the names already given so a camp's roster never
    /// doubles a name.
    /// </summary>
    public static string Raider(ref Rng rng, IReadOnlyCollection<string>? taken = null)
        => Weave(ref rng, RaiderOpeners, RaiderClosers, shortInFive: 5, taken);

    private static string Weave(ref Rng rng, string[] openers, string[] closers, int shortInFive, IReadOnlyCollection<string>? taken = null)
    {
        string name = "";
        for (int attempt = 0; attempt < 64; attempt++)
        {
            string opener = rng.Pick(openers);
            string middle = rng.Next(5) < shortInFive ? "" : rng.Pick(Middles);
            string closer = rng.Pick(closers);
            name = opener + middle + closer;
            if (SeamsClean(opener, middle, closer) && (taken is null || !taken.Contains(name)))
                return name;
        }
        return name; // Unreachable in practice; the last draw stands so the weave never fails.
    }

    /// <summary>No doubled letter where syllables meet (Randdor, Pelarrath).</summary>
    private static bool SeamsClean(string opener, string middle, string closer)
        => middle.Length == 0
            ? opener[^1] != closer[0]
            : opener[^1] != middle[0] && middle[^1] != closer[0];
}
