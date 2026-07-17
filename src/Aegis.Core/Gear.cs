namespace Aegis.Core;

public enum GearSlot { Weapon, Armor }

/// <summary>
/// A piece of equipment (D-041, the D-015 contract made real): requirements are
/// printed, and falling short penalizes rather than blocks. Gear lives on the
/// bearer, so like rations it survives death (the remnant takes coin and essence
/// only) and crosses waygates untouched (vision sec 10: character carries fully).
/// Wear is the D-025 auto-scaling sink: use dulls it, and the smith prices the
/// mending off the item's own value.
/// </summary>
public sealed class GearItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required GearSlot Slot { get; init; }

    /// <summary>Weapon: added to every full swing. Armor: subtracted from every hit taken (never below 1).</summary>
    public required int Bonus { get; init; }

    public required Attr ReqAttr { get; init; }
    public required int Req { get; init; }

    /// <summary>Coin value: what the smith charges whole, and what repair is priced against.</summary>
    public required int Value { get; init; }

    public required int MaxWear { get; init; }
    public int Wear { get; set; }

    /// <summary>Which skill a weapon trains and draws on (D-042). Armor ignores it; bare hands are Brawling.</summary>
    public SkillId Family { get; init; } = SkillId.Brawling;

    /// <summary>Fully worn: a dull edge or cut batting. Half the good, until the smith sees it.</summary>
    public bool Worn => Wear >= MaxWear;

    public bool MeetsReq(AttributeSet attrs) => attrs[ReqAttr] >= Req;

    /// <summary>
    /// Under-requirement halves the good; full wear halves it again. Penalized,
    /// never blocked: you can swing the too-big maul, badly, and the maul itself
    /// tells you what to become.
    /// </summary>
    public int EffectiveBonus(AttributeSet attrs)
    {
        int bonus = Bonus;
        if (!MeetsReq(attrs)) bonus /= 2;
        if (Worn) bonus /= 2;
        return bonus;
    }

    /// <summary>What the smith asks to put this right: half the item's value at full wear, scaled down, never free while worn at all.</summary>
    public int RepairPrice => Wear == 0 ? 0 : Math.Max(1, (Wear * Value + 2 * MaxWear - 1) / (2 * MaxWear));
}

/// <summary>
/// The authored gear catalog (D-041). Five items, each with one home: the smith
/// stocks the plain three; the grave-iron blade waits in the barrow's chest and
/// the carver's maul in the quarry's toolcache (site loot beyond coin, the D-033
/// deferral). Instances are minted fresh so wear never leaks between games.
/// </summary>
public static class GearCatalog
{
    public static readonly string[] SmithStock = ["woodaxe", "quilted_jack", "riveted_shirt"];

    public static GearItem Create(string id) => id switch
    {
        "woodaxe" => new GearItem
        {
            Id = id, Name = "woodsman's axe", Slot = GearSlot.Weapon,
            Bonus = 2, ReqAttr = Attr.Might, Req = 5, Value = 8, MaxWear = 40,
            Family = SkillId.Hafted,
        },
        "quilted_jack" => new GearItem
        {
            Id = id, Name = "quilted jack", Slot = GearSlot.Armor,
            Bonus = 1, ReqAttr = Attr.Vigor, Req = 5, Value = 8, MaxWear = 40,
        },
        "riveted_shirt" => new GearItem
        {
            Id = id, Name = "riveted shirt", Slot = GearSlot.Armor,
            Bonus = 2, ReqAttr = Attr.Vigor, Req = 7, Value = 20, MaxWear = 50,
        },
        "grave_iron" => new GearItem
        {
            Id = id, Name = "grave-iron blade", Slot = GearSlot.Weapon,
            Bonus = 4, ReqAttr = Attr.Might, Req = 7, Value = 18, MaxWear = 45,
            Family = SkillId.Blades,
        },
        "carvers_maul" => new GearItem
        {
            Id = id, Name = "carver's maul", Slot = GearSlot.Weapon,
            Bonus = 5, ReqAttr = Attr.Might, Req = 8, Value = 26, MaxWear = 45,
            Family = SkillId.Hafted,
        },
        _ => throw new ArgumentException($"Unknown gear id: {id}"),
    };
}
