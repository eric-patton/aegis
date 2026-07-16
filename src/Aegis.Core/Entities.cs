namespace Aegis.Core;

public sealed class Player
{
    public Pos Pos { get; set; }
    public int MaxHp { get; set; } = 20;
    public int Hp { get; set; } = 20;
    public int MaxStamina { get; set; } = 10;
    public int Stamina { get; set; } = 10;
    public int Coin { get; set; }
    public int Essence { get; set; }
    public int WoundedTurns { get; set; }
    public int Deaths { get; set; }

    /// <summary>Effective max HP while Wounded: the Aegis is spent (D-008).</summary>
    public int EffectiveMaxHp => WoundedTurns > 0 ? Math.Max(1, MaxHp * 4 / 5) : MaxHp;
}

public enum MonsterKind { Goblin }

public sealed class Monster
{
    public required MonsterKind Kind { get; init; }
    public required Pos Pos { get; set; }
    public int Hp { get; set; } = 8;
    public Intent? Intent { get; set; }
    public bool Alive => Hp > 0;
    public string Name => Kind switch { MonsterKind.Goblin => "goblin", _ => "creature" };
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

public enum IntentKind { CrushingBlow }

/// <summary>What death leaves behind: unspent coin and Essence, one reclaim attempt (D-008).</summary>
public sealed class Remnant
{
    public required string MapId { get; init; }
    public required Pos Pos { get; init; }
    public required int Coin { get; init; }
    public required int Essence { get; init; }
}
