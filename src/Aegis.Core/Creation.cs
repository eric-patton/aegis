namespace Aegis.Core;

/// <summary>
/// The five folk (D-092): fixed anchors grown from the world's own fiction
/// (D-017's structure with an original roster). Each is one attribute tilt and
/// one qualitative trait; every generated world rerolls their cultures and
/// standing, so the anchor is the blood, never the flag.
/// </summary>
public enum FolkId { Steadfolk, Emberwrought, Cairnborn, Heathborn, Wrightkin }

/// <summary>
/// The seven pasts (D-092): what the hands were before the catching. Each banks
/// level one of a skill, carries one small concrete extra, and writes a fact
/// the world can answer.
/// </summary>
public enum PastId { Soldier, Poacher, HedgeHealer, SmithsHand, ScribesWard, Wayfarer, Oathbreaker }

/// <summary>The precious things (D-092): one chosen keepsake, soul-bound, never dropped.</summary>
public enum ThingId { Word, FineArms, CraftKit, Purse, Keepsake }

/// <summary>
/// The burdens (D-093): one may be taken at the asking, and it buys a second
/// precious thing. Each is a live weight the worlds keep collecting on.
/// </summary>
public enum BurdenId { OldWound, HuntedPast, MarkedFace }

/// <summary>The vows (D-093): a private aim, chosen once, that the road can answer.</summary>
public enum VowId { Vengeance, Finding, Return }

/// <summary>One folk's catalog entry: tilt (may be null for the balanced stock) and the trait line.</summary>
public sealed record FolkDef(FolkId Id, string Name, Attr? TiltUp, Attr? TiltDown, string Blurb, string Trait);

/// <summary>One past's catalog entry: the skill it banked and the line the scene shows.</summary>
public sealed record PastDef(PastId Id, string Name, SkillId Skill, string Blurb);

/// <summary>One precious thing's catalog entry.</summary>
public sealed record ThingDef(ThingId Id, string Name, string Blurb);

/// <summary>One burden's catalog entry: the name, the scene's line, and what it costs.</summary>
public sealed record BurdenDef(BurdenId Id, string Name, string Blurb, string Price);

/// <summary>One vow's catalog entry.</summary>
public sealed record VowDef(VowId Id, string Name, string Blurb);

public static class CreationCatalog
{
    public static readonly IReadOnlyList<FolkDef> Folk =
    [
        new(FolkId.Steadfolk, "Steadfolk", null, null,
            "the common grain of the kindled worlds, stead-raised and kin-held",
            "a third shaping, and coin from home"),
        new(FolkId.Emberwrought, "Emberwrought", Attr.Mind, Attr.Vigor,
            "lines touched, generations back, by worlds kindled too near the fire",
            "a deeper well of focus, always"),
        new(FolkId.Cairnborn, "Cairnborn", Attr.Will, Attr.Grace,
            "the folk who keep barrows and speak for the buried, slow to startle",
            "reads come one tier keener"),
        new(FolkId.Heathborn, "Heathborn", Attr.Grace, Attr.Might,
            "edge-dwellers of the freshly kindled wilds, born hunters and pickers",
            "harvests yield one more, hide and sprig alike"),
        new(FolkId.Wrightkin, "Wrightkin", Attr.Might, Attr.Wits,
            "descended of the old order's bond-crafters; the hands remember",
            "carried gear wears half as fast"),
    ];

    public static readonly IReadOnlyList<PastDef> Pasts =
    [
        new(PastId.Soldier, "a soldier", SkillId.Blades, "drilled iron and kept ranks; a worn jack still fits"),
        new(PastId.Poacher, "a poacher", SkillId.Ranged, "took the lord's game quietly; the bow came along"),
        new(PastId.HedgeHealer, "a hedge-healer", SkillId.Alchemy, "knew the worts and what they mend; sprigs in the wallet"),
        new(PastId.SmithsHand, "a smith's-hand", SkillId.Warding, "worked the bellows and the awl; smiths know their own"),
        new(PastId.ScribesWard, "a scribe's-ward", SkillId.Spellcraft, "raised among old writings, and what they say of deep places"),
        new(PastId.Wayfarer, "a wayfarer", SkillId.Athletics, "lived off the roads between steads; rations spare"),
        new(PastId.Oathbreaker, "an oathbreaker", SkillId.Blades, "hard-schooled and twice-skilled, but the name is stained"),
    ];

    public static readonly IReadOnlyList<ThingDef> Things =
    [
        new(ThingId.Word, "a known word", "one working, carried from wherever the road began"),
        new(ThingId.FineArms, "fine arms", "grave-iron, above anything the early forges sell"),
        new(ThingId.CraftKit, "a craft kit", "the stillcraft in the hands, and six good sprigs"),
        new(ThingId.Purse, "a heavy purse", "twenty-five coin, honest or not"),
        new(ThingId.Keepsake, "an unassuming thing", "small, worn smooth, and it will not say what it is"),
    ];

    public static readonly IReadOnlyList<BurdenDef> Burdens =
    [
        new(BurdenId.OldWound, "an old wound", "a blade found you once, and found something it kept",
            "the brim of your blood sits two lower, always"),
        new(BurdenId.HuntedPast, "a hunted past", "something in the dens' kind knows your smell, world after world",
            "every world's raiders wake already wrathful at you"),
        new(BurdenId.MarkedFace, "a marked face", "steads look at you twice, and the second look is colder",
            "every stead's suspicion wakes already upon you"),
    ];

    public static readonly IReadOnlyList<VowDef> Vows =
    [
        new(VowId.Vengeance, "a vow of vengeance", "the raiding kind took something that cannot be given back"),
        new(VowId.Finding, "a vow of finding", "someone went down the road ahead of you and did not send word"),
        new(VowId.Return, "a vow of the road's end", "to walk until the road itself gives an answer"),
    ];

    public static FolkDef FolkOf(FolkId id) => Folk[(int)id];
    public static PastDef PastOf(PastId id) => Pasts[(int)id];
    public static ThingDef ThingOf(ThingId id) => Things[(int)id];
    public static BurdenDef BurdenOf(BurdenId id) => Burdens[(int)id];
    public static VowDef VowOf(VowId id) => Vows[(int)id];

    /// <summary>Attribute bounds at the shaping: creation never leaves the humble band (D-005).</summary>
    public const int ShapeFloor = 3;
    public const int ShapeCeiling = 7;
}

/// <summary>The creation scene's stages, in asking order (D-092; Burden/Vow/Face are D-093).</summary>
public enum CreationStage { Folk, Past, ShapeRaise, ShapePay, Thing, Burden, Vow, Face, Name, Review }
