using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The east road (D-138, plan 2026-07 B1): the world's second overworld and
/// travel-as-play's first machinery. The tests hold the four promises the
/// plan named: supplies (the wayhouse's counter, the verges' herbs, the camp
/// cooking the kill), weather exposure (the sky ruling the step and the
/// night), camp rests (the D-006 box opened), and an encounter site seeded
/// along the way (the half-way glade), plus the seams: the mouth crossing
/// both ways, the beast's choice at it, and the two maps never bleeding into
/// each other's ledgers.
/// </summary>
public class RoadTests
{
    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    /// <summary>Walks the real mouth: stand on it, press the door key.</summary>
    private static void TakeRoad(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.True(game.OnRoad);
    }

    [Fact]
    public void TheWorldGrowsARoad_Deterministically()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            Assert.Equal(Terrain.RoadMouth, a.Overworld[a.RoadMouthPos]);
            Assert.Equal(Terrain.RoadMouth, a.Road[a.RoadHomePos]);
            Assert.Equal(a.Road.ContentHash(), b.Road.ContentHash());
            Assert.Equal(a.Overworld.ContentHash(), b.Overworld.ContentHash());

            var keeper = a.Waykeeper;
            Assert.True(keeper.OnRoad);
            Assert.True(a.Road.Walkable(keeper.Pos), $"seed {seed}: waykeeper on unwalkable ground");

            var trail = a.RoadWildsSite;
            Assert.True(trail.OnRoad);
            Assert.Equal(SiteKind.Wilds, trail.Kind);
            Assert.NotEmpty(trail.Spawns);
            Assert.All(trail.Spawns, s => Assert.Equal(MonsterKind.Hart, s.Kind));
            Assert.Equal(Terrain.WildsEntrance, a.Road[trail.OverworldPos]);
            // The valley's own wilds accessor never reads the road's trail (D-138).
            Assert.Null(a.WildsSite);

