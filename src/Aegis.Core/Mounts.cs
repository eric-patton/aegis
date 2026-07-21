namespace Aegis.Core;

/// <summary>
/// The beasts of the road (D-100, the last niche of D-024): mortal,
/// world-bound, and overworld-only: a beast waits at a site's mouth while the
/// bearer goes below, and never crosses the waygate. Each kind has its own
/// road in (bought, storied, or won wild) and its own lean; stage 1 is the
/// stead's mule, sold at the steadholder's bench to a friend of the stead.
/// </summary>
public enum MountKind { Mule, Courser, FellPony }

public sealed class Mount
{
    public required MountKind Kind { get; init; }
    public required Pos Pos { get; set; }

    /// <summary>
    /// Which overworld the beast stands on (D-138, generalized D-146): a
    /// beast at the bearer's side takes a mouth with them; one left grazing
    /// keeps its own map, and its position means nothing on another.
    /// </summary>
    public Area Area { get; set; }

    /// <summary>Legacy read (D-146): true exactly on the road, as the D-138 bool meant it.</summary>
    public bool OnRoad => Area == Area.Road;

    /// <summary>
    /// Coin ridden in the saddlebags (D-100): what the beast carries does not
    /// fall with the bearer, but a raid that lands while the bearer is below
    /// takes the beast whole, bags and all. Banking is a choice of risks.
    /// </summary>
    public int Bags { get; set; }

    public string Name => Kind switch
    {
        MountKind.Mule => "the mule",
        MountKind.Courser => "the courser",
        _ => "the fell pony",
    };
}

public static class MountCatalog
{
    /// <summary>The stead's asking for its own beast (D-100): dear on purpose, and only ever to a friend.</summary>
    public const int MuleCoin = 40;

    /// <summary>The courser's saddlebags (D-100 stage 2): a racer's tack, not a banker's.</summary>
    public const int CourserBagsCap = 25;

    /// <summary>Bread it takes to win the wild fell pony (D-100 stage 2).</summary>
    public const int PonyFeedings = 3;

    /// <summary>What the bags will hold (D-100 stage 2): the mule and the pony carry without end; the courser travels light.</summary>
    public static int BagsCap(MountKind kind) => kind == MountKind.Courser ? CourserBagsCap : int.MaxValue;

    /// <summary>
    /// Where the ridden stride doubles (D-100): open grass for every beast;
    /// the courser takes the hills and the wood at the same pace, the
    /// fastest road there is.
    /// </summary>
    public static bool Strides(MountKind kind, Terrain t) =>
        t == Terrain.Grass || (kind == MountKind.Courser && t is Terrain.Hills or Terrain.Forest)
        || (kind == MountKind.FellPony && t == Terrain.Heath); // bred to the high ground (D-146)

    /// <summary>Only the fell pony keeps its nerve at an uncanny mouth (D-100 stage 2); the others bolt for home.</summary>
    public static bool Spooks(MountKind kind) => kind != MountKind.FellPony;

    /// <summary>The mouths whose tenants a mortal beast can smell (D-100): the barrow's dead, the quarry's stone men, the hall's iron pack, the mere's warders, the ring's keeper.</summary>
    public static bool UncannyMouth(SiteKind kind) =>
        kind is SiteKind.Barrow or SiteKind.Quarry or SiteKind.Hall or SiteKind.Leaguer or SiteKind.Hollow;
}
