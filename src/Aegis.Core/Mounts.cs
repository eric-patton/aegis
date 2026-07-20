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
}
