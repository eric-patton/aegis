namespace Aegis.Core;

public sealed class Player
{
    public Pos Pos { get; set; }
    public AttributeSet Attributes { get; } = new();
    public int Hp { get; set; } = 20;
    public int Stamina { get; set; } = 10;
    public int Coin { get; set; }
    public int Essence { get; set; }

    /// <summary>Meta-currency minted from coin at each crossing (D-011); never raw power.</summary>
    public int Legend { get; set; }

    public int WoundedTurns { get; set; }
    public int Deaths { get; set; }

    /// <summary>
    /// A heavy blow wound up and not yet loosed (D-058): commitment runs both
    /// ways (D-004). 'w' and a line set the cell; the wind-up costs its stamina
    /// now and stands one turn, visible, for the field to answer; the next act
    /// looses it on this cell, hit or miss. Journal-derived like everything on
    /// the bearer: a save that lands mid-wind-up replays the same declaration.
    /// </summary>
    public Pos? HeaveTarget { get; set; }

    /// <summary>Cycle of the first conversation with any world's Unbinder; 0 = never met (D-034).</summary>
    public int FirstUnbinderCycle { get; set; }

    /// <summary>Total unbindings ever performed on this character, all worlds.</summary>
    public int Unbindings { get; set; }

    /// <summary>
    /// Carried provisions (D-036): bought with coin, eaten anywhere with 'e'.
    /// On your person, so they survive death (unlike coin) and crossings.
    /// </summary>
    public int Rations { get; set; }

    /// <summary>
    /// Hides taken from game in the wilds (D-070): the hunt's trade-goods, sold for
    /// coin at the woodward's bench (D-071). On your person like rations, so they
    /// survive death and cross the waygate.
    /// </summary>
    public int Hide { get; set; }

    /// <summary>
    /// Raw meat taken from a hart (D-073): the hunt's yield before a fire. Inedible
    /// as it is; cooked into rations at the wood's edge, the Cooking skill fattening
    /// the take. On your person like hides, so it survives death and crossings.
    /// </summary>
    public int RawMeat { get; set; }

    /// <summary>
    /// Herbs foraged from the wood (D-074): a trade-good sold at the wood's edge, the
    /// Survival skill fattening what a spot gives. On your person like hides, surviving
    /// death and the crossing.
    /// </summary>
    public int Herb { get; set; }

    // Gear (D-041): the other half of the build. Banked like attributes: the
    // remnant never takes it, and it crosses waygates untouched (vision secs 8, 10).

    public GearItem? Weapon { get; set; }
    public GearItem? Armor { get; set; }

    /// <summary>The strung bow (D-050): its own slot, so the axe never leaves the other hand.</summary>
    public GearItem? Bow { get; set; }

    /// <summary>Gear owned but not worn. Small by design: six items exist in the world.</summary>
    public List<GearItem> Pack { get; } = [];

    /// <summary>Everything owned, equipped first, in the gear menu's stable order.</summary>
    public IEnumerable<GearItem> AllGear
    {
        get
        {
            if (Weapon is not null) yield return Weapon;
            if (Bow is not null) yield return Bow;
            if (Armor is not null) yield return Armor;
            foreach (var item in Pack) yield return item;
        }
    }

    public bool OwnsGear(string id) => AllGear.Any(g => g.Id == id);

    /// <summary>The Aegis speaks once at the first iron taken up; never again.</summary>
    public bool GearLineHeard { get; set; }

    /// <summary>
    /// The use-grown track (D-042). Banked like attributes and gear: death never
    /// touches it, and it crosses waygates whole (vision secs 8, 10).
    /// </summary>
    public SkillSet Skills { get; } = new();

    /// <summary>The Aegis speaks once at the first skill rise; never again.</summary>
    public bool SkillLineHeard { get; set; }

    /// <summary>
    /// Knacks chosen at skill thresholds (D-046), in the order they were taken.
    /// A choice forecloses its siblings forever: like the skills that opened
    /// them, knacks never respec. Banked and crossing like the rest of the body.
    /// </summary>
    public List<PerkId> Perks { get; } = [];

    public bool HasPerk(PerkId id) => Perks.Contains(id);

    /// <summary>The Aegis speaks once at the first knack taken; never again.</summary>
    public bool KnackLineHeard { get; set; }

    /// <summary>The Aegis speaks once at the first level-4 knack (D-055); never again.</summary>
    public bool DeepKnackLineHeard { get; set; }

