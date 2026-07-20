using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The locked coffer (D-122): the crime family's third verb, and the first
/// with no wronged party breathing. One box of old iron per fighting deep
/// whose makers were the locking kind (the barrow left out: the dead lock
/// nothing, they watch), opened on Sleight dice at 'g', one sitting per lock
/// per world. A lock that gives pays coin and feeds the hand; a lock that
/// holds teaches nothing and keeps its lid. No shame, no facts, no witness.
/// Seeds are pinned: on seed 1 the camp's lock gives, on seed 4 it holds
/// (deterministic, probed once, stable).
/// </summary>
public class CofferTests
{
    [Fact]
    public void TheOdds_RideTheSkill_HarderThanAPocket()
    {
        Assert.Equal(0.35, Locks.ChanceFor(0));
        Assert.Equal(0.65, Locks.ChanceFor(5), 3);
        Assert.Equal(0.85, Locks.ChanceFor(20)); // old iron keeps its last word
    }

    [Fact]
    public void TheCoffer_StandsInTheLockingDeeps_AndNowhereElse()
    {
        var game = new Game(1);
        var camp = game.World.CampSite;
        Assert.NotNull(camp.CofferPos);
        Assert.True(camp.Map.Walkable(camp.CofferPos!.Value));
        Assert.NotEqual(camp.ChestPos, camp.CofferPos.Value);
        Assert.NotEqual(camp.StonePos, camp.CofferPos);

        // The dead lock nothing, and the quiet sites keep no strongboxes.
        var world = WorldGen.Generate(SeedTree.Derive(1, "cycle", 2), tier: 2);
        Assert.NotNull(world.CampSite.CofferPos);
        Assert.Null(world.BarrowSite!.CofferPos);
        foreach (var site in world.Sites.Where(s => s.Kind is SiteKind.Songhall or SiteKind.Wilds or SiteKind.Hollow))
            Assert.Null(site.CofferPos);
    }

    [Fact]
    public void TheLockGives_PaysCoin_AndTeachesTheHand()
    {
        var (game, camp) = EnteredCamp(1);
        int coin = game.Player.Coin;

        game.Debug_SetPlayerPos(camp.CofferPos!.Value);
        game.ApplyKey('g');

        Assert.True(camp.CofferOpened);
        Assert.True(camp.CofferTried);
        Assert.Equal(coin + 13, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Sleight));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("sounds like agreement"));
    }

    [Fact]
    public void TheLockHolds_AndTeachesNothing()
    {
        var (game, camp) = EnteredCamp(4);
        int coin = game.Player.Coin;

        game.Debug_SetPlayerPos(camp.CofferPos!.Value);
        game.ApplyKey('g');

        Assert.False(camp.CofferOpened);
        Assert.True(camp.CofferTried);
        Assert.Equal(coin, game.Player.Coin);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Sleight)); // only work that worked counts
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("this one has won"));
    }

    [Fact]
    public void OneSitting_PerLock_PerWorld()
    {
        var (game, camp) = EnteredCamp(4);
        game.Debug_SetPlayerPos(camp.CofferPos!.Value);
        game.ApplyKey('g');
        int coin = game.Player.Coin;

        game.ApplyKey('g');

        Assert.False(camp.CofferOpened);
        Assert.Equal(coin, game.Player.Coin); // no second dice drawn
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("second sitting"));
    }

    [Fact]
    public void AnEmptiedBox_StopsMattering()
    {
        var (game, camp) = EnteredCamp(1);
        game.Debug_SetPlayerPos(camp.CofferPos!.Value);
        game.ApplyKey('g');
        int coin = game.Player.Coin;

        game.ApplyKey('g');

        Assert.Equal(coin, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("nothing here to take"));
    }

    [Fact]
    public void TheLock_KeepsNoLedger_EitherWay()
    {
        foreach (ulong seed in new ulong[] { 1, 4 }) // the give and the hold
        {
            var (game, camp) = EnteredCamp(seed);
            game.Debug_SetPlayerPos(camp.CofferPos!.Value);
            game.ApplyKey('g');

            Assert.Equal(0, game.Shame);
            Assert.False(game.World.Facts.Exists("shame", "confronted"));
            Assert.False(game.World.Facts.Exists("secret", "lifted_purse"));
        }
    }

    [Fact]
    public void TheTile_NamesTheBox_AndThenTheRefusal()
    {
        var (game, camp) = EnteredCamp(4);
        StepOnto(game, camp.CofferPos!.Value);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Press g to try"));

        game.ApplyKey('g');
        StepOff(game, camp.CofferPos!.Value);
        StepOnto(game, camp.CofferPos!.Value);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("does not give a second sitting"));
    }

    [Fact]
    public void TheCrossing_RegeneratesTheLock_Innocent()
    {
        var (game, camp) = EnteredCamp(4);
        game.Debug_SetPlayerPos(camp.CofferPos!.Value);
        game.ApplyKey('g');
        Assert.True(camp.CofferTried);

        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        var next = game.World.CampSite;
        Assert.NotNull(next.CofferPos);
        Assert.False(next.CofferTried); // a new world's iron has met no one's fingers
        Assert.False(next.CofferOpened);
    }

    /// <summary>Into the camp with its tenants stilled, so the lock is the only argument left.</summary>
    private static (Game, Site) EnteredCamp(ulong seed)
    {
        var game = new Game(seed);
        var camp = game.World.CampSite;
        foreach (var m in game.Monsters.Where(m => m.SiteId == "goblin-camp"))
            m.Hp = 0;
        game.Debug_SetPlayerPos(camp.OverworldPos);
        game.Apply(Command.Enter);
        return (game, camp);
    }

    /// <summary>Walk (not teleport) onto a cell, so the tile speaks (EnterTile fires on feet, not on Debug hooks).</summary>
    private static void StepOnto(Game game, Pos target)
    {
        foreach (var (dx, dy, key) in new[] { (-1, 0, 'l'), (1, 0, 'h'), (0, -1, 'j'), (0, 1, 'k') })
        {
            var beside = target.Plus(dx, dy);
            if (!game.CurrentSite!.Map.Walkable(beside)) continue;
            game.Debug_SetPlayerPos(beside);
            game.ApplyKey(key);
            Assert.Equal(target, game.Player.Pos);
            return;
        }
        Assert.Fail("no walkable cell beside the coffer");
    }

    /// <summary>One walkable step off a cell, any direction.</summary>
    private static void StepOff(Game game, Pos from)
    {
        foreach (var (dx, key) in new[] { (-1, 'h'), (1, 'l') })
        {
            var beside = from.Plus(dx, 0);
            if (!game.CurrentSite!.Map.Walkable(beside)) continue;
            game.ApplyKey(key);
            return;
        }
        foreach (var (dy, key) in new[] { (-1, 'k'), (1, 'j') })
        {
            var beside = from.Plus(0, dy);
            if (!game.CurrentSite!.Map.Walkable(beside)) continue;
            game.ApplyKey(key);
            return;
        }
        Assert.Fail("nowhere to step off the coffer");
    }
}
