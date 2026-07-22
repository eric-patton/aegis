namespace Aegis.Core;

public enum GearSlot { Weapon, Armor, Ranged }

/// <summary>
/// What the iron does beyond its numbers (D-056, paying D-004's oldest promise:
/// weapons change verbs). Arc: a paid swing carries through into everything
/// else at the bearer's side. Answer: a telegraphed blow stood through and
/// taken is answered over the iron, instantly and for free. Reach: the point
/// strikes two strides out ('t' sets it, a direction sends it). The bow's verb
/// is the loosed line itself (D-050), and bare fists keep their verbs in the
/// knacks, so both carry None.
/// </summary>
public enum MoveVerb { None, Arc, Answer, Reach }

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

    public required int MaxWear { get; set; }
    public int Wear { get; set; }

    /// <summary>Whether this piece has enough ironwork to take a tarn-iron temper (D-154).</summary>
    public bool TarnTemperable { get; init; }

    /// <summary>The bloom-temper is one-time per piece and travels with the gear.</summary>
    public bool TarnTempered { get; set; }

    /// <summary>Which skill a weapon trains and draws on (D-042). Armor ignores it; bare hands are Brawling.</summary>
    public SkillId Family { get; init; } = SkillId.Brawling;

    /// <summary>The weapon's verb (D-056). Hangs on the piece, not the family: the axe and the spear share a skill and not a verb.</summary>
    public MoveVerb Move { get; init; } = MoveVerb.None;

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
/// The authored gear catalog (D-041). Ten items, each with one home: the smith
/// stocks the plain five (the hunting bow, D-050, is bowyer's work the forge
/// takes in trade; the ash spear, D-056, carries the reach); the grave-iron blade waits in the barrow's chest, the
/// carver's maul in the quarry's toolcache, the wright's mail in the fallen
/// hall's coffer, the yew warbow in the ringfort's arms-chest (site loot
/// beyond coin, the D-033 deferral), and the scaled byrnie under the
/// leaguer's cist. Instances are minted fresh so wear never leaks between games.
/// </summary>
public static class GearCatalog
{
    public static readonly string[] SmithStock = ["woodaxe", "quilted_jack", "riveted_shirt", "hunting_bow", "ash_spear"];

    public static GearItem Create(string id) => id switch
    {
        "woodaxe" => new GearItem
        {
            Id = id, Name = "woodsman's axe", Slot = GearSlot.Weapon,
            Bonus = 2, ReqAttr = Attr.Might, Req = 5, Value = 8, MaxWear = 40,
            Family = SkillId.Hafted, Move = MoveVerb.Arc, TarnTemperable = true,
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
            TarnTemperable = true,
        },
        // The bearer's ranged verb (D-050) and the first iron gated on Grace
        // (the D-041 deferral): the bow asks the eye and the step, not the arm.
        // Wear is its whole ammunition: every draw frays the string a little,
        // and the smith restrings it like any edge.
        "hunting_bow" => new GearItem
        {
            Id = id, Name = "hunting bow", Slot = GearSlot.Ranged,
            Bonus = 2, ReqAttr = Attr.Grace, Req = 7, Value = 16, MaxWear = 40,
            Family = SkillId.Ranged,
        },
        // The reach (D-056): the smith's fifth ware and the catalog's third verb.
        // The asking sits between the axe's and the grave-iron's, because what
        // is bought is not the arm: it is the two strides the world keeps.
        "ash_spear" => new GearItem
        {
            Id = id, Name = "ash spear", Slot = GearSlot.Weapon,
            Bonus = 3, ReqAttr = Attr.Might, Req = 6, Value = 14, MaxWear = 40,
            Family = SkillId.Hafted, Move = MoveVerb.Reach, TarnTemperable = true,
        },
        "grave_iron" => new GearItem
        {
            Id = id, Name = "grave-iron blade", Slot = GearSlot.Weapon,
            Bonus = 4, ReqAttr = Attr.Might, Req = 7, Value = 18, MaxWear = 45,
            Family = SkillId.Blades, Move = MoveVerb.Answer, TarnTemperable = true,
        },
        "carvers_maul" => new GearItem
        {
            Id = id, Name = "carver's maul", Slot = GearSlot.Weapon,
            Bonus = 5, ReqAttr = Attr.Might, Req = 8, Value = 26, MaxWear = 45,
            Family = SkillId.Hafted, Move = MoveVerb.Arc, TarnTemperable = true,
        },
        "wrights_mail" => new GearItem
        {
            Id = id, Name = "wright's mail", Slot = GearSlot.Armor,
            Bonus = 3, ReqAttr = Attr.Vigor, Req = 9, Value = 34, MaxWear = 55,
            TarnTemperable = true,
        },
        // The watch's own answer (D-053): the ringfort punishes the bow, and
        // its arms-chest holds the better one: the deep ranged signature D-050
        // deferred. The boards stop this one too; the answer to the answer is
        // a heavier draw, never an exemption.
        "warbow" => new GearItem
        {
            Id = id, Name = "yew warbow", Slot = GearSlot.Ranged,
            Bonus = 4, ReqAttr = Attr.Grace, Req = 9, Value = 32, MaxWear = 50,
            Family = SkillId.Ranged,
        },
        // The holm-holder's harness (D-057): the armor ladder's next rung, out
        // of the leaguer's cist. Made for the besieged side of a siege: it sat
        // an age under falling stones, and the falling stones are still there.
        "scaled_byrnie" => new GearItem
        {
            Id = id, Name = "scaled byrnie", Slot = GearSlot.Armor,
            Bonus = 4, ReqAttr = Attr.Vigor, Req = 11, Value = 44, MaxWear = 60,
            TarnTemperable = true,
        },
        _ => throw new ArgumentException($"Unknown gear id: {id}"),
    };
}
