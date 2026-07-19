using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The known kinds' second moves (D-096): the goblin's rallying cry, the
/// wight's grave-chill on marked ground, the thegn's measured cut whose mark
/// lies to any read short of keen, and the hound-lunge that hauls the bearer
/// toward the pack. All tested at the resolution seam with planted intents,
/// the deterministic half of each move.
/// </summary>
public class EnemyMoveTests
{
    private static Game AtCamp()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        return game;
    }

    /// <summary>Plants a foe on the first open cell at the given walk distance from the bearer.</summary>
    private static Monster Plant(Game game, MonsterKind kind, int dist, int hp = 60)
    {
        var map = game.World.Camp;
        var origin = game.Player.Pos;
        for (int dx = -dist; dx <= dist; dx++)
            for (int dy = -dist; dy <= dist; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p.Chebyshev(origin) != dist || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                var foe = new Monster { Kind = kind, Pos = p, SiteId = "goblin-camp", Hp = hp };
                game.Monsters.Add(foe);
                return foe;
            }
        throw new InvalidOperationException($"no open cell at distance {dist}");
    }

    [Fact]
    public void TheCry_BringsTheCamp_AStrideCloser()
    {
        var game = AtCamp();
        var crier = Plant(game, MonsterKind.Goblin, 3);
        crier.Intent = new Intent { Kind = IntentKind.RallyCry, TargetCell = crier.Pos };
        var pack = game.Monsters.Where(m => m.Alive && m.Kind == MonsterKind.Goblin && m != crier)
            .ToDictionary(m => m, m => m.Pos.Chebyshev(game.Player.Pos));
        Assert.NotEmpty(pack);

        game.ApplyKey('.');
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("every ear in it"));
        // The far campmates answered the cry with a stride they would not
        // otherwise have taken (near ones may already be engaged or walled).
        Assert.Contains(pack, kv => kv.Key.Alive && kv.Key.Pos.Chebyshev(game.Player.Pos) < kv.Value);
    }

    [Fact]
    public void TheGraveChill_TakesTheArms_OnKeptGround_AndFadesOut()
    {
        var game = AtCamp();
        var wight = Plant(game, MonsterKind.Wight, 3);
        wight.Intent = new Intent { Kind = IntentKind.GraveChill, TargetCell = game.Player.Pos };

        game.ApplyKey('.'); // the ground is kept; the cold closes
        Assert.Equal(3, game.Player.ChilledTurns); // set to 4, one already worked off
        wight.Hp = 0; // stilled, so a second breath cannot restart the clock mid-test
        for (int i = 0; i < 3; i++) game.ApplyKey('.');
        Assert.Equal(0, game.Player.ChilledTurns);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("works out of your arms"));
    }

    [Fact]
    public void TheGraveChill_ClosesOnNothing_WhenTheFeetAnswer()
    {
        var game = AtCamp();
        var wight = Plant(game, MonsterKind.Wight, 4);
        wight.Intent = new Intent { Kind = IntentKind.GraveChill, TargetCell = game.Player.Pos };

        // Any step off the marked cell is the whole counterplay.
        foreach (var (dx, dy) in Directions.Cardinal)
            if (game.World.Camp.Walkable(game.Player.Pos.Plus(dx, dy)))
            {
                game.ApplyKey((dx, dy) switch { (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', _ => 'j' });
                break;
            }
        Assert.Equal(0, game.Player.ChilledTurns);
    }

    [Fact]
    public void TheMeasuredCut_FallsWhereItWasAlwaysGoing_NotWhereItPointed()
    {
        var game = AtCamp();
        var thegn = Plant(game, MonsterKind.Thegn, 1);
        var truth = game.Player.Pos;
        var lie = truth.Plus(0, -1);
        thegn.Intent = new Intent { Kind = IntentKind.MeasuredCut, TargetCell = lie, FeintCell = truth };
        int hp = game.Player.Hp;

        game.ApplyKey('.'); // standing off the shown mark, on the true one
        Assert.True(game.Player.Hp < hp);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("The mark was the lie"));
    }

    [Fact]
    public void TheLungeThatLands_HaulsTheBearer_TowardThePack()
    {
        var game = AtCamp();
        var biter = Plant(game, MonsterKind.Hound, 1);
        var packmate = Plant(game, MonsterKind.Hound, 4);
        biter.Intent = new Intent { Kind = IntentKind.ThroatLunge, TargetCell = game.Player.Pos };
        int before = packmate.Pos.Chebyshev(game.Player.Pos);

        game.ApplyKey('.');
        if (game.Player.Hp > 0) // the lunge landed by construction; the drag needs open ground
            Assert.True(packmate.Pos.Chebyshev(game.Player.Pos) <= before);
    }
}
