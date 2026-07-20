using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The calling and its shade (D-099, the summon slot of D-024): the fifth
/// word on the stones, held rather than spent (part of the pool stays bound
/// while the shade walks), a full body on the guest engine in its own slot,
/// modest of blow but doubled on the uncanny, refusing the severed as the
/// bearer's own choice, and unraveling without weight: fall, release, the
/// bearer's death, and the waygate all end it, and nothing grieves.
/// </summary>
public class ShadeTests
{
    private static Pos OpenAt(Game game, Pos origin, int dist)
    {
        var map = game.CurrentMap;
        for (int dx = -dist; dx <= dist; dx++)
            for (int dy = -dist; dy <= dist; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p.Chebyshev(origin) != dist || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (p == game.Player.Pos) continue;
                if (game.Shade is { } s && s.Pos == p) continue;
                if (game.Guest is { } g && g.Pos == p) continue;
                return p;
            }
        throw new InvalidOperationException($"no open cell at distance {dist}");
    }

    /// <summary>Says the calling honestly: 'z', then the word's own digit.</summary>
    private static void SayCalling(Game game)
    {
        game.ApplyKey('z');
        game.ApplyKey((char)('1' + game.Player.Spells.IndexOf(SpellId.Calling)));
    }

    private static Game StandTheShade(ulong seed = 42)
    {
        var game = new Game(seed);
        game.Debug_LearnSpell(SpellId.Calling);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        SayCalling(game);
        Assert.NotNull(game.Shade);
        return game;
    }

    [Fact]
    public void TheStonesLeanings_AllCarryTheFifthWord_TheBarrowSecond()
    {
        foreach (var kind in Enum.GetValues<SiteKind>())
            Assert.Contains(SpellId.Calling, SpellCatalog.StonePreference(kind));
        // The most soul-touched fabric offers it earliest of all.
        Assert.Equal(SpellId.Calling, SpellCatalog.StonePreference(SiteKind.Barrow)[1]);
    }

    [Fact]
    public void TheFullHead_FindsTheStoneStillGiving()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Spark);
        game.Debug_LearnSpell(SpellId.Levin);
        game.Debug_LearnSpell(SpellId.Ward);
        game.Debug_LearnSpell(SpellId.Veilsight);

        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampSite.StonePos!.Value);
        game.ApplyKey('g');

        // Before D-099 this bearer found only company; now the last word waits.
        Assert.True(game.Player.HasSpell(SpellId.Calling));
    }

    [Fact]
    public void TheCalling_StandsTheShade_AndBindsTheHold_SpendingNothing()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Calling);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        int pool = game.Player.Focus;

        SayCalling(game);

        Assert.NotNull(game.Shade);
        Assert.Equal(1, game.Shade!.Pos.Chebyshev(game.Player.Pos));
        Assert.Equal(pool, game.Player.Focus); // held, never spent
        Assert.Equal(pool - Game.CallingHold, game.SpendableFocus);
        Assert.True(game.Player.CallingLineHeard);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("a shade, standing"));
    }

    [Fact]
    public void TheHold_RefusesTheOtherWords_UntilTheShadeIsReleased()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Calling);
        game.Debug_LearnSpell(SpellId.Ward);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.Player.Focus = 3;
        SayCalling(game);

        // One spendable point is not the ward's two.
        game.ApplyKey('z');
        game.ApplyKey((char)('1' + game.Player.Spells.IndexOf(SpellId.Ward)));
        Assert.Equal(0, game.Player.WardTurns);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("bound to the calling"));

        // Saying the calling again is the release, and frees the hold.
        SayCalling(game);
        Assert.Null(game.Shade);
        game.Player.Focus = 3;
        game.ApplyKey('z');
        game.ApplyKey((char)('1' + game.Player.Spells.IndexOf(SpellId.Ward)));
        Assert.Equal(Game.WardHeldTurns - 1, game.Player.WardTurns); // the saying's own turn already ticked
    }

    [Fact]
    public void TheRelease_IsSaidAnywhere_EvenUnderTheOpenSky()
    {
        var game = StandTheShade();
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.ShrinePos);

        SayCalling(game);

        Assert.Null(game.Shade);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("let the calling finish itself"));
    }

    [Fact]
    public void TheShadesHand_IsModest_OnMortalFoes()
    {
        var game = StandTheShade();
        var goblin = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, game.Shade!.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        game.Monsters.Add(goblin);

        game.ApplyKey('.');

        int blow = 60 - goblin.Hp;
        Assert.InRange(blow, 1, 3);
    }

    [Fact]
    public void TheShadesHand_FallsDouble_OnTheUncanny()
    {
        var game = StandTheShade();
        var wight = new Monster { Kind = MonsterKind.Wight, Pos = OpenAt(game, game.Shade!.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        game.Monsters.Add(wight);

        game.ApplyKey('.');

        int blow = 60 - wight.Hp;
        Assert.InRange(blow, 2, 6);
        Assert.Equal(0, blow % 2); // soul-stuff answers soul-stuff, twice over
    }

    [Fact]
    public void TheShade_RefusesTheSevered_TheChoiceStaysTheBearers()
    {
        var game = StandTheShade();
        var keeper = new Monster { Kind = MonsterKind.Severed, Pos = OpenAt(game, game.Shade!.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        game.Monsters.Add(keeper);

        game.ApplyKey('.');

        Assert.Equal(60, keeper.Hp);
    }

    [Fact]
    public void TheBrokenShade_Unravels_AndNothingGrieves()
    {
        var game = StandTheShade();
        var shade = game.Shade!;
        shade.Hp = 1;
        var brute = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, shade.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        brute.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = shade.Pos };
        game.Monsters.Add(brute);
        int shame = game.Shame;

        game.ApplyKey('.');

        Assert.Null(game.Shade);
        Assert.Equal(shame, game.Shame); // no life was spent
        Assert.Empty(game.World.Facts.OfType("guest-fell"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("frays to a smoke"));
    }

    [Fact]
    public void TheBearersFall_LetsTheHeldWordSlip_AndKeepsTheKnowing()
    {
        var game = StandTheShade();

        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        Assert.Null(game.Shade);
        Assert.True(game.Player.HasSpell(SpellId.Calling)); // the word survives every fall
        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains("gone before you are"));
    }

    [Fact]
    public void TheCrossing_LeavesTheCalledThing_AndCarriesTheWord()
    {
        var game = StandTheShade();
        game.Debug_ClearCamp();
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.GatePos);

        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(2, game.Cycle);
        Assert.Null(game.Shade);
        Assert.True(game.Player.HasSpell(SpellId.Calling));
    }

    [Fact]
    public void TheShadeAndTheGuest_WalkTheSameRoad_EachInTheirOwnSlot()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Calling);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.Debug_SetGuest(new Guest
        {
            Id = "guest_test",
            Name = "Aldith",
            Role = GuestRole.Huntsman,
            Pos = OpenAt(game, game.Player.Pos, 1),
            MaxHp = 16,
            Hp = 16,
        });

        SayCalling(game);
        game.ApplyKey('.');

        Assert.NotNull(game.Guest);
        Assert.NotNull(game.Shade);
        Assert.NotEqual(game.Guest!.Pos, game.Shade!.Pos); // two bodies, two cells
    }

    [Fact]
    public void TheGroundWord_FallsToTheShade_WhenNoOneElseWalks()
    {
        var game = StandTheShade();

        game.ApplyKey('o');
        Assert.True(game.Shade!.Holding);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("The shade stills where it stands"));

        game.ApplyKey('o');
        Assert.False(game.Shade!.Holding);
    }
}
