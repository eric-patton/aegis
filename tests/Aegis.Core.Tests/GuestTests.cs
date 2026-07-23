using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The one who walks with you (D-097, stage 1): the guest engine. Following,
/// holding, the contextual order key, the tending, fighting to their own
/// measure, body-blocking marked ground, coming through doors, and dying for
/// real. Guests are cast through the debug hook here; the storylet doors that
/// cast them in play are stage 2's work.
/// </summary>
public class GuestTests
{
    private static Guest MakeGuest(GuestRole role, Pos pos, int hp = 14) =>
        new() { Id = "guest_test", Name = "Oswin", Role = role, Pos = pos, MaxHp = hp, Hp = hp };

    /// <summary>The first open cell at the exact walk distance from an origin.</summary>
    private static Pos OpenAt(Game game, Pos origin, int dist)
    {
        var map = game.CurrentMap;
        for (int dx = -dist; dx <= dist; dx++)
            for (int dy = -dist; dy <= dist; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p.Chebyshev(origin) != dist || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (game.World.Npcs.Any(n => n.Pos == p)) continue;
                if (p == game.Player.Pos || p == game.Guest?.Pos) continue;
                return p;
            }
        throw new InvalidOperationException($"no open cell at distance {dist}");
    }

    private static Game AtCamp()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        return game;
    }

    [Fact]
    public void TheGuest_WalksBackToYourShoulder()
    {
        var game = new Game(42);
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 3));
        game.Debug_SetGuest(guest);

        int before = guest.Pos.Chebyshev(game.Player.Pos);
        game.ApplyKey('.');
        Assert.True(guest.Pos.Chebyshev(game.Player.Pos) < before);
        game.ApplyKey('.');
        game.ApplyKey('.');
        Assert.True(guest.Pos.Chebyshev(game.Player.Pos) <= 1);
    }

    [Fact]
    public void TheOrder_HoldsTheGround_AndIsFreeOffTheFight()
    {
        var game = new Game(42);
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 1));
        game.Debug_SetGuest(guest);

        int turn = game.Turn;
        game.ApplyKey('o');
        Assert.True(guest.Holding);
        Assert.Equal(turn, game.Turn); // free on quiet ground, like the footing

        var held = guest.Pos;
        for (int i = 0; i < 4; i++) game.ApplyKey('.');
        Assert.Equal(held, guest.Pos); // the ground is held

        game.ApplyKey('o');
        Assert.False(guest.Holding);
    }

    [Fact]
    public void TheOrder_WithNoOneBeside_IsRefused()
    {
        var game = new Game(42);
        int turn = game.Turn;
        game.ApplyKey('o');
        Assert.Equal(turn, game.Turn);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("No one walks with you"));
    }

    [Fact]
    public void TheTending_SpendsTheSatchel_OnTheirBlood()
    {
        var game = new Game(42);
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 1));
        guest.Hp = 5;
        game.Debug_SetGuest(guest);
        game.Player.Herb = 2;

        int turn = game.Turn;
        game.ApplyKey('o');
        Assert.Equal(1, game.Player.Herb);   // a sprig spent, not a word said
        Assert.Equal(9, guest.Hp);           // mended 4
        Assert.Equal(turn + 1, game.Turn);   // handwork always costs the turn
    }

    [Fact]
    public void TheFighter_HitsLikeOne_AndTheCrofterDoesNot()
    {
        // Same seed, same planted foe, different hands: the huntsman's blow
        // draws from (2,6), the crofter's from (1,3). Competence is who they
        // are, not a slider (D-097).
        foreach (var (role, lo, hi) in new[] { (GuestRole.Huntsman, 2, 5), (GuestRole.Crofter, 1, 2) })
        {
            var game = AtCamp();
            var guest = MakeGuest(role, OpenAt(game, game.Player.Pos, 1));
            game.Debug_SetGuest(guest);
            var foe = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, guest.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
            foe.Intent = new Intent
            {
                Kind = IntentKind.CrushingBlow,
                TargetCell = game.Player.Pos,
                TurnsUntilResolve = 3,
            };
            game.Monsters.Add(foe);

            game.ApplyKey('.');
            int dealt = 60 - foe.Hp;
            Assert.InRange(dealt, lo, hi);
        }
    }

    [Fact]
    public void TheBody_TakesTheBlow_OnTheMarkedGround()
    {
        var game = AtCamp();
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 2));
        game.Debug_SetGuest(guest);
        var brute = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, guest.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        brute.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = guest.Pos };
        game.Monsters.Add(brute);

        game.ApplyKey('.');
        Assert.True(guest.Hp < guest.MaxHp);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("marked ground"));
    }

    [Fact]
    public void TheGuest_CanDie_AndTheAegisSaysItsPiece()
    {
        var game = AtCamp();
        var guest = MakeGuest(GuestRole.Crofter, OpenAt(game, game.Player.Pos, 2), hp: 2);
        game.Debug_SetGuest(guest);
        var brute = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, guest.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        brute.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = guest.Pos };
        game.Monsters.Add(brute);

        game.ApplyKey('.');
        Assert.False(guest.Alive);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("does not move again"));
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("never made to hold but one"));
    }

    [Fact]
    public void TheRaider_TurnsOnTheNearerBody()
    {
        var game = AtCamp();
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 4));
        game.Debug_SetGuest(guest);
        var raider = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, guest.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        game.Monsters.Add(raider);

        // The raider stands beside the guest and far from the bearer: its
        // iron goes where the blood is nearest. The answer may now be the
        // fellow's clean step off marked ground rather than blood paid.
        game.ApplyKey('.');
        Assert.True(guest.Hp < guest.MaxHp || !guest.Alive
            || game.PhysicalTargetsOnFellows > 0 && game.FellowEvasions > 0);
    }

    [Fact]
    public void TheDoorway_IsASharedStep()
    {
        var game = new Game(42);
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 1));
        game.Debug_SetGuest(guest);

        var target = guest.Pos;
        var own = game.Player.Pos;
        int dx = Math.Sign(target.X - own.X), dy = Math.Sign(target.Y - own.Y);
        char key = (dx, dy) switch
        {
            (-1, -1) => 'y', (0, -1) => 'k', (1, -1) => 'u',
            (-1, 0) => 'h', (1, 0) => 'l',
            (-1, 1) => 'b', (0, 1) => 'j', _ => 'n',
        };
        game.ApplyKey(key);
        Assert.Equal(target, game.Player.Pos);
        Assert.Equal(own, guest.Pos); // traded places, no shoving match
    }

    [Fact]
    public void TheGuest_ComesThroughTheDoor_AndOutAgain()
    {
        var game = new Game(42);
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 1));
        game.Debug_SetGuest(guest);

        game.Debug_SetPlayerPos(game.World.CampSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
        Assert.True(guest.Pos.Chebyshev(game.Player.Pos) <= 1);

        game.ApplyKey('<');
        Assert.Equal(MapMode.Overworld, game.Mode);
        Assert.True(guest.Pos.Chebyshev(game.Player.Pos) <= 1);
    }

    [Fact]
    public void TheGuest_IsAtTheShrine_WhenYouWake()
    {
        var game = new Game(42);
        var guest = MakeGuest(GuestRole.Huntsman, OpenAt(game, game.Player.Pos, 1));
        game.Debug_SetGuest(guest);

        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.True(guest.Alive);
        Assert.True(guest.Pos.Chebyshev(game.World.ShrinePos) <= 1);
    }
}
