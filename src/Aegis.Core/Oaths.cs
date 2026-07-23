namespace Aegis.Core;

public enum OathId
{
    CrowdedDark,
    HungryRoad,
    SpentEdge,
    SlowMending,
    OldBlood,
    LeanDark,
    HushedName,
    ClosedDoor,
    LongCount,
}

/// <summary>One oath's catalog entry. Blurbs are written for the terms menu: one line, plain register.</summary>
public sealed record OathDef(OathId Id, string Name, string Blurb, int Weight);

/// <summary>
/// The covenants catalog (D-011, D-047), in the game's own register: oaths, sworn
/// at the waygate as the terms of the crossing. Each is a modifier on the NEXT
/// world, taken up freely and lapsing at its far gate. Effects are penalties
/// only; what they buy is Legend and a louder echo, never raw power. The summed
/// weight is the visible Threat score, called the burden.
/// </summary>
public static class OathCatalog
{
    public static readonly IReadOnlyList<OathDef> All =
    [
        new(OathId.CrowdedDark, "the crowded dark", "one more in every den", 1),
        new(OathId.HungryRoad, "the hungry road", "bread costs double", 1),
        new(OathId.SpentEdge, "the spent edge", "iron wears twice as fast", 1),
        new(OathId.SlowMending, "the slow mending", "wounds last twice as long", 1),
        // D-051's second wave. The old blood is the first oath to weigh 2: it
        // bites in every fight in the world, and the burden scale says so.
        new(OathId.OldBlood, "the old blood", "every blow lands 1 deeper", 2),
        new(OathId.LeanDark, "the lean dark", "essence comes up halved", 1),
        new(OathId.HushedName, "the hushed name", "the songs fall silent", 1),
        new(OathId.ClosedDoor, "the closed door", "regard comes one deed later", 1),
        new(OathId.LongCount, "the long count", "the toll drains every second turn", 1),
    ];

    public static OathDef Def(OathId id) => All.First(o => o.Id == id);

    /// <summary>Stable snake_case ids for snapshots and pilots.</summary>
    public static string IdOf(OathId id) => id switch
    {
        OathId.CrowdedDark => "crowded_dark",
        OathId.HungryRoad => "hungry_road",
        OathId.SpentEdge => "spent_edge",
        OathId.SlowMending => "slow_mending",
        OathId.OldBlood => "old_blood",
        OathId.LeanDark => "lean_dark",
        OathId.HushedName => "hushed_name",
        OathId.ClosedDoor => "closed_door",
        OathId.LongCount => "long_count",
        _ => id.ToString().ToLowerInvariant(),
    };
}
