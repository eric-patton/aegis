using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The bearer's own second bar (D-126, the other half of D-004's contract:
/// the field reads the bearer back). Landed committed blows rock the bearer's
/// guard against Will's brim: light blows a point, heavy blows two, the
/// charge's mass three. A pressing bearer's thinner guard is leaned on a
/// point harder, a set guard shrugs a point (a light blow whole), and a body
/// holding its own wind-up is rocked a point deeper. At the brim the guard
/// breaks: the held heave dies in the hands, the arms refuse two turns
/// (turn-free), every blow lands 2 deeper (the thegn's 4: it knows the
/// door), and the feet keep working, because retreat is the whole answer.
/// </summary>
public class BearerGuardTests
{
    [Fact]
    public void TheBrim_IsWillsClause()
    {
        var game = new Game(42);
        Assert.Equal(8, game.Player.MaxPosture);
        game.Player.Attributes[Attr.Will] = game.Player.Attributes[Attr.Will] + 2;
        Assert.Equal(10, game.Player.MaxPosture);
    }

    [Fact]
    public void TheShownBlow_RocksTheGuard()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        game.ApplyKey('.');

        Assert.Equal(GuardBreak.BearerLight, game.Player.PostureDmg);
    }

    [Fact]
    public void TheStone_StrikesTheBody_NotTheGuard()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.HurledStone, TargetCell = game.Player.Pos };

        int hp = game.Player.Hp;
        game.ApplyKey('.');

        // Thrown weight against the body, not force through the guard's line.
        Assert.True(game.Player.Hp < hp);
        Assert.Equal(0, game.Player.PostureDmg);
    }

    [Fact]
    public void ThePressingFoot_IsLeanedOn()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        game.Player.Stance = Stance.Pressing;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        game.ApplyKey('.');

        Assert.Equal(GuardBreak.BearerLight + 1, game.Player.PostureDmg);
    }

    [Fact]
    public void TheSetGuard_ShrugsTheLightBlow()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        game.Player.Stance = Stance.Guarded;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };
        game.ApplyKey('.');
        Assert.Equal(0, game.Player.PostureDmg);

        // A heavy blow still gets a point through the set guard.
        goblin.Intent = new Intent { Kind = IntentKind.SunderingCut, TargetCell = game.Player.Pos };
        game.ApplyKey('.');
        Assert.Equal(GuardBreak.BearerHeavy - 1, game.Player.PostureDmg);
    }

    [Fact]
    public void TheCommittedBody_IsRead()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        game.Debug_GrantGear("woodaxe");
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        // The blow resolves on the wind-up's own turn: it finds a body whose
        // guard is already down behind the gathering weight.
        game.ApplyKey('w');
        game.ApplyKey(DirKey(goblin.Pos.X - game.Player.Pos.X, goblin.Pos.Y - game.Player.Pos.Y));

        Assert.Equal(GuardBreak.BearerLight + 1, game.Player.PostureDmg);
        Assert.NotNull(game.Player.HeaveTarget);
    }

    [Fact]
    public void TheBrim_BreaksTheGuard_AndTheHeaveDiesInHand()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        game.Debug_GrantGear("woodaxe");
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        game.Player.PostureDmg = game.Player.MaxPosture - 1;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        game.ApplyKey('w');
        game.ApplyKey(DirKey(goblin.Pos.X - game.Player.Pos.X, goblin.Pos.Y - game.Player.Pos.Y));

        // The break: posture reset, two full turns without arms (the setting
        // turn's own tick already took the first count), the heave dead.
        Assert.Equal(0, game.Player.PostureDmg);
        Assert.Equal(2, game.Player.StaggerTurns);
        Assert.Null(game.Player.HeaveTarget);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("Your guard is beaten open"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("dies in your own hands"));
    }

    [Fact]
    public void TheStaggeredArms_Refuse_TheFeetStill()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        goblin.Hp = 99;
        var stand = AdjacentTo(game, goblin.Pos);
        game.Debug_SetPlayerPos(stand);
        game.Player.Hp = 99;
        game.Player.StaggerTurns = 3;

        // Swing, parry, thrust, and heave all refuse without the turn.
        int turn = game.Turn;
        game.ApplyKey(DirKey(goblin.Pos.X - stand.X, goblin.Pos.Y - stand.Y));
        Assert.Equal(turn, game.Turn);
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = stand, TurnsUntilResolve = 9 };
        game.ApplyKey('a');
        Assert.Equal(turn, game.Turn);
        game.ApplyKey('t');
        Assert.Equal(turn, game.Turn);
        game.ApplyKey('w');
        Assert.Equal(turn, game.Turn);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("your feet still will"));

        // The feet keep working: retreat is the staggered bearer's answer.
        var away = AdjacentTo(game, stand, avoid: goblin.Pos);
        game.ApplyKey(DirKey(away.X - stand.X, away.Y - stand.Y));
        Assert.Equal(turn + 1, game.Turn);
        Assert.Equal(away, game.Player.Pos);
    }

    [Fact]
    public void TheOpenGuard_TakesBlowsDeeper()
    {
        int plain = LandedBlow(MonsterKind.Goblin, stagger: 0);
        int open = LandedBlow(MonsterKind.Goblin, stagger: 5);
        Assert.Equal(plain + GuardBreak.OpenGuardDeeper, open);
    }

    [Fact]
    public void TheThegn_PutsThePointThroughTheDoor()
    {
        // The reader alone knows the door a beaten-open guard leaves: the
        // riposte's mirror, 2 through the open guard and 2 more for the eye.
        int plain = LandedBlow(MonsterKind.Thegn, stagger: 0);
        int open = LandedBlow(MonsterKind.Thegn, stagger: 5);
        Assert.Equal(plain + GuardBreak.OpenGuardDeeper + 2, open);
    }

    [Fact]
    public void TheCharge_RocksByMass()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.BoarCharge, TargetCell = game.Player.Pos };

        game.ApplyKey('.');

        Assert.Equal(GuardBreak.BearerCharge, game.Player.PostureDmg);
    }

    [Fact]
    public void TheQuietGround_SettlesTheGuard()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Player.PostureDmg = 3;
        goblin.Hp = 0;

        game.ApplyKey('.');

        Assert.Equal(0, game.Player.PostureDmg);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("settles whole"));
    }

    [Fact]
    public void TheFall_HandsTheBearerBackStanding()
    {
        var game = new Game(42);
        var goblin = LoneGoblin(game);
        game.Debug_SetPlayerPos(AdjacentTo(game, goblin.Pos));
        game.Player.Hp = 1;
        game.Player.PostureDmg = 5;
        game.Player.StaggerTurns = 2;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        game.ApplyKey('.');

        Assert.Equal(1, game.Player.Deaths);
        Assert.Equal(0, game.Player.PostureDmg);
        Assert.Equal(0, game.Player.StaggerTurns);
    }

    /// <summary>
    /// Same seed, same cell, same dice: only the bearer's stagger differs, so
    /// the blow's difference is exactly what the open guard hands through.
    /// The striker is fabricated so the goblin and the thegn stand on the
    /// same ground and draw the same rolls.
    /// </summary>
    private static int LandedBlow(MonsterKind kind, int stagger)
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        foreach (var m in game.Monsters.Where(m => m.SiteId == "goblin-camp")) m.Hp = 0;
        var striker = new Monster
        {
            Kind = kind,
            Pos = AdjacentTo(game, game.Player.Pos),
            SiteId = "goblin-camp",
            Hp = 99,
        };
        game.Monsters.Add(striker);
        striker.Intent = new Intent { Kind = IntentKind.MeasuredCut, TargetCell = game.Player.Pos };
        game.Player.Hp = 99;
        game.Player.StaggerTurns = stagger;

        game.ApplyKey('.');
        return 99 - game.Player.Hp;
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
            if (map.Walkable(p) && p != avoid && p != game.Player.Pos
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
