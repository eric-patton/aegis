using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The families' signature verbs (D-095): the hafted heave sunders the linden
/// board for good and staggers wind-ups; a paid cut answers a wind-up marking
/// the bearer's own ground with a half-step off it; bare knuckles shove the
/// struck body a stride back. The spear's long thrust already stood (D-053).
/// </summary>
public class WeaponVerbTests
{
    /// <summary>A game in the camp with a planted foe beside the bearer, and the line to it.</summary>
    private static (Game Game, Monster Foe, char Dir) Arrange(MonsterKind kind, int hp)
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        var entry = game.World.CampEntryPos;
        game.Debug_SetPlayerPos(entry);
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var beside = entry.Plus(dx, dy);
            var behind = beside.Plus(dx, dy);
            if (!game.World.Camp.Walkable(beside) || !game.World.Camp.Walkable(behind)) continue;
            if (game.Monsters.Any(m => m.Alive && (m.Pos == beside || m.Pos == behind))) continue;
            var foe = new Monster { Kind = kind, Pos = beside, SiteId = "goblin-camp", Hp = hp };
            game.Monsters.Add(foe);
            return (game, foe, DirKey(dx, dy));
        }
        throw new InvalidOperationException("no open lane at the camp's mouth");
    }

    [Fact]
    public void TheSunder_SplitsTheBoard_AndStaggersTheWindUp()
    {
        var (game, carl, dir) = Arrange(MonsterKind.Carl, hp: 60);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        carl.Intent = new Intent { Kind = IntentKind.SeaxStab, TargetCell = game.Player.Pos };

        game.ApplyKey('w');
        game.ApplyKey(dir);
        Assert.NotNull(game.Player.HeaveTarget);
        game.ApplyKey('.'); // the next act says the blow

        Assert.True(carl.BoardBroken);
        Assert.Null(carl.Intent); // staggered clean out of the wind-up
    }

    [Fact]
    public void TheAnsweredStep_CarriesTheFeet_OffTheMarkedGround()
    {
        var (game, goblin, dir) = Arrange(MonsterKind.Goblin, hp: 60);
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        var marked = game.Player.Pos;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = marked };

        game.ApplyKey(dir); // the paid cut
        Assert.NotEqual(marked, game.Player.Pos); // the feet answered
        Assert.Equal(1, game.Player.Pos.Chebyshev(goblin.Pos)); // and kept the reach
    }

    [Fact]
    public void TheShove_CarriesTheBody_AStrideBack()
    {
        var (game, goblin, dir) = Arrange(MonsterKind.Goblin, hp: 60);
        Assert.Null(game.Player.Weapon); // bare knuckles
        // A held wind-up keeps the goblin committed, so the stride it is
        // carried back is not simply walked forward again the same turn.
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos, TurnsUntilResolve = 3 };
        var before = goblin.Pos;

        game.ApplyKey(dir);
        Assert.True(goblin.Alive);
        Assert.Equal(1, before.Chebyshev(goblin.Pos)); // one stride straight back
        Assert.Equal(2, game.Player.Pos.Chebyshev(goblin.Pos)); // out of the bearer's reach
    }

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (0, -1) => 'k',
        (0, 1) => 'j',
        _ => 'n',
    };
}