    /// <summary>
    /// Every draw of the string, hit or miss: the waxed string's clock (D-055).
    /// Wear frays on draws while marks are counted on hits, so the parity skip
    /// needs its own count. Journal-derived like everything on the character.
    /// </summary>
    public int Looses { get; set; }

    /// <summary>
    /// Lessons (D-052, the proficiency half of D-016): discrete know-how shown
    /// by the stead's own people, never trained. Banked and crossing like the
    /// rest of the body: hands keep what hands were taught.
    /// </summary>
    public List<LessonId> Lessons { get; } = [];

    public bool HasLesson(LessonId id) => Lessons.Contains(id);

    /// <summary>The Aegis speaks once at the first lesson shown; never again.</summary>
    public bool LessonLineHeard { get; set; }

    /// <summary>The Aegis marks the first hide sold at the bench (D-071); once only.</summary>
    public bool HideLineHeard { get; set; }

    /// <summary>The Aegis marks the first regard a stead ever holds for the bearer (D-076); once only.</summary>
    public bool RegardLineHeard { get; set; }

    /// <summary>The Aegis marks the first wrath the dens ever hold for the bearer (D-078); once only.</summary>
    public bool WrathLineHeard { get; set; }

    /// <summary>Whether the Aegis's one line on the taking has been heard (D-086); once per character.</summary>
    public bool ShameLineHeard { get; set; }

    /// <summary>
    /// The bestiary (D-059, paying D-004's oldest clause: telegraph clarity
    /// scales with what the bearer knows). How many of a kind's wind-ups the
    /// bearer has watched resolve, hit or miss, capped once the tell is read
    /// cold. Bearer-knowledge like the skills and the lessons: death never
    /// touches it and it crosses waygates whole (vision secs 8, 10), and it is
    /// rebuilt by replay, never serialized.
    /// </summary>
    public Dictionary<MonsterKind, int> Reads { get; } = new();

    /// <summary>
    /// The tier (Cycle) at which each read was last sharpened (D-061). A harder
    /// world's kinds move a shade strangely, so a read earned lower on the chain
    /// reads a notch softer here: the dulling is the gap between this stamp and
    /// the current tier. One wind-up watched here restamps it, snapping the read
    /// back cold. Like the bank itself, this is rebuilt by replay, never serialized.
    /// </summary>
    public Dictionary<MonsterKind, int> ReadTierStamp { get; } = new();

    /// <summary>One witnessed wind-up (or Wits 6) names a kind's tell; three (or Wits 8) reads its weight too.</summary>
    public const int ReadNamed = 1;
    public const int ReadKeen = 3;

    /// <summary>
    /// A wind-up watched to its end teaches the tell, hit or miss, and stamps
    /// the tier it was read at (D-061), so a later, harder world knows how far
    /// downstream the reading was. Capped: a tell read cold is read cold, but the
    /// stamp still refreshes, which is how a veteran re-sharpens in a single look.
    /// </summary>
    public void WitnessTell(MonsterKind kind, int tier = 1)
    {
        int seen = Reads.GetValueOrDefault(kind);
        if (seen < ReadKeen) Reads[kind] = seen + 1;
        ReadTierStamp[kind] = tier;
    }

    /// <summary>
    /// How clearly the bearer reads a kind's telegraph (D-059, dulled across the
    /// chain by D-061). Witnessed wind-ups bank toward the read; Wits above the
    /// baseline is a head start, so a keen-eyed bearer reads a stranger on sight.
    /// A harder world dulls an earned read by the tiers climbed since it was last
    /// sharpened, but never below its name (Read): the carry is never taken back.
    /// Dulling steps exactly one clarity tier, Keen down to Read, so what goes
    /// quiet is the weight of the blow; the name and the whole marked shape still
    /// hold (a named kind always shows where and how wide, never less safe), until
    /// one look here restamps it cold. Innate acuity (the Wits head start) is the
    /// bearer's own and is never dulled. This is clarity, never the dodge: the
    /// marked cell is always dodgeable by feet.
    /// </summary>
    public ReadTier ReadOf(MonsterKind kind, int tier = 1)
    {
        int banked = Reads.GetValueOrDefault(kind);
        int dulled = banked;
        if (banked >= ReadNamed)
        {
            dulled -= Math.Max(0, tier - ReadTierStamp.GetValueOrDefault(kind, tier));
            if (dulled < ReadNamed) dulled = ReadNamed;   // a named kind never dulls back to a blur
        }
        int read = dulled + Math.Max(0, Attributes[Attr.Wits] - AttributeSet.Baseline);
        return read >= ReadKeen ? ReadTier.Keen : read >= ReadNamed ? ReadTier.Read : ReadTier.Blur;
    }

