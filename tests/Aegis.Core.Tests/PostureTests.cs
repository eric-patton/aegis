using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The second bar and the parry (D-125, paying D-004's posture clause). Every
/// foe carries a guard beside its blood, rocked by pressure that is not blood:
/// a paid blow a point, the wall two, the heave three, a parried blow four. At
/// the brim the guard breaks: the wind-up dies, the body staggers open two
/// turns, and one melee blow through the open door lands 4 deeper. The parry
/// itself is 'a' against a blow shown at the bearer's own ground: the turn
/// spent on the guard instead of the kill, no dice, and the feint's lying mark
/// is a blow that can never be met.
/// </summary>
public class PostureTests
{
    [Fact]
    public void TheGuard_WearsUnderPaidBlows_AndBreaks()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        game.Debug_GrantGear("woodaxe");
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = game.Player.MaxHp;
        game.Player.Stamina = 20;

        var toward = DirKey(goblin.Pos.X - game.Player.Pos.X, goblin.Pos.Y - game.Player.Pos.Y);
        game.ApplyKey(toward);
        game.ApplyKey(toward);
        game.ApplyKey(toward);
        Assert.Equal(3, goblin.PostureDmg);
        Assert.False(goblin.GuardBroken);

        // The fourth paid blow reaches the goblin's brim of 4: the break.
        game.ApplyKey(toward);
        Assert.Equal(0, goblin.PostureDmg);
        Assert.True(goblin.GuardBroken);
        Assert.True(goblin.ExposedTurns > 0);
        Assert.Null(goblin.Intent);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("guard is beaten open"));
    }

    [Fact]
    public void TheBreak_StillsTheWindUp()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        goblin.PostureDmg = goblin.MaxPosture - 1;
        game.Debug_GrantGear("woodaxe");
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = game.Player.MaxHp;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos, TurnsUntilResolve = 2 };

        int hp = game.Player.Hp;
        game.ApplyKey(DirKey(goblin.Pos.X - game.Player.Pos.X, goblin.Pos.Y - game.Player.Pos.Y));

        // The guard broke under the blow, and the blow it was raising died
        // unthrown: no wind-up survives its own body's stagger.
        Assert.True(goblin.GuardBroken);
        Assert.Null(goblin.Intent);
        Assert.Equal(hp, game.Player.Hp);
    }

    [Fact]
    public void TheRiposte_LandsFourDeeper_AndSpendsTheDoor()
    {
        int plain = FirstBlow(broken: false, exposedOnly: false, out _);
        int riposte = FirstBlow(broken: true, exposedOnly: false, out var throughDoor);

        Assert.Equal(plain + GuardBreak.RiposteBonus, riposte);
        Assert.False(throughDoor.GuardBroken); // one blow spends the door
    }

    [Fact]
    public void TheCarlsSpentBoard_IsNotTheOpenDoor()
    {
        // Standing open (D-053) without a broken guard earns the shafts their
        // marks, not the riposte: the door is the break's alone.
        int plain = FirstBlow(broken: false, exposedOnly: false, out _);
        int exposed = FirstBlow(broken: false, exposedOnly: true, out _);
        Assert.Equal(plain, exposed);
    }

    [Fact]
    public void TheParry_TurnsTheShownBlow_AndRocksTheGuard()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = game.Player.MaxHp;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };
        Assert.True(game.ParryOpen);

        int hp = game.Player.Hp;
        int turn = game.Turn;
        game.ApplyKey('a');

        // The blow came as shown and was turned whole: no blood, the striker's
        // guard rocked 4 (a goblin's whole brim: the break), and the guard-work
        // teaches the family in hand, bare knuckles here.
        Assert.Equal(turn + 1, game.Turn);
        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(10 - GuardBreak.ParryCost, game.Player.Stamina);
        Assert.True(goblin.GuardBroken);
        Assert.Equal(0, goblin.PostureDmg);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Brawling));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("turn it aside whole"));
    }

    [Fact]
    public void TheParry_NeedsABlowShownAtYourGround()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        var stand = AdjacentTo(game, goblin.Pos);
        game.Debug_SetPlayerPos(stand);

        // No wind-up at all: nothing for a guard to meet.
        int turn = game.Turn;
        game.ApplyKey('a');
        Assert.Equal(turn, game.Turn);
        Assert.Equal(10, game.Player.Stamina);

        // A mark on a neighbor's ground is a blow that is not coming here.
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = AdjacentTo(game, goblin.Pos, avoid: stand) };
        Assert.False(game.ParryOpen);
        game.ApplyKey('a');
        Assert.Equal(turn, game.Turn);
    }

    [Fact]
    public void TheFeintsLyingMark_CannotBeMet()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        var stand = AdjacentTo(game, goblin.Pos);
        game.Debug_SetPlayerPos(stand);

        // The shown mark sits a neighbor aside and the truth is the bearer's
        // own cell (D-096): the iron goes where the eye says, so below a keen
        // read there is nothing shown here to set a guard against.
        goblin.Intent = new Intent
        {
            Kind = IntentKind.MeasuredCut,
            TargetCell = AdjacentTo(game, goblin.Pos, avoid: stand),
            FeintCell = stand,
        };
        Assert.False(game.ParryOpen);
        int turn = game.Turn;
        game.ApplyKey('a');
        Assert.Equal(turn, game.Turn);
    }

    [Fact]
    public void TheWindedArms_CannotSetTheGuard()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };
        game.Player.Stamina = GuardBreak.ParryCost - 1;

        int turn = game.Turn;
        game.ApplyKey('a');

        Assert.Equal(turn, game.Turn);
        Assert.Equal(GuardBreak.ParryCost - 1, game.Player.Stamina);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("answered by feet"));
    }

    [Fact]
    public void TheWall_HandsTheShoveBack()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        var (goblin, dx, dy) = WallBackedGoblin(game);
        foreach (var m in game.Monsters.Where(m => m.SiteId == "goblin-camp" && m != goblin)) m.Hp = 0;
        goblin.Hp = 99;
        game.Debug_SetPlayerPos(goblin.Pos.Plus(-dx, -dy));
        game.Player.Hp = game.Player.MaxHp;

        // A bare-knuckle blow into a body backed against stone: the blow's
        // point of pressure, plus the wall's two.
        game.ApplyKey(DirKey(dx, dy));

        Assert.Equal(GuardBreak.BlowPressure + GuardBreak.SlamPressure, goblin.PostureDmg);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("held there a breath"));
    }

    [Fact]
    public void TheHeave_RocksByWeight()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        game.Debug_GrantGear("woodaxe");
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = game.Player.MaxHp;

        int dx = goblin.Pos.X - game.Player.Pos.X, dy = goblin.Pos.Y - game.Player.Pos.Y;
        game.ApplyKey('w');
        game.ApplyKey(DirKey(dx, dy)); // the wind-up: cell locked, one turn shown
        game.ApplyKey('.');            // the next act looses it

        Assert.Equal(GuardBreak.HeavePressure, goblin.PostureDmg);
    }

    [Fact]
    public void TheStagger_HoldsFeetAndBlows_AndTheDoorClosesUnspent()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        goblin.GuardBroken = true;
        goblin.ExposedTurns = 2;
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = game.Player.MaxHp;
        var stood = goblin.Pos;

        // Two turns of stagger: the goblin neither steps nor strikes.
        int hp = game.Player.Hp;
        game.ApplyKey('.');
        game.ApplyKey('.');
        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(stood, goblin.Pos);
        Assert.Equal(0, goblin.ExposedTurns);
        Assert.Null(goblin.Intent);

        // The stagger walked off un-riposted: the body next acts whole, and
        // the door is closed.
        game.ApplyKey('.');
        Assert.False(goblin.GuardBroken);
    }

    /// <summary>
    /// Same seed, same goblin, same cell, same dice: only the guard's state
    /// differs, so the blow's difference is exactly the door's worth.
    /// </summary>
    private static int FirstBlow(bool broken, bool exposedOnly, out Monster target)
    {
        var game = new Game(42);
        target = LoneGoblin(game);
        target.Hp = 99;
        game.Debug_GrantGear("woodaxe");
        game.Debug_SetPlayerPos(AdjacentTo(game, target.Pos));
        game.Player.Hp = game.Player.MaxHp;
        if (broken) { target.GuardBroken = true; target.ExposedTurns = 2; }
        if (exposedOnly) target.ExposedTurns = 2;

        int before = target.Hp;
        game.ApplyKey(DirKey(target.Pos.X - game.Player.Pos.X, target.Pos.Y - game.Player.Pos.Y));
        return before - target.Hp;
    }

    /// <summary>A goblin with a flank to stand on and unwalkable ground at its back along that line.</summary>
    private static (Monster goblin, int dx, int dy) WallBackedGoblin(Game game)
    {
        var map = game.World.Camp;
        foreach (var goblin in game.Monsters.Where(m => m.Alive && m.SiteId == "goblin-camp"))
            foreach (var (dx, dy) in Directions.All8)
            {
                var stand = goblin.Pos.Plus(-dx, -dy);
                var back = goblin.Pos.Plus(dx, dy);
                if (map.Walkable(stand) && !game.Monsters.Any(m => m.Alive && m.Pos == stand)
                    && map.InBounds(back) && !map.Walkable(back))
                    return (goblin, dx, dy);
            }
        Assert.Fail("no wall-backed goblin in this camp");
        return default!;
    }

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

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => throw new InvalidOperationException($"no key for ({dx},{dy})"),
    };
}
