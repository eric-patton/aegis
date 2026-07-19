using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The stead's suspicion (D-086): the home faction's Infamy axis and the first
/// transgression verb. Grab beside an overworld house pilfers it, once per door
/// per world; shame climbs one rung per door (watched, unwelcome, named a thief),
/// each rung costing in its own currency (the hearthtale closes, the friend's
/// price and purse close, the larder bars), and coin left on the robbed sill is
/// the designed way back down. Shame runs beside regard, never against it.
/// </summary>
public class ShameTests
{
    [Fact]
    public void TheLadder_CountsOneRungPerDoor()
    {
        Assert.Equal(0, SteadShame.RungFor(0));
        Assert.Equal(1, SteadShame.RungFor(1));
        Assert.Equal(2, SteadShame.RungFor(2));
        Assert.Equal(3, SteadShame.RungFor(3));
        Assert.Equal("", SteadShame.TitleOf(0));
        Assert.Equal("watched in this stead", SteadShame.TitleOf(1));
        Assert.Equal("unwelcome here", SteadShame.TitleOf(2));
        Assert.Equal("named a thief here", SteadShame.TitleOf(3));
    }

    [Fact]
    public void TheFirstTheft_IsSeen_Named_AndWritten()
    {
        var game = AtTheDoors(42);
        int rations = game.Player.Rations;

        game.Apply(Command.Grab);

        Assert.Equal(rations + 1, game.Player.Rations);
        Assert.Equal(1, game.Shame);
        Assert.True(game.World.Facts.Exists("shame", "watched"));
        var log = game.Log.Recent(8).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("The latch lifts"));
        Assert.Contains(log, t => t.Contains("marks you from the lane"));      // three houses, no secrets
        Assert.Contains(log, t => t.Contains("watched in this stead"));        // the rung is named
        Assert.Contains(log, t => t.Contains("can be made right at the door")); // the way back is named
        Assert.Contains(log, t => t.Contains("what is taken is carried too")); // the Aegis's one line
    }

    [Fact]
    public void TheAegisAside_IsHeardOnce()
    {
        var game = AtTheDoors(42);
        game.Apply(Command.Grab);
        game.Apply(Command.Grab);

        Assert.Single(game.Log.Entries, e => e.Text.Contains("what is taken is carried too"));
    }

    [Fact]
    public void ThreeDoors_NameAThief_AndEachGivesOnce()
    {
        var game = new Game(42);
        game.Player.Coin = 0; // no restitution possible: the last press must not repay
        int rations = game.Player.Rations;
        RobDoors(game, 3);

        Assert.Equal(3, game.Shame);
        Assert.Equal(rations + 3, game.Player.Rations);
        Assert.True(game.World.Facts.Exists("shame", "unwelcome"));
        Assert.True(game.World.Facts.Exists("shame", "thief"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("named a thief here"));

        // Every door is spent; without coin the repay branch refuses and nothing moves.
        AtTheDoors(game);
        game.Apply(Command.Grab);
        Assert.Equal(3, game.Shame);
        Assert.Equal(rations + 3, game.Player.Rations);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("The wrong keeps until you can pay"));
    }

    [Fact]
    public void TheRobbedDoor_AsksToBeMadeRight_BeforeAnyOtherOpens()
    {
        // At a corner shared between a robbed door and an innocent one, the same
        // key repays before it robs: a mistaken press must never commit a worse
        // deed than the one intended.
        var game = AtTheDoors(42); // the center: all three doors at arm's reach
        game.Player.Coin = SteadShame.RepayCoin;
        game.Apply(Command.Grab);
        Assert.Equal(1, game.Shame);
        Assert.Single(game.World.PilferedHouses);

        game.Apply(Command.Grab);
        Assert.Equal(0, game.Shame);                 // the sill is paid
        Assert.Single(game.World.PilferedHouses);    // and no second door was opened
        Assert.Equal(0, game.Player.Coin);
    }

    [Fact]
    public void TheFullPack_TurnsTheHandAway()
    {
        var game = AtTheDoors(42);
        game.Player.Rations = Game.RationCap;

        game.Apply(Command.Grab);

        Assert.Equal(0, game.Shame);
        Assert.Empty(game.World.PilferedHouses);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("even thieving has its arithmetic"));
    }

    [Fact]
    public void Restitution_WalksTheLadderBackDown_DoorByDoor()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        RobDoors(game, 3);
        Assert.Equal(3, game.Shame);

        // From the center every robbed door is at arm's reach; each press pays one sill.
        AtTheDoors(game);
        game.Player.Coin = SteadShame.RepayCoin * 3;
        game.Apply(Command.Grab);
        Assert.Equal(2, game.Shame);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("unwelcome here") && e.Text.Contains("no worse"));

        game.Apply(Command.Grab);
        game.Apply(Command.Grab);
        Assert.Equal(0, game.Shame);
        Assert.Equal(0, game.Player.Coin);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("book on you is even again"));

        // Every door is now closed both ways: robbed once, repaid once, done.
        game.Apply(Command.Grab);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("nothing here to take"));
        Assert.Equal(0, game.Shame);

        // The record keeps what the ledger forgave: the stead remembers having watched.
        Assert.True(game.World.Facts.Exists("shame", "watched"));
    }

    [Fact]
    public void Suspicion_ClosesTheFriendsPrice_WithoutTouchingTheRegard()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // regard 3: a friend, the friend's price open
        int friendly = game.RationPrice;

        game.Player.Coin = 0;
        RobDoors(game, 2); // shame 2: unwelcome

        // The folk do not extend a friend's terms to one held unwelcome; the
        // regard itself stands untouched beside the shame (dual axes, D-023).
        Assert.Equal(friendly + 1, game.RationPrice);
        Assert.Equal(3, game.Regard);
        Assert.Equal(2, game.Shame);
    }

    [Fact]
    public void ThePurse_IsWithheldFromTheUnwelcome()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        RobDoors(game, 2); // unwelcome before the deed lands
        int coin = game.Player.Coin;

        game.Debug_ClearCamp(); // crosses the friend rung: the purse moment

        Assert.Equal(coin, game.Player.Coin); // no purse pooled
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("count their doors, and keep their coin"));
    }

    [Fact]
    public void TheLarder_IsBarredToANamedThief()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        RobDoors(game, 3); // named a thief
        game.Player.Coin = 20;
        int rations = game.Player.Rations;

        var holder = game.World.Npcs.First(n => n.Id == "npc_steadholder");
        NpcTests.BumpNpc(game, holder);
        Assert.Contains(game.Offers, o => o.Label.Contains("the larder is barred to you"));

        game.ApplyKey(OfferKey(game, TradeGood.Ration));
        Assert.Equal(rations, game.Player.Rations);
        Assert.Equal(20, game.Player.Coin);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("Not to you"));
    }

    [Fact]
    public void TheHearthtale_IsNotToldToWatchedHands()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // the friend rung: the telling would be open
        AtTheDoors(game);
        game.Apply(Command.Grab); // watched: the fence closes

        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);

        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("inside its own fence"));
        Assert.False(game.World.Facts.Exists("rumor", "stead_hearthtale"));
    }

    [Fact]
    public void TheClosedDoors_AreSeenOnTheLane()
    {
        var game = AtTheDoors(42);
        game.Apply(Command.Grab);

        StepStillNearAHouse(game);

        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("A door ahead of you closes"));
    }

    [Fact]
    public void TheCrossing_LeavesTheShameBehind()
    {
        var game = AtTheDoors(42);
        game.Apply(Command.Grab);
        Assert.Equal(1, game.TakeSnapshot().Shame);
        Assert.Equal("watched in this stead", game.TakeSnapshot().ShameTitle);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.Equal(0, game.Shame);
        Assert.Equal("", game.TakeSnapshot().ShameTitle);
        Assert.Empty(game.World.PilferedHouses); // a fresh world's doors stand innocent
        Assert.False(game.World.Facts.Exists("shame", "watched"));
    }

    /// <summary>Stands the player at the settlement center: adjacent to all three of the stead's doors.</summary>
    private static Game AtTheDoors(ulong seed) => AtTheDoors(new Game(seed));

    private static Game AtTheDoors(Game game)
    {
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2));
        return game;
    }

    /// <summary>
    /// Robs n distinct doors. A robbed door beside you asks to be made right
    /// before any other is opened (repay outranks theft at a shared corner), so
    /// the thief must find, for each door, an angle clear of their earlier work.
    /// </summary>
    internal static void RobDoors(Game game, int n)
    {
        var map = game.World.Overworld;
        for (int robbed = 0; robbed < n; robbed++)
        {
            var spots = AllPositions(map).Where(p =>
                map.Walkable(p) && !game.World.Npcs.Any(x => x.Pos == p)
                && Neighbors(map, p).Any(q => map[q] == Terrain.House && !game.World.PilferedHouses.Contains(q))
                && !Neighbors(map, p).Any(q => map[q] == Terrain.House
                    && game.World.PilferedHouses.Contains(q) && !game.World.RepaidHouses.Contains(q)))
                .Take(1).ToList();
            Assert.True(spots.Count > 0, "no clean angle on an unrobbed door");
            game.Debug_SetPlayerPos(spots[0]);
            int before = game.Shame;
            game.Apply(Command.Grab);
            Assert.Equal(before + 1, game.Shame);
        }
    }

    private static IEnumerable<Pos> AllPositions(GameMap map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                yield return new Pos(x, y);
    }

    private static IEnumerable<Pos> Neighbors(GameMap map, Pos p) =>
        Directions.All8.Select(d => p.Plus(d.dx, d.dy)).Where(map.InBounds);

    /// <summary>One step that still ends beside a house, so the NearHouse hook fires.</summary>
    private static void StepStillNearAHouse(Game game)
    {
        var map = game.World.Overworld;
        foreach (var (dx, dy) in Directions.All8)
        {
            var target = game.Player.Pos.Plus(dx, dy);
            if (!map.Walkable(target) || game.World.Npcs.Any(n => n.Pos == target)) continue;
            bool nearHouse = Directions.All8.Any(d =>
                map.InBounds(target.Plus(d.dx, d.dy)) && map[target.Plus(d.dx, d.dy)] == Terrain.House);
            if (!nearHouse) continue;
            game.Apply((dx, dy) switch
            {
                (0, -1) => Command.MoveN,
                (0, 1) => Command.MoveS,
                (-1, 0) => Command.MoveW,
                (1, 0) => Command.MoveE,
                (-1, -1) => Command.MoveNW,
                (1, -1) => Command.MoveNE,
                (-1, 1) => Command.MoveSW,
                _ => Command.MoveSE,
            });
            return;
        }
        Assert.Fail("no walkable step beside a house");
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));
}
