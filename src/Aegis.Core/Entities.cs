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

    /// <summary>Derived from Vigor (D-015): the humble baseline of 5 gives 20.</summary>
    public int MaxHp => 10 + Attributes[Attr.Vigor] * 2;

    /// <summary>Derived from Vigor: baseline 5 gives 10.</summary>
    public int MaxStamina => 5 + Attributes[Attr.Vigor];

    /// <summary>Flat melee bonus from Might above baseline.</summary>
    public int MeleeBonus => Math.Max(0, (Attributes[Attr.Might] - AttributeSet.Baseline) / 2);

    /// <summary>Chance to slip a direct (non-telegraphed) attack, from Grace. Telegraphs are dodged by feet, not stats.</summary>
    public double DodgeChance => Math.Clamp((Attributes[Attr.Grace] - AttributeSet.Baseline) * 0.04, 0, 0.4);

    /// <summary>Effective max HP while Wounded: the Aegis is spent (D-008).</summary>
    public int EffectiveMaxHp => WoundedTurns > 0 ? Math.Max(1, MaxHp * 4 / 5) : MaxHp;
}

public enum MonsterKind { Goblin, Wight }

public sealed class Monster
{
    public required MonsterKind Kind { get; init; }
    public required Pos Pos { get; set; }

    /// <summary>Which site this monster haunts; only the current site's monsters act.</summary>
    public required string SiteId { get; init; }

    public int Hp { get; set; } = 8;
    public Intent? Intent { get; set; }
    public bool Alive => Hp > 0;
    public string Name => Kind switch
    {
        MonsterKind.Goblin => "goblin",
        MonsterKind.Wight => "wight",
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

public enum IntentKind { CrushingBlow, BarrowBlade }

/// <summary>
/// Villagers live beside their houses; the Unbinder (D-034) is the wandering
/// mender cast into every world under a fresh guise, and talks differently.
/// </summary>
public enum NpcKind { Villager, Unbinder }

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
