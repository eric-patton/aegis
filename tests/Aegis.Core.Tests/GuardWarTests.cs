using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The guard war's coda (D-129): both kinds the second bars were waiting for.
/// The shield-carl's board-check is the field's first pressure verb aimed at
/// the bearer's guard and not the blood: telegraphed mass along the guard's
/// line at the charge's tier, no dice and no wound in it, dodged by feet or
/// met by the parry like any shown blow, and gone with a sundered board. The
/// drilled sword-thegn is the first kind that answers being parried: taught
/// the bind beside the blow, it rolls off the met guard keeping half its
/// force and shoves a point back through the crossed iron.
/// </summary>
public class GuardWarTests
{
    [Fact]
    public void TheBoardCheck_ShovesTheGuardNotTheBlood()
    {
        var game = new Game(42);
        var carl = Fabricate(game, MonsterKind.Carl);
        carl.Intent = new Intent { Kind = IntentKind.BoardCheck, TargetCell = game.Player.Pos };

        int hp = game.Player.Hp;
        game.ApplyKey('.');

        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(GuardBreak.CheckPressure, game.Player.PostureDmg);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("shoves your guard wide of its line"));
    }

    [Fact]
    public void TheBoardCheck_ReadsTheStance()
    {
        // The check rides the same reading as every committed blow (D-126):
        // a pressing bearer is leaned on harder, a set guard blunts a point.
        var game = new Game(42);
        var carl = Fabricate(game, MonsterKind.Carl);
        game.Player.Stance = Stance.Pressing;
        carl.Intent = new Intent { Kind = IntentKind.BoardCheck, TargetCell = game.Player.Pos };
        game.ApplyKey('.');
        Assert.Equal(GuardBreak.CheckPressure + 1, game.Player.PostureDmg);

        game.Player.PostureDmg = 0;
        game.Player.Stance = Stance.Guarded;
        carl.Intent = new Intent { Kind = IntentKind.BoardCheck, TargetCell = game.Player.Pos };
        game.ApplyKey('.');
        Assert.Equal(GuardBreak.CheckPressure - 1, game.Player.PostureDmg);
    }

    [Fact]
    public void TheBoardCheck_IsDodgedByFeet()
    {
        var game = new Game(42);
        var carl = Fabricate(game, MonsterKind.Carl);
        var stand = game.Player.Pos;
        carl.Intent = new Intent { Kind = IntentKind.BoardCheck, TargetCell = stand };

        var away = AdjacentTo(game, stand, avoid: carl.Pos);
        int hp = game.Player.Hp;
        game.ApplyKey(DirKey(away.X - stand.X, away.Y - stand.Y));

        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(0, game.Player.PostureDmg);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("the place you left"));
    }

    [Fact]
    public void TheBoardCheck_CanBeMet()
    {
        // The parry is the check's own counter: the carl's whole thrown mass
        // turned back on its guard, and nothing reaches the bearer's bar.
        var game = new Game(42);
        var carl = Fabricate(game, MonsterKind.Carl);
        carl.Intent = new Intent { Kind = IntentKind.BoardCheck, TargetCell = game.Player.Pos };
        Assert.True(game.ParryOpen);

        int hp = game.Player.Hp;
        game.ApplyKey('a');

        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(0, game.Player.PostureDmg);
        Assert.Equal(GuardBreak.ParryPressure, carl.PostureDmg);
    }

    [Fact]
    public void TheWholeBoard_Checks()
    {
        var game = new Game(42);
        Fabricate(game, MonsterKind.Carl);
        game.Player.Hp = 999;

        for (int i = 0; i < 40 && !Declared(game); i++) game.ApplyKey('.');

        Assert.True(Declared(game));
    }

    [Fact]
    public void TheSunderedBoard_HasNoCheck()
    {
        var game = new Game(42);
        var carl = Fabricate(game, MonsterKind.Carl);
        carl.BoardBroken = true;
        game.Player.Hp = 999;

        for (int i = 0; i < 40; i++) game.ApplyKey('.');

        Assert.False(Declared(game));
    }

    [Fact]
    public void TheDrilledThegn_AnswersTheMetParry()
    {
        var game = new Game(42);
        var thegn = Fabricate(game, MonsterKind.Thegn);
        thegn.Intent = new Intent { Kind = IntentKind.MeasuredCut, TargetCell = game.Player.Pos };
        Assert.True(game.ParryOpen);

        int hp = game.Player.Hp;
        game.ApplyKey('a');

        // The blow is still turned (no blood) and the guard-work still
        // teaches, but the drilled hand gives only half its force to the
        // bind, and the crossed iron shoves a point back.
        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(GuardBreak.DrilledParryPressure, thegn.PostureDmg);
        Assert.Equal(GuardBreak.BindPressure, game.Player.PostureDmg);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Brawling));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("drilled for the bind"));
    }

    [Fact]
    public void TheBind_IsShruggedByTheSetGuard()
    {
        var game = new Game(42);
        var thegn = Fabricate(game, MonsterKind.Thegn);
        game.Player.Stance = Stance.Guarded;
        thegn.Intent = new Intent { Kind = IntentKind.MeasuredCut, TargetCell = game.Player.Pos };

        game.ApplyKey('a');

        Assert.Equal(GuardBreak.DrilledParryPressure, thegn.PostureDmg);
        Assert.Equal(0, game.Player.PostureDmg);
    }

    [Fact]
    public void TheUndrilledHand_GivesItsWholeSwing()
    {
        var game = new Game(42);
        var carl = Fabricate(game, MonsterKind.Carl);
        carl.Intent = new Intent { Kind = IntentKind.SeaxStab, TargetCell = game.Player.Pos };

        game.ApplyKey('a');

        Assert.Equal(GuardBreak.ParryPressure, carl.PostureDmg);
        Assert.Equal(0, game.Player.PostureDmg);
    }

    private static bool Declared(Game game) =>
        game.Log.Entries.Any(e => e.Text.Contains("squares the whole board"));

    /// <summary>
    /// A striker stood beside the bearer on the camp's own ground, everything
    /// else dead: the BearerGuardTests fabrication, so kind-specific answers
    /// can be read off a known cell with known dice.
    /// </summary>
    private static Monster Fabricate(Game game, MonsterKind kind)
    {
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
        game.Player.Hp = 99;
        return striker;
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
