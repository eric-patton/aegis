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

    /// <summary>Cycle of the first conversation with any world's Unbinder; 0 = never met (D-034).</summary>
    public int FirstUnbinderCycle { get; set; }

    /// <summary>Total unbindings ever performed on this character, all worlds.</summary>
    public int Unbindings { get; set; }

    /// <summary>
    /// Carried provisions (D-036): bought with coin, eaten anywhere with 'e'.
    /// On your person, so they survive death (unlike coin) and crossings.
    /// </summary>
    public int Rations { get; set; }

    // Gear (D-041): the other half of the build. Banked like attributes: the
    // remnant never takes it, and it crosses waygates untouched (vision secs 8, 10).

    public GearItem? Weapon { get; set; }
    public GearItem? Armor { get; set; }

    /// <summary>Gear owned but not worn. Small by design: six items exist in the world.</summary>
    public List<GearItem> Pack { get; } = [];

    /// <summary>Everything owned, equipped first, in the gear menu's stable order.</summary>
    public IEnumerable<GearItem> AllGear
    {
        get
        {
            if (Weapon is not null) yield return Weapon;
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

    /// <summary>Chance to slip a direct (non-telegraphed) attack, from Grace. Telegraphs are dodged by feet, not stats.</summary>
    public double DodgeChance => Math.Clamp((Attributes[Attr.Grace] - AttributeSet.Baseline) * 0.04, 0, 0.4);

    /// <summary>Effective max HP while Wounded: the Aegis is spent (D-008).</summary>
    public int EffectiveMaxHp => WoundedTurns > 0 ? Math.Max(1, MaxHp * 4 / 5) : MaxHp;
}

/// <summary>How the threshold resolved (D-039): unresolved, the keeping taken up, or laid down.</summary>
public enum Resolution { None, Kept, Refused }

public enum MonsterKind { Goblin, Wight, Severed, Graven, Hound }

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
    /// </summary>
    public bool Dormant { get; set; }

    public bool Alive => Hp > 0;
    public string Name => Kind switch
    {
        MonsterKind.Goblin => "goblin",
        MonsterKind.Wight => "wight",
        MonsterKind.Severed => "severed one",
        MonsterKind.Graven => "graven man",
        MonsterKind.Hound => "iron hound",
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

public enum IntentKind { CrushingBlow, BarrowBlade, SunderingCut, HurledStone, GravenFist, ThroatLunge }

/// <summary>
/// Villagers live beside their houses; the Unbinder (D-034) is the wandering
/// mender cast into every world under a fresh guise, and talks differently.
/// The Severed kind (D-038) is a former bearer met as a person, not a foe:
/// the game never makes them fightable, only listenable. The Smith (D-041)
/// keeps their own small menu so the villagers' nine digits stay unbreached.
/// </summary>
public enum NpcKind { Villager, Unbinder, Severed, Smith }

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
