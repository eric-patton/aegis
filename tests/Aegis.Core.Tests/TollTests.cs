using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The Death's Toll and its scars (D-098, paying D-009): the deterministic
/// ledger (fill on death, drain by turn, convert above the line, no roll),
/// the scar matched to the death's shape with a fixed fallback order, each
/// mark's mechanical weight, and the waygate wiping the count while the
/// scars cross with the body.
/// </summary>
public class TollTests
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
                return p;
            }
        throw new InvalidOperationException($"no open cell at distance {dist}");
    }

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, -1) => 'y', (0, -1) => 'k', (1, -1) => 'u',
        (-1, 0) => 'h', (1, 0) => 'l',
        (-1, 1) => 'b', (0, 1) => 'j', _ => 'n',
    };

    /// <summary>A shapeless death: no hand on it, so any scar falls to the fixed order.</summary>
    private static void FallShapeless(Game game)
    {
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
    }

    /// <summary>
    /// Dies to a specific kind's specific wind-up, so the scar matching has a
    /// death shape to read: the bearer stands at one hit point on the marked
    /// ground and lets the declared blow fall.
    /// </summary>
    private static void FallTo(Game game, MonsterKind kind, IntentKind windup)
    {
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        var brute = new Monster { Kind = kind, Pos = OpenAt(game, game.Player.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        brute.Intent = new Intent { Kind = windup, TargetCell = game.Player.Pos };
        game.Monsters.Add(brute);
        game.Debug_HurtPlayer(game.Player.Hp - 1);
        game.ApplyKey('.');
        Assert.Equal(game.World.ShrinePos, game.Player.Pos); // the blow fell, and the Aegis caught what it could
        brute.Hp = 0; // the scaffold leaves with the test
    }

    [Fact]
    public void TheFirstFall_FillsTheCount_AndWarns_ButNeverScars()
    {
        var game = new Game(42);
        FallShapeless(game);

        Assert.Equal(DeathsToll.Fill, game.Player.Toll); // baseline Will: the full weight
        Assert.Empty(game.Player.Scars);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains($"The toll stands at {DeathsToll.Fill}"));
    }

    [Fact]
    public void TheToll_DrainsByTheTurn_AndSpeaks_CrossingUnderTheLine()
    {
        var game = new Game(42);
        FallShapeless(game);

        for (int i = 0; i < DeathsToll.Fill - DeathsToll.Line + 1; i++)
            game.ApplyKey('.');

        Assert.Equal(DeathsToll.Line - 1, game.Player.Toll);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("settles below the line"));
    }

    [Fact]
    public void TheSecondFall_AboveTheLine_TakesTheHand_TheBlowAskedFor()
    {
        var game = new Game(42);
        FallShapeless(game); // the warning
        FallTo(game, MonsterKind.Goblin, IntentKind.CrushingBlow); // the collection

        Assert.Equal([ScarId.CrushedHand], game.Player.Scars);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("what the counting costs"));
    }

    [Fact]
    public void TheShapelessSecondFall_FallsToTheFixedOrder()
    {
        var game = new Game(42);
        FallShapeless(game);
        FallShapeless(game);

        Assert.Equal([ScarId.TakenEye], game.Player.Scars);
    }

    [Fact]
    public void TheUncannyHand_LeavesTheHauntedLook_WhateverItSwung()
    {
        var game = new Game(42);
        FallShapeless(game);
        FallTo(game, MonsterKind.Wight, IntentKind.BarrowBlade);

        Assert.Equal([ScarId.HauntedLook], game.Player.Scars);
    }

    [Fact]
    public void TheTakenEye_StepsTheRead_DownOneWholeTier()
    {
        var game = new Game(42);
        var p = game.Player;
        p.WitnessTell(MonsterKind.Goblin);
        p.WitnessTell(MonsterKind.Goblin);
        p.WitnessTell(MonsterKind.Goblin);
        Assert.Equal(ReadTier.Keen, p.ReadOf(MonsterKind.Goblin));
        Assert.Equal(ReadTier.Blur, p.ReadOf(MonsterKind.Hart));

        p.Scars.Add(ScarId.TakenEye);
        Assert.Equal(ReadTier.Read, p.ReadOf(MonsterKind.Goblin)); // keen no more
        Assert.Equal(ReadTier.Blur, p.ReadOf(MonsterKind.Hart));   // a blur has no lower to go
    }

    [Fact]
    public void TheCrushedHand_AsksABreathMoreWind_EverySwing()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        var post = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, game.Player.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        game.Monsters.Add(post);
        char at = DirKey(Math.Sign(post.Pos.X - game.Player.Pos.X), Math.Sign(post.Pos.Y - game.Player.Pos.Y));

        int before = game.Player.Stamina;
        game.ApplyKey(at);
        Assert.Equal(3, before - game.Player.Stamina);

        game.Player.Scars.Add(ScarId.CrushedHand);
        before = game.Player.Stamina;
        game.ApplyKey(at);
        Assert.Equal(4, before - game.Player.Stamina);
    }

    [Fact]
    public void TheHauntedLook_CoolsTheRegard_AndDearsTheBread()
    {
        var game = new Game(42);
        int fairPrice = game.RationPrice;

        game.Player.Scars.Add(ScarId.HauntedLook);
        Assert.Equal(fairPrice + 1, game.RationPrice);

        game.Debug_ClearCamp(); // the deed that raises three raises two, colder
        Assert.Equal(2, game.Regard);
    }

    [Fact]
    public void TheCrossing_WipesTheCount_AndTheScarsCross_WithTheBody()
    {
        var game = new Game(42);
        FallShapeless(game);
        FallShapeless(game);
        Assert.True(game.Player.Toll > DeathsToll.Line);
        Assert.Equal([ScarId.TakenEye], game.Player.Scars);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(2, game.Cycle);
        Assert.Equal(0, game.Player.Toll);
        Assert.Equal([ScarId.TakenEye], game.Player.Scars);
    }
}
