using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The black tarn and its finite fishing loop (D-156).</summary>
public class BlackTarnTests
{
    [Fact]
    public void Generation_PlacesOneDeterministicTarn_WithThreeReachableBanks()
    {
        for (ulong seed = 1; seed <= 100; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);
            var tarn = a.BlackTarnSite;

            Assert.Equal(SiteKind.BlackTarn, tarn.Kind);
            Assert.Equal(Area.Fells, tarn.Area);
            Assert.Empty(tarn.Spawns);
            Assert.True(tarn.ChestLooted);
            Assert.Equal(Terrain.TarnEntrance, a.Fells[tarn.OverworldPos]);
            Assert.Equal(tarn.OverworldPos, b.BlackTarnSite.OverworldPos);
            Assert.Equal(tarn.Map.ContentHash(), b.BlackTarnSite.Map.ContentHash());
            Assert.Equal(FellFishing.ReachesPerWorld, tarn.FishingReaches.Count);
            Assert.DoesNotContain(tarn.OverworldPos, a.FellHerbs);
            Assert.DoesNotContain(tarn.OverworldPos, a.TarnIronSeams);
            Assert.Contains(tarn.OverworldPos, Reachable(a.Fells, a.FellHomePos));

            var inside = Reachable(tarn.Map, tarn.EntryPos);
            Assert.All(tarn.FishingReaches, reach =>
            {
                Assert.Equal(Terrain.FishingReach, tarn.Map[reach]);
                Assert.Contains(reach, inside);
                Assert.Contains(Directions.All8, d => tarn.Map.InBounds(reach.Plus(d.dx, d.dy))
                    && tarn.Map[reach.Plus(d.dx, d.dy)] == Terrain.Water);
            });

            var measured = WorldEval.Measure(a).Sites.Single(s => s.Id == "black-tarn");
            Assert.Equal("blacktarn", measured.Kind);
            Assert.Equal(FellFishing.ReachesPerWorld, measured.FishingReaches);
        }
    }

    [Fact]
    public void TheWaykeeper_SellsOnePermanentLine_AndRefusesShortCoin()
    {
        var game = new Game(42);
        EnterRoad(game);
        Bump(game, game.World.Waykeeper);
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
        Assert.Contains(game.Topics, t => t.Label == "The fells" && t.Answer.Contains("black tarn"));
        int gear = game.Player.AllGear.Count();

        game.ApplyKey(OfferKey(game, TradeGood.FishingLine));
        Assert.False(game.Player.FishingLine);
        Assert.Equal(0, game.Player.Coin);

        game.Player.Coin = FellFishing.LinePrice;
        game.ApplyKey(OfferKey(game, TradeGood.FishingLine));
        Assert.True(game.Player.FishingLine);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(gear, game.Player.AllGear.Count());

        game.Player.Coin = FellFishing.LinePrice;
        game.ApplyKey(OfferKey(game, TradeGood.FishingLine));
        Assert.Equal(FellFishing.LinePrice, game.Player.Coin);
    }

    [Fact]
    public void AReach_RefusesBareHands_ThenSpendsEightTurns_AndFeedsSurvival()
    {
        var game = EnterTarn();
        var tarn = game.World.BlackTarnSite;
        var reach = tarn.FishingReaches[0];
        game.Debug_SetPlayerPos(reach);
        int turn = game.Turn;

        game.ApplyKey('g');
        Assert.Equal(turn, game.Turn);
        Assert.Contains(reach, tarn.FishingReaches);

        game.Player.FishingLine = true;
        while (game.Player.Skills.Level(SkillId.Survival) < 2)
            game.Player.Skills.AddUse(SkillId.Survival);
        int uses = game.Player.Skills.Uses(SkillId.Survival);
        int expected = Math.Min(FellFishing.MaxYield,
            1 + game.Player.Skills.Bonus(SkillId.Survival));
        game.ApplyKey('g');

        Assert.Equal(turn + FellFishing.WorkTurns, game.Turn);
        Assert.Equal(expected, game.Player.TarnTrout);
        Assert.Equal(uses + 1, game.Player.Skills.Uses(SkillId.Survival));
        Assert.DoesNotContain(reach, tarn.FishingReaches);
        Assert.Equal(Terrain.Heath, tarn.Map[reach]);
        Assert.Equal(expected, game.TakeSnapshot().TarnTrout);
        Assert.Equal(FellFishing.ReachesPerWorld - 1, game.TakeSnapshot().FishingReaches);

        turn = game.Turn;
        game.ApplyKey('g');
        Assert.Equal(turn, game.Turn);
    }

    [Fact]
    public void ThreeSittings_ExhaustAndCompleteTheSite_UntilTheNextWorld()
    {
        var game = EnterTarn();
        game.Player.FishingLine = true;
        var tarn = game.World.BlackTarnSite;

        foreach (var reach in tarn.FishingReaches.ToList())
        {
            game.Debug_SetPlayerPos(reach);
            game.ApplyKey('g');
        }

        Assert.True(tarn.Cleared);
        Assert.Empty(tarn.FishingReaches);
        Assert.True(game.World.Facts.Exists("resource-state", "black_tarn_worked"));

        Cross(game);
        Assert.True(game.Player.FishingLine);
        Assert.Equal(FellFishing.ReachesPerWorld, game.World.BlackTarnSite.FishingReaches.Count);
        Assert.False(game.World.BlackTarnSite.Cleared);
    }

    [Fact]
    public void Presentation_ShowsTheMouth_ThreeReachGlyphs_Guidance_AndCompletion()
    {
        var game = EnterTarn();
        game.Player.FishingLine = true;
        var tarn = game.World.BlackTarnSite;
        var lines = Presenter.Render(game, 120, 40).ToTextLines();
        Assert.Contains(lines, line => line.Contains("The black tarn"));
        Assert.Contains(lines, line => line.Contains("Fishing reaches: 3"));
        Assert.Equal(FellFishing.ReachesPerWorld,
            lines.Take(32).Sum(line => line.Take(87).Count(ch => ch == 'f')));

        foreach (var reach in tarn.FishingReaches.ToList())
        {
            game.Debug_SetPlayerPos(reach);
            game.ApplyKey('g');
        }
        game.Debug_SetPlayerPos(tarn.EntryPos);
        game.ApplyKey('<');
        StepOnto(game, tarn.OverworldPos);

        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("Every known reach"));
    }

    [Fact]
    public void TheFixedFire_CooksFish_WithTheCookingBonus_AndHonorsTheCap()
    {
        var game = new Game(42);
        while (game.Player.Skills.Level(SkillId.Cooking) < 2)
            game.Player.Skills.AddUse(SkillId.Cooking);
        game.Player.TarnTrout = 2;
        int uses = game.Player.Skills.Uses(SkillId.Cooking);
        OpenWoodwardBench(game);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.CookFish
            && o.Label.Contains("2 fish into 3 ration"));

        game.ApplyKey(TradeKey(game, TradeGood.CookFish));
        Assert.Equal(0, game.Player.TarnTrout);
        Assert.Equal(3, game.Player.Rations);
        Assert.Equal(uses + 1, game.Player.Skills.Uses(SkillId.Cooking));

        game.Player.TarnTrout = 4;
        game.Player.Rations = Game.RationCap - 1;
        game.ApplyKey(TradeKey(game, TradeGood.CookFish));
        Assert.Equal(Game.RationCap, game.Player.Rations);
        Assert.Equal(3, game.Player.TarnTrout);

        game.ApplyKey(TradeKey(game, TradeGood.CookFish));
        Assert.Equal(Game.RationCap, game.Player.Rations);
        Assert.Equal(3, game.Player.TarnTrout);
    }

    [Fact]
    public void Camp_CooksFishBeforeSupper_ThroughTheExistingAutomaticModel()
    {
        var game = new Game(42);
        game.Player.TarnTrout = 2;
        game.Player.Rations = 0;
        game.Debug_SetPlayerPos(OpenCampGround(game));
        int uses = game.Player.Skills.Uses(SkillId.Cooking);

        game.ApplyKey('m');

        Assert.Equal(0, game.Player.TarnTrout);
        Assert.Equal(1, game.Player.Rations);
        Assert.Equal(uses + 1, game.Player.Skills.Uses(SkillId.Cooking));
    }

    [Fact]
    public void TheTownCounter_BuysFish_WithBondHaggleTitheAndCommerce()
    {
        var game = GameAt(WorldTwist.HeldRoad);
        while (game.Player.Skills.Level(SkillId.Commerce) < 1)
            game.Player.Skills.AddUse(SkillId.Commerce);
        game.Player.Coin = 100;
        EnterTown(game);
        Bump(game, game.NpcsHere.Single(n => n.Id == "npc_guildmaster"));
        game.ApplyKey(OfferKey(game, TradeGood.Bond));
        Assert.True(game.GuildSworn);
        game.ApplyKey('z');

        game.Player.TarnTrout = 2;
        int coin = game.Player.Coin;
        int commerceLevel = game.Player.Skills.Level(SkillId.Commerce);
        int commerceUses = game.Player.Skills.Uses(SkillId.Commerce);
        int tithes = game.RoadTithes;
        Bump(game, game.NpcsHere.Single(n => n.Id == "npc_provisioner"));
        game.ApplyKey(OfferKey(game, TradeGood.TarnTrout));

        int expected = 2 * FellFishing.TroutPrice + commerceLevel
            + CarriersGuild.LotBonus - WorldTwistCatalog.RoadTithe;
        Assert.Equal(0, game.Player.TarnTrout);
        Assert.Equal(coin + expected, game.Player.Coin);
        Assert.Equal(tithes + 1, game.RoadTithes);
        Assert.Equal(commerceUses + 1, game.Player.Skills.Uses(SkillId.Commerce));
    }

    [Fact]
    public void ABarredTownCounter_RefusesTheFishLot()
    {
        var game = new Game(42);
        game.Player.TarnTrout = 3;
        game.Player.Coin = 5;
        EnterTown(game);
        game.Debug_RaiseTownBook(TownLaw.BarredRung);
        Bump(game, game.NpcsHere.Single(n => n.Id == "npc_provisioner"));

        game.ApplyKey(OfferKey(game, TradeGood.TarnTrout));

        Assert.Equal(3, game.Player.TarnTrout);
        Assert.Equal(5, game.Player.Coin);
    }

    [Fact]
    public void LineAndFish_SurviveDeathAndCrossing_WhileReachesRenew()
    {
        var game = new Game(42);
        game.Player.FishingLine = true;
        game.Player.TarnTrout = 4;
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        Assert.True(game.Player.FishingLine);
        Assert.Equal(4, game.Player.TarnTrout);

        Cross(game);

        Assert.True(game.Player.FishingLine);
        Assert.Equal(4, game.Player.TarnTrout);
        Assert.Equal(FellFishing.ReachesPerWorld, game.World.BlackTarnSite.FishingReaches.Count);
        var snapshot = game.TakeSnapshot();
        Assert.True(snapshot.FishingLine);
        Assert.Equal(4, snapshot.TarnTrout);
    }

    private static Game EnterTarn()
    {
        var game = new Game(42);
        FrontierTests.ClimbFells(game);
        game.Debug_SetPlayerPos(game.World.BlackTarnSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(SiteKind.BlackTarn, game.CurrentSite!.Kind);
        return game;
    }

    private static void EnterRoad(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.Equal(Area.Road, game.Area);
    }

    private static void EnterTown(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        EnterRoad(game);
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(SiteKind.Town, game.CurrentSite!.Kind);
    }

    private static void OpenWoodwardBench(Game game)
    {
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.True(game.InTradeMenu);
    }

    private static void Cross(Game game)
    {
        if (game.Mode == MapMode.Site)
        {
            game.Debug_SetPlayerPos(game.CurrentSite!.EntryPos);
            game.ApplyKey('<');
        }
        if (game.Area == Area.Fells)
        {
            game.Debug_SetPlayerPos(game.World.FellHomePos);
            game.ApplyKey('>');
        }
        if (game.Area == Area.Road)
        {
            game.Debug_SetPlayerPos(game.World.RoadHomePos);
            game.ApplyKey('>');
        }
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }

    private static Game GameAt(WorldTwist twist)
    {
        ulong seed = Enumerable.Range(1, 500).Select(i => (ulong)i)
            .First(s => WorldTwistCatalog.ForCycle(s, WorldTwistCatalog.FirstTier) == twist);
        var game = new Game(seed);
        while (game.Cycle < WorldTwistCatalog.FirstTier) Cross(game);
        return game;
    }

    private static void Bump(Game game, Npc npc)
    {
        var beside = Directions.All8.Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p) && !game.NpcsHere.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static char TradeKey(Game game, TradeGood good) =>
        (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == good));

    private static Pos OpenCampGround(Game game)
    {
        for (int y = 1; y < game.CurrentMap.Height - 1; y++)
            for (int x = 1; x < game.CurrentMap.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (game.CurrentMap[p] is Terrain.Grass or Terrain.Forest or Terrain.Hills)
                    return p;
            }
        throw new InvalidOperationException("no camp ground");
    }

    private static void StepOnto(Game game, Pos target)
    {
        foreach (var (dx, dy) in Directions.All8)
        {
            var from = target.Plus(-dx, -dy);
            if (!game.CurrentMap.Walkable(from)) continue;
            game.Debug_SetPlayerPos(from);
            game.ApplyKey(KeyFor(dx, dy));
            if (game.Player.Pos == target) return;
        }
        throw new InvalidOperationException("no approach to the black tarn");
    }

    private static HashSet<Pos> Reachable(GameMap map, Pos from)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var (dx, dy) in Directions.All8)
            {
                var q = p.Plus(dx, dy);
                if (map.Walkable(q) && seen.Add(q)) queue.Enqueue(q);
            }
        }
        return seen;
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
