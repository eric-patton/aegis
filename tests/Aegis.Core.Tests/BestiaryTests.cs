using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The bestiary (D-059, paying D-004's oldest clause: telegraph clarity scales
/// with what the bearer knows). A kind's wind-up starts as a Blur (danger, but
/// not its shape or name), sharpens to a named Read once its tell has been
/// witnessed, and to a Keen read once it is known cold. Wits above the baseline
/// is a head start, so a keen-eyed bearer reads a stranger on sight. The read is
/// bearer-knowledge: it survives death and crosses waygates whole, and it is
/// rebuilt by replay, never serialized.
/// </summary>
public class BestiaryTests
{
    [Fact]
    public void AStranger_ReadsAsABlur_ThenNames_ThenReadsItsWeight()
    {
        var p = new Player();
        // Never faced: a blur. Danger without a shape.
        Assert.Equal(ReadTier.Blur, p.ReadOf(MonsterKind.Boar));

        // One wind-up witnessed and the tell has a name.
        p.WitnessTell(MonsterKind.Boar);
        Assert.Equal(ReadTier.Read, p.ReadOf(MonsterKind.Boar));

        // Three, and its weight reads too. And a tell read cold is read cold:
        // the count caps, never runs away.
        p.WitnessTell(MonsterKind.Boar);
        p.WitnessTell(MonsterKind.Boar);
        Assert.Equal(ReadTier.Keen, p.ReadOf(MonsterKind.Boar));
        p.WitnessTell(MonsterKind.Boar);
        p.WitnessTell(MonsterKind.Boar);
        Assert.Equal(Player.ReadKeen, p.Reads[MonsterKind.Boar]);
        Assert.Equal(ReadTier.Keen, p.ReadOf(MonsterKind.Boar));

        // The read is per kind: knowing the boar teaches nothing of the wight.
        Assert.Equal(ReadTier.Blur, p.ReadOf(MonsterKind.Wight));
    }

    [Fact]
    public void KeenEyes_ReadAStrangerOnSight()
    {
        // At the humble baseline, a stranger is a blur until it is watched.
        var plain = new Player();
        Assert.Equal(AttributeSet.Baseline, plain.Attributes[Attr.Wits]);
        Assert.Equal(ReadTier.Blur, plain.ReadOf(MonsterKind.Hound));

        // A point of Wits is a head start: the tell reads on sight, never faced.
        var sharp = new Player();
        sharp.Attributes[Attr.Wits] = AttributeSet.Baseline + 1;
        Assert.Empty(sharp.Reads);
        Assert.Equal(ReadTier.Read, sharp.ReadOf(MonsterKind.Hound));

        // A keen eye reads a stranger's weight on sight, and Wits stacks with
        // what has been banked besides.
        var keen = new Player();
        keen.Attributes[Attr.Wits] = AttributeSet.Baseline + 3;
        Assert.Equal(ReadTier.Keen, keen.ReadOf(MonsterKind.Hound));
    }

    [Fact]
    public void AWitnessedTell_BanksTheRead_AndShowsInTheSnapshot()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        // Wound up at the bearer's own cell, so the blow lands and is survived.
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = game.Player.MaxHp;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        Assert.Equal(ReadTier.Blur, game.Player.ReadOf(MonsterKind.Goblin));
        game.ApplyKey('.');

        // The tell was watched to its end: the goblin now reads.
        Assert.Equal(1, game.Player.Reads.GetValueOrDefault(MonsterKind.Goblin));
        Assert.Equal(ReadTier.Read, game.Player.ReadOf(MonsterKind.Goblin));
        Assert.Contains("goblin:1", game.TakeSnapshot().Reads);
    }

    [Fact]
    public void ATellDodged_TeachesJustTheSame()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        var stand = AdjacentTo(game, goblin.Pos);
        game.Debug_SetPlayerPos(stand);
        // Aimed at the ground the bearer just left: a blow spent on nothing.
        var empty = AdjacentTo(game, goblin.Pos, avoid: stand);
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = empty };

        int hp = game.Player.Hp;
        game.ApplyKey('.');

        // No blood drawn, and the tell learned all the same: reading is watching.
        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(1, game.Player.Reads.GetValueOrDefault(MonsterKind.Goblin));
    }

    [Fact]
    public void TheBestiary_CrossesWithTheBearer()
    {
        var game = new Game(42);
        game.Player.WitnessTell(MonsterKind.Goblin);
        game.Player.WitnessTell(MonsterKind.Goblin);
        game.Player.WitnessTell(MonsterKind.Wight);

        // Cycle 1 is the master-seed world; one crossing steps into the next.
        Cross(game);
        Assert.Equal(2, game.Cycle);

        // The harder world is new ground, but the reads are the bearer's own.
        Assert.Equal(2, game.Player.Reads.GetValueOrDefault(MonsterKind.Goblin));
        Assert.Equal(1, game.Player.Reads.GetValueOrDefault(MonsterKind.Wight));
        Assert.Equal(ReadTier.Read, game.Player.ReadOf(MonsterKind.Wight));
    }

    [Fact]
    public void TheBestiary_SurvivesDeath_AndTheKillingTellBanks()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        // A killing blow wound up on the bearer's cell: it lands, and it fells.
        game.Player.Hp = 1;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        game.ApplyKey('.');

        // The bearer fell, and the tell that felled them is read all the same:
        // knowledge is the one thing death never takes.
        Assert.Equal(1, game.Player.Deaths);
        Assert.Equal(1, game.Player.Reads.GetValueOrDefault(MonsterKind.Goblin));
        Assert.Equal(ReadTier.Read, game.Player.ReadOf(MonsterKind.Goblin));
    }

    // ---- helpers ----

    /// <summary>Drop into the camp with a single live goblin; all its fellows quiet.</summary>
    private static Monster LoneGoblin(Game game)
    {
        game.Debug_SetMode(MapMode.Site);
        var goblin = game.Monsters.First(m => m.Alive && m.SiteId == "goblin-camp");
        foreach (var m in game.Monsters.Where(m => m.SiteId == "goblin-camp" && m != goblin)) m.Hp = 0;
        return goblin;
    }

    private static Pos AdjacentTo(Game game, Pos target, Pos? avoid = null)
    {
        var map = game.World.Camp;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (map.Walkable(p) && p != avoid
                && !game.Monsters.Any(m => m.Alive && m.Pos == p)) return p;
        }
        Assert.Fail($"no open cell beside {target}");
        return default;
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }
}
