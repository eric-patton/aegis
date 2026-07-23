using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Burglary proper (D-127): crime's last named verb, the whole distance in.
/// 's' beside a stead door slips the latch and crosses the sill: a clean
/// entry pays the kist's coin and an heirloom for the road-cart, feeds
/// Larceny, and writes the stead's third secret fact from a deed; a caught
/// entry jumps the unified ladder two rungs at once, with restitution at the
/// crossed sill (twice a door's coin) as the designed exit. One try per door
/// per world, and the sill and the kist are two independent ledgers. Seeds
/// are pinned: on seed 1 the first entry comes out unwoken, on seed 4 the
/// floorboard answers (deterministic, probed once, stable).
/// </summary>
public class BurglaryTests
{
    [Fact]
    public void TheOdds_SitBetweenPocketAndCoffer()
    {
        Assert.Equal(0.4, Burglary.ChanceFor(0));
        Assert.Equal(0.6, Burglary.ChanceFor(4), 3);
        Assert.Equal(0.85, Burglary.ChanceFor(20)); // no hand is ever safe
        Assert.True(Burglary.ChanceFor(0) < Lifting.ChanceFor(0));
        Assert.True(Burglary.ChanceFor(0) > Locks.ChanceFor(0));
    }

    [Fact]
    public void TheCleanEntry_TakesTheKist_AndNobodyWakes()
    {
        var game = new Game(1);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        int coin = game.Player.Coin;
        int turn = game.Turn;

        game.Apply(Command.Burgle);

        Assert.Equal(coin + 4, game.Player.Coin);
        Assert.Equal(1, game.Player.Trinket); // the heirloom only the road-cart buys
        Assert.Equal(turn + 1, game.Turn); // the crossing in costs the turn it takes
        Assert.Single(game.World.BurgledHouses);
        Assert.Empty(game.World.CaughtBurglaries);
        Assert.Equal(0, game.Shame); // done right, the stead is none the wiser
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Sleight));
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Larceny));
        Assert.True(game.World.Facts.Exists("secret", "burgled_house"));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("never know by whom"));
    }

    [Fact]
    public void TheCaughtEntry_JumpsTwoRungsAtOnce()
    {
        var game = new Game(4);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        int coin = game.Player.Coin;

        game.Apply(Command.Burgle);

        Assert.Equal(coin, game.Player.Coin); // nothing taken, everything seen
        Assert.Equal(0, game.Player.Trinket);
        Assert.Single(game.World.CaughtBurglaries);
        Assert.Equal(2, game.Shame); // a body in the dark of a house is not a loaf
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Larceny)); // only work that worked counts
        Assert.True(game.World.Facts.Exists("shame", "housebroken"));
        Assert.True(game.World.Facts.Exists("shame", "unwelcome")); // both rungs crossed in one night
        var log = game.Log.Recent(6).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("a floorboard answers"));
        Assert.Contains(log, t => t.Contains("crossed sill is made right"));
    }

    [Fact]
    public void OneDoor_KeepsItsNights()
    {
        var game = new Game(1);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        game.Apply(Command.Burgle);
        int coin = game.Player.Coin;
        int turn = game.Turn;

        game.Apply(Command.Burgle);

        Assert.Equal(coin, game.Player.Coin);
        Assert.Equal(turn, game.Turn); // refused without the turn
        Assert.Single(game.World.BurgledHouses);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("listens harder after"));
    }

    [Fact]
    public void TheRepay_WalksBothRungsDown_AtTheDoorItCrossed()
    {
        var game = new Game(4);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        game.Apply(Command.Burgle);
        Assert.Equal(2, game.Shame);
        game.Player.Coin = 20;

        game.Apply(Command.Burgle); // the same key that crossed it

        Assert.Equal(20 - SteadShame.BreakInRepayCoin, game.Player.Coin);
        Assert.Equal(0, game.Shame);
        Assert.Single(game.World.RepaidBurglaries);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("does not close on you"));

        // A repaid sill is closed both ways: the door keeps its nights.
        game.Apply(Command.Burgle);
        Assert.Equal(20 - SteadShame.BreakInRepayCoin, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("listens harder after"));
    }

    [Fact]
    public void ShortCoin_LeavesTheWrongStanding()
    {
        var game = new Game(4);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        game.Apply(Command.Burgle);
        game.Player.Coin = SteadShame.BreakInRepayCoin - 1;

        game.Apply(Command.Burgle);

        Assert.Equal(SteadShame.BreakInRepayCoin - 1, game.Player.Coin);
        Assert.Equal(2, game.Shame);
        Assert.Empty(game.World.RepaidBurglaries);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("The wrong keeps"));
    }

    [Fact]
    public void TheSill_AndTheKist_AreTwoLedgers()
    {
        var game = new Game(1);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        game.Apply(Command.Grab); // the sill-reach first (D-086)
        Assert.Equal(1, game.Shame);
        Assert.Equal(1, game.Player.Trinket);

        game.Apply(Command.Burgle); // the same door still has a kist behind it

        Assert.Single(game.World.BurgledHouses);
        Assert.Equal(2, game.Player.Trinket);
        Assert.Equal(1, game.Shame); // the clean entry adds nothing to the sill's count
    }

    [Fact]
    public void TheDeepPlaces_UseTheSameKeyForSoftTread()
    {
        var game = new Game(1);
        game.Debug_SetMode(MapMode.Site);
        int turn = game.Turn;

        game.Apply(Command.Burgle);

        Assert.Equal(turn, game.Turn);
        Assert.Empty(game.World.BurgledHouses);
        Assert.True(game.SoftTread);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("settle into soft tread"));
    }

    [Fact]
    public void TheVerb_NeedsADoor_AndCostsNoTurnWithout()
    {
        var game = new Game(1);
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, 3));
        int turn = game.Turn;

        game.Apply(Command.Burgle);

        Assert.Equal(turn, game.Turn);
        Assert.Empty(game.World.BurgledHouses);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("No door stands near enough"));
    }

    [Fact]
    public void TheCrossing_RegeneratesTheDoors_AndDropsTheShame()
    {
        var game = new Game(4);
        game.Debug_SetPlayerPos(SpotBesideOneDoor(game));
        game.Apply(Command.Burgle);
        Assert.Equal(2, game.Shame);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.Empty(game.World.BurgledHouses);
        Assert.Empty(game.World.CaughtBurglaries);
        Assert.Equal(0, game.Shame); // the next stead's dark has heard no step
    }

    /// <summary>
    /// A stand beside exactly one door, so a second press argues with the
    /// same house instead of finding its neighbor's latch.
    /// </summary>
    private static Pos SpotBesideOneDoor(Game game)
    {
        var map = game.World.Overworld;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.World.Npcs.Any(n => n.Pos == p)) continue;
                if (Directions.All8.Count(d =>
                        map.InBounds(p.Plus(d.dx, d.dy)) && map[p.Plus(d.dx, d.dy)] == Terrain.House) == 1)
                    return p;
            }
        Assert.Fail("no cell beside exactly one door");
        return default;
    }
}
