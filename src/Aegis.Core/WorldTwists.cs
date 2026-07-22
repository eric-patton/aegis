namespace Aegis.Core;

/// <summary>
/// The one generated law a tier-7+ world keeps (D-151/D-152). None is the
/// taught ladder, tiers 1-6. Values append forever if the library grows.
/// </summary>
public enum WorldTwist { None, HeldRoad, GraveMarket, HornedLaw }

/// <summary>The standing faith whose custom holds a Held Road world.</summary>
public enum RoadFaith { Shrine, Harrow }

/// <summary>
/// The authored twist library and its deterministic shuffle bag. The bag is
/// derived from the character's master seed and the hostility tier, so no
/// runtime state is serialized: every three eligible worlds contain all three
/// laws, and a refill cannot repeat the law that closed the previous bag.
/// </summary>
public static class WorldTwistCatalog
{
    public const int FirstTier = 7;
    public const int WaystonesPerWorld = 3;
    public const int RoadTithe = 1;
    public const int WightEssence = 8;
    public const int ProtectedHideFencePrice = 3;
    public const int WolfHideTownBonus = 1;

    private static readonly WorldTwist[] Library =
        [WorldTwist.HeldRoad, WorldTwist.GraveMarket, WorldTwist.HornedLaw];

    public static IReadOnlyList<WorldTwist> All => Library;

    public static WorldTwist ForCycle(ulong masterSeed, int tier)
    {
        if (tier < FirstTier) return WorldTwist.None;

        int wantedBlock = (tier - FirstTier) / Library.Length;
        int wantedSlot = (tier - FirstTier) % Library.Length;
        WorldTwist previous = WorldTwist.None;
        WorldTwist[] bag = [];
        for (int block = 0; block <= wantedBlock; block++)
        {
            bag = Shuffle(masterSeed, block);
            if (previous != WorldTwist.None && bag[0] == previous)
                (bag[0], bag[1]) = (bag[1], bag[0]);
            previous = bag[^1];
        }
        return bag[wantedSlot];
    }

    private static WorldTwist[] Shuffle(ulong masterSeed, int block)
    {
        var bag = Library.ToArray();
        var rng = new Rng(SeedTree.Derive(masterSeed, "twist-bag", block));
        for (int i = bag.Length - 1; i > 0; i--)
        {
            int j = rng.Range(0, i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }
        return bag;
    }

    public static string IdOf(WorldTwist twist) => twist switch
    {
        WorldTwist.HeldRoad => "held_road",
        WorldTwist.GraveMarket => "grave_market",
        WorldTwist.HornedLaw => "horned_law",
        _ => "none",
    };

    public static string NameOf(WorldTwist twist) => twist switch
    {
        WorldTwist.HeldRoad => "The Held Road",
        WorldTwist.GraveMarket => "The Grave Market",
        WorldTwist.HornedLaw => "The Horned Law",
        _ => "",
    };

    public static string FaithName(RoadFaith faith) => faith switch
    {
        RoadFaith.Shrine => "the shrine's keeping",
        RoadFaith.Harrow => "the harrow's order",
        _ => "",
    };

    public static int GravePricePerWight(bool leanDark) => WightEssence / (leanDark ? 4 : 2);
}