    /// <summary>The Aegis speaks once at the first standing rise (D-048); never again.</summary>
    public bool StandingLineHeard { get; set; }

    /// <summary>
    /// Patron deeds pledged in the current world (D-054): the coin is already
    /// spent, and the pledge waits for the crossing to be weighed. Death never
    /// takes a pledge: coin dropped in a remnant was never pledged coin.
    /// </summary>
    public List<PatronDeedId> PledgedDeeds { get; } = [];

    /// <summary>
    /// Patron deeds weighed at a crossing (D-054): permanent, like everything
    /// on the character. Their traces stand in the songhall of every world the
    /// bearer's songs reach from then on.
    /// </summary>
    public List<PatronDeedId> PatronDeeds { get; } = [];

    /// <summary>Pledged or standing: either way the hall will not take it twice.</summary>
    public bool HasPatronDeed(PatronDeedId id) => PledgedDeeds.Contains(id) || PatronDeeds.Contains(id);

    /// <summary>The Aegis speaks once at the first pledge (D-054); never again.</summary>
    public bool PatronLineHeard { get; set; }

    // Arc-ladder state (D-037, design/story/aegis-arc.md sec 6). The fact graph is
    // per-world, so rung progress lives on the character. Each flag is set by the
    // storylet or crossing scene that completes its rung; later rungs gate on
    // earlier flags, never on cycle counts (the ladder's timing-tolerance rule).

    /// <summary>Rung 2a: the post-fight truth about the stranger-kind has been heard.</summary>
    public bool SeveredTruthHeard { get; set; }

    /// <summary>Rung 2b: the Aegis's crossing-scene admission has been spoken.</summary>
    public bool CrossingGuiltHeard { get; set; }

    /// <summary>Rung 3a: the shrine vision of the forging has been witnessed.</summary>
    public bool VisionSeen { get; set; }

    /// <summary>Rung 3c: the crossing-scene ledger reveal has been spoken.</summary>
    public bool LedgerHeard { get; set; }

    /// <summary>Rung 4a: the agency-model severed's side has been heard (D-038).</summary>
    public bool SeveredPeaceHeard { get; set; }

    /// <summary>Rung 4b: the essence-model severed's routine has been witnessed (D-038).</summary>
    public bool SeveredCostSeen { get; set; }

    /// <summary>Rung 4d: the commission has been spoken in full at a crossing (D-038).</summary>
    public bool CommissionHeard { get; set; }

    /// <summary>
    /// The threshold choice (D-039, arc sec 8). Both answers resolve the mystery;
    /// they differ in fiction and voice register, never in mechanics.
    /// </summary>
    public Resolution Resolution { get; set; }

    /// <summary>The Unbinder's layered identity, by trust not clock (0 = guise only).</summary>
    public int UnbinderRevealTier { get; set; }

    // Steady state (D-045, arc sec 9). Post-resolution families read this state;
    // like every rung flag it is journal-derived, never serialized.

    /// <summary>The cycle the threshold was answered in (0 = unanswered).</summary>
    public int ResolutionCycle { get; set; }

    /// <summary>Completed worlds' names, oldest first: the long song's verses.</summary>
    public List<string> WorldsWalked { get; } = [];

    /// <summary>Severed laid down gently rather than fought: the post-resolution verb.</summary>
    public int SeveredUnbound { get; set; }

    /// <summary>Severed caught whole and set into the songs rather than fought or laid down: the rarest grace (D-060), spent once ever.</summary>
    public int SeveredRestored { get; set; }

    /// <summary>The cycle a severed one was mended in, so the songs can run ahead of the bearer (0 = never).</summary>
    public int SeveredRestoredCycle { get; set; }

    /// <summary>The one permitted long thread, advanced a beat at a time (0 = not begun).</summary>
    public int ArgumentStage { get; set; }

    /// <summary>The cycle the argument last advanced in: a line at a time, never binged.</summary>
    public int ArgumentCycle { get; set; }

    /// <summary>Derived from Vigor (D-015): the humble baseline of 5 gives 20.</summary>
    public int MaxHp => 10 + Attributes[Attr.Vigor] * 2;

    /// <summary>Derived from Vigor: baseline 5 gives 10. A brawler's wind is their own (D-046).</summary>
    public int MaxStamina => 5 + Attributes[Attr.Vigor] + (HasPerk(PerkId.DeepBreath) ? 2 : 0);

    /// <summary>Flat melee bonus from Might above baseline.</summary>
    public int MeleeBonus => Math.Max(0, (Attributes[Attr.Might] - AttributeSet.Baseline) / 2);