            Assert.All(a.RoadHerbs, h => Assert.Equal(Terrain.Forest, a.Road[h]));
            Assert.True(a.Facts.Exists("site", "road"));
            Assert.True(a.Facts.Exists("person", "npc_waykeeper"));
        }
    }

    [Fact]
    public void TheMouth_CrossesBothWays_AndSiteLooksUpItsOwnMap()
    {
        var game = new Game(42);
        TakeRoad(game);
        Assert.Equal(game.World.RoadHomePos, game.Player.Pos);
        Assert.Equal("road", game.CurrentMap.Id);

        // The glade is entered from the road with the same door key as everything.
        var trail = game.World.RoadWildsSite;
        game.Debug_SetPlayerPos(trail.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
        Assert.Equal("road-wilds", game.CurrentSite!.Id);
        game.Debug_SetPlayerPos(trail.EntryPos);
        game.ApplyKey('<');
        Assert.True(game.OnRoad); // climbing out of the road's trail comes back up on the road

        game.Debug_SetPlayerPos(game.World.RoadHomePos);
        game.ApplyKey('>');
        Assert.False(game.OnRoad);
        Assert.Equal(game.World.RoadMouthPos, game.Player.Pos);
        Assert.Equal("overworld", game.CurrentMap.Id);
    }

    [Fact]
    public void TheBeast_TakesTheMouthAtTheSide_AndGrazesWhenLeft()
    {
        // The mule bought at the wood's edge (D-100's own surface).
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Player.Coin = MountCatalog.MuleCoin;
        var woodward = game.World.Npcs.First(n => n.Id == "npc_woodward");
        NpcTests.BumpNpc(game, woodward);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey((char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == TradeGood.Beast)));
        game.ApplyKey(' ');
        game.ApplyKey(' ');
        Assert.NotNull(game.Mount);

        // At the side, the beast crosses the mouth with the bearer.
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.Mount!.Pos = game.World.RoadMouthPos.Plus(0, 1);
        game.ApplyKey('>');
        Assert.True(game.OnRoad);
        Assert.True(game.Mount!.OnRoad);
        Assert.True(game.Mount!.Pos.Chebyshev(game.Player.Pos) <= 1);

        // Left grazing on the road, it keeps its own map while the bearer goes home.
        game.Mount!.Pos = game.Player.Pos.Plus(8, 0);
        game.Debug_SetPlayerPos(game.World.RoadHomePos);
        game.ApplyKey('>');
        Assert.False(game.OnRoad);
        Assert.True(game.Mount!.OnRoad);
    }

    [Fact]
    public void TheCamp_CooksTheKill_MendsTheBody_AndFeedsTheCrafts()
    {
        var game = new Game(42);
        var spot = OpenGround(game.World.Overworld, game);
        game.Debug_SetPlayerPos(spot);
        game.Debug_HurtPlayer(10);
        game.Player.Rations = 1;
        game.Player.RawMeat = 2;
        int cookingBefore = game.Player.Skills.Uses(SkillId.Cooking);
        int survivalBefore = game.Player.Skills.Uses(SkillId.Survival);
        int turnBefore = game.Turn;
        int maxHp = game.Player.EffectiveMaxHp;

        game.ApplyKey('m');

        Assert.Equal(0, game.Player.RawMeat);                    // the kill went over the fire
        Assert.Equal(2, game.Player.Rations);                    // 1 + 2 cooked - 1 supper
        Assert.Equal(maxHp - 4, game.Player.Hp);                 // the base 6 mended of the 10
        Assert.Equal(game.Player.MaxStamina, game.Player.Stamina);
        Assert.Equal(cookingBefore + 1, game.Player.Skills.Uses(SkillId.Cooking));
        Assert.Equal(survivalBefore + 1, game.Player.Skills.Uses(SkillId.Survival));
        Assert.Equal(turnBefore + RoadLife.CampTurns, game.Turn); // the night is real time
    }

    [Fact]
    public void TheColdCamp_RestoresOnlyTheLegs_AndCountsNoCraft()
    {
        var game = new Game(42);
        game.Debug_SetPlayerPos(OpenGround(game.World.Overworld, game));
        game.Debug_HurtPlayer(5);
        game.Player.Rations = 0;
        game.Player.RawMeat = 0;
        game.Player.Stamina = 1;
        int hp = game.Player.Hp;
        int survivalBefore = game.Player.Skills.Uses(SkillId.Survival);
        int turnBefore = game.Turn;

        game.ApplyKey('m');

        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(game.Player.MaxStamina, game.Player.Stamina);
        Assert.Equal(survivalBefore, game.Player.Skills.Uses(SkillId.Survival));
        Assert.Equal(turnBefore + RoadLife.CampTurns, game.Turn);
    }

    [Fact]
    public void TheSky_RulesTheStepAndTheNight()
    {
        var game = new Game(42);
        TakeRoad(game);

        // Rain takes the step's small recovery.
        game.Debug_SetSky(RoadSky.Rain);
        game.Player.Stamina = 1;
        StepOntoPlain(game);
        Assert.Equal(1, game.Player.Stamina);

        // A clear sky gives it back.
        game.Debug_SetSky(RoadSky.Clear);
        StepOntoPlain(game);
        Assert.Equal(2, game.Player.Stamina);

        // A fed camp under weather mends half.
        game.Debug_SetSky(RoadSky.Rain);
        game.Debug_HurtPlayer(10);
        game.Player.Rations = 1;
        int maxHp = game.Player.EffectiveMaxHp;
        MoveToPlain(game);
        game.ApplyKey('m');
        Assert.Equal(maxHp - 7, game.Player.Hp); // 6/2 = 3 of the 10 mended

        // And the cold wind refuses a supperless camp outright: no night passes.
        game.Debug_SetSky(RoadSky.Cold);
        game.Player.Rations = 0;
        game.Player.RawMeat = 0;
        int turnBefore = game.Turn;
        game.ApplyKey('m');
        Assert.Equal(turnBefore, game.Turn);
    }

    [Fact]
    public void TheWayhouse_SellsRoadBread_AndTheDeepestSleepOnTheRoad()
    {
        var game = new Game(42);
        TakeRoad(game);
        var keeper = game.World.Waykeeper;
        var beside = Directions.All8
            .Select(d => keeper.Pos.Plus(d.dx, d.dy))
            .First(p => game.World.Road.Walkable(p) && !game.World.Npcs.Any(n => n.OnRoad && n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(keeper.Pos.X - beside.X, keeper.Pos.Y - beside.Y));

        Assert.True(game.InTalkMenu);
        Assert.Equal(keeper.Name, game.TalkNpc!.Name);
        Assert.True(game.World.Facts.Exists("met", "npc_waykeeper"));

        game.Player.Coin = Peddling.RationPrice + RoadLife.BedCoin;
        game.Player.Rations = 0;
        game.Debug_HurtPlayer(8);
        int maxHp = game.Player.EffectiveMaxHp;

        game.ApplyKey(OfferKey(game, TradeGood.Ration));
        Assert.Equal(1, game.Player.Rations);
        Assert.Equal(RoadLife.BedCoin, game.Player.Coin);

        game.ApplyKey(OfferKey(game, TradeGood.Bed));
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(maxHp, game.Player.Hp);
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus);

        // Short coin buys the bench by the fire, which is a bench.
        game.Debug_HurtPlayer(3);
        game.ApplyKey(OfferKey(game, TradeGood.Bed));
        Assert.Equal(maxHp - 3, game.Player.Hp);
    }

    [Fact]
    public void TheVerges_ArePickedOnTheStep_FromTheRoadsOwnList()
    {
        var game = new Game(42);
        TakeRoad(game);
        var (spot, beside) = game.World.RoadHerbs
            .SelectMany(h => Directions.All8.Select(d => (h, p: h.Plus(d.dx, d.dy))))
            .First(c => game.World.Road.Walkable(c.p) && !game.World.Npcs.Any(n => n.OnRoad && n.Pos == c.p));
        int herbsBefore = game.Player.Herb;
        int spotsBefore = game.World.RoadHerbs.Count;
        int valleySpots = game.World.Herbs.Count;

        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(spot.X - beside.X, spot.Y - beside.Y));

        Assert.True(game.Player.Herb > herbsBefore);
        Assert.Equal(spotsBefore - 1, game.World.RoadHerbs.Count);
        Assert.Equal(valleySpots, game.World.Herbs.Count); // the valley's wood is untouched
    }

    [Fact]
    public void TheRoad_ReplaysLikeEverythingElse()
    {
        static Game Run()
        {
            var game = new Game(1234);
            game.Player.Rations = 2;
            game.Debug_SetPlayerPos(game.World.RoadMouthPos);
            game.ApplyKey('>');
            foreach (char key in "llll")
                game.ApplyKey(key);
            game.ApplyKey('m');
            foreach (char key in "hhhh")
                game.ApplyKey(key);
            return game;
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a.Player.Pos, b.Player.Pos);
        Assert.Equal(a.Player.Hp, b.Player.Hp);
        Assert.Equal(a.Player.Stamina, b.Player.Stamina);
        Assert.Equal(a.Player.Rations, b.Player.Rations);
        Assert.Equal(a.Turn, b.Turn);
        Assert.Equal(a.Sky, b.Sky);
        Assert.Equal(a.OnRoad, b.OnRoad);
    }

    /// <summary>A plain valley tile (grass, wood, or hills) clear of everyone, near the map's middle.</summary>
    private static Pos OpenGround(GameMap map, Game game)
    {
        for (int y = 2; y < map.Height - 2; y++)
            for (int x = 2; x < map.Width - 2; x++)
            {
                var p = new Pos(x, y);
                if (map[p] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)) continue;
                if (game.World.Npcs.Any(n => !n.OnRoad && n.Pos == p)) continue;
                if (game.World.Herbs.Contains(p) || game.World.Gleanings.Contains(p)) continue;
                if (game.World.WildPonyPos is { } pony && pony.Chebyshev(p) <= 2) continue;
                return p;
            }
        throw new InvalidOperationException("no open ground");
    }

    /// <summary>One step onto adjacent plain road ground (asserts such a step exists).</summary>
    private static void StepOntoPlain(Game game)
    {
        var p = game.Player.Pos;
        foreach (var (dx, dy) in Directions.All8)
        {
            var q = p.Plus(dx, dy);
            if (!game.World.Road.Walkable(q)) continue;
            if (game.World.Road[q] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)) continue;
            if (game.World.Npcs.Any(n => n.OnRoad && n.Pos == q)) continue;
            if (game.World.RoadHerbs.Contains(q)) continue;
            game.ApplyKey(KeyFor(dx, dy));
            return;
        }
        throw new InvalidOperationException("no plain step from here");
    }

    /// <summary>Puts the bearer on plain road ground where a camp is allowed.</summary>
    private static void MoveToPlain(Game game)
    {
        if (game.World.Road[game.Player.Pos] is Terrain.Grass or Terrain.Forest or Terrain.Hills) return;
        StepOntoPlain(game);
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => '.',
    };
}