    /// <summary>Flat ranged bonus from Grace above baseline (D-050): the eye and the release, not the arm.</summary>
    public int AimBonus => Math.Max(0, (Attributes[Attr.Grace] - AttributeSet.Baseline) / 2);

    /// <summary>Chance to slip a direct (non-telegraphed) attack, from Grace. Telegraphs are dodged by feet, not stats.</summary>
    public double DodgeChance => Math.Clamp((Attributes[Attr.Grace] - AttributeSet.Baseline) * 0.04, 0, 0.4);

    /// <summary>Effective max HP while Wounded: the Aegis is spent (D-008).</summary>
    public int EffectiveMaxHp => WoundedTurns > 0 ? Math.Max(1, MaxHp * 4 / 5) : MaxHp;
}

/// <summary>How the threshold resolved (D-039): unresolved, the keeping taken up, or laid down.</summary>
public enum Resolution { None, Kept, Refused }

public enum MonsterKind { Goblin, Wight, Severed, Graven, Hound, Carl, Boar, Warder, Thegn, Hart }

public sealed class Monster
{
    public required MonsterKind Kind { get; init; }
    public required Pos Pos { get; set; }

    /// <summary>Which site this monster haunts; only the current site's monsters act.</summary>
    public required string SiteId { get; init; }

    public int Hp { get; set; } = 8;
    public Intent? Intent { get; set; }

    /// <summary>
    /// Standing as a statue (D-040): graven men begin dormant and do nothing
    /// until the bearer comes near in their line of sight, or strikes them.
    /// The leaguer's warders (D-057) begin the same way, but wake as one:
    /// the first sighting is a horn, and the horn is for everybody.
    /// </summary>
    public bool Dormant { get; set; }

    /// <summary>
    /// Standing open (D-053): a carl whose blow is spent holds its board wide,
    /// and a boar that missed its charge stands blown. While it runs, the
    /// monster neither steps nor strikes, and shafts find it.
    /// </summary>
    public int ExposedTurns { get; set; }

    public bool Alive => Hp > 0;
    public string Name => Kind switch
    {
        MonsterKind.Goblin => "goblin",
        MonsterKind.Wight => "wight",
        MonsterKind.Severed => "severed one",
        MonsterKind.Graven => "graven man",
        MonsterKind.Hound => "iron hound",
        MonsterKind.Carl => "shield-carl",
        MonsterKind.Boar => "war-boar",
        MonsterKind.Warder => "sling-warder",
        MonsterKind.Thegn => "sword-thegn",
        MonsterKind.Hart => "hart",
        _ => "creature",
    };
}

/// <summary>
/// A telegraphed action (D-004): declared one turn before it resolves,
/// aimed at a cell, dodgeable by not being there when it lands.
/// </summary>
public sealed class Intent
{
    public required IntentKind Kind { get; init; }
    public required Pos TargetCell { get; init; }
    public int TurnsUntilResolve { get; set; } = 1;
}

public enum IntentKind { CrushingBlow, BarrowBlade, SunderingCut, HurledStone, GravenFist, ThroatLunge, SeaxStab, BoarCharge, LoftedStone }

/// <summary>
/// How clearly the bearer reads a kind's wind-up (D-059): a stranger's is a
/// Blur (danger, but not its shape or name), a Read shows where and what, a Keen
/// read knows its weight too. Familiarity and Wits sharpen the blur toward keen.
/// </summary>
public enum ReadTier { Blur, Read, Keen }

/// <summary>
/// Villagers live beside their houses; the Unbinder (D-034) is the wandering
/// mender cast into every world under a fresh guise, and talks differently.
/// The Severed kind (D-038) is a former bearer met as a person, not a foe:
/// the game never makes them fightable, only listenable. The Smith (D-041)
/// keeps their own small menu so the villagers' nine digits stay unbreached.
/// </summary>
public enum NpcKind { Villager, Unbinder, Severed, Smith, Skald }

/// <summary>
/// A named, placed person (D-031). Static in v1: they stand near their homes and
/// talk. The Id is stable within a world and is what facts reference.
/// </summary>
public sealed class Npc
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public required Pos Pos { get; init; }
    public NpcKind Kind { get; init; } = NpcKind.Villager;
}

/// <summary>What death leaves behind: unspent coin and Essence, one reclaim attempt (D-008).</summary>
public sealed class Remnant
{
    public required string MapId { get; init; }
    public required Pos Pos { get; init; }
    public required int Coin { get; init; }
    public required int Essence { get; init; }
}
