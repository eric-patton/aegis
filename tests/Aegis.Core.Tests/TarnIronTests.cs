using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The high fells' tarn-iron economy (D-153).</summary>
public class TarnIronTests
{
    [Fact]
    public void Generation_PlacesFourDeterministicReachableSeams_AndMeasuresThem()
    {
        for (ulong seed = 1; seed <= 100; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            Assert.Equal(FellIron.SeamsPerWorld, a.TarnIronSeams.Count);
            Assert.Equal(a.TarnIronSeams, b.TarnIronSeams);
            Assert.Equal(a.Fells.ContentHash(), b.Fells.ContentHash());
            Assert.All(a.TarnIronSeams, seam =>
            {
                Assert.Equal(Terrain.TarnIron, a.Fells[seam]);
                Assert.Contains(seam, Reachable(a.Fells, a.FellHomePos));
            });
            Assert.True(a.Facts.Exists("resource", "tarn_iron"));
            Assert.Equal(FellIron.SeamsPerWorld, WorldEval.Measure(a).TarnIronSeams);
        }
    }

    [Fact]
    public void ASeam_RefusesBareHands_Blades_AndAWornHaft()
    {
        var game = new Game(42);
        FrontierTests.ClimbFells(game);
        var seam = game.World.TarnIronSeams[0];
        game.Debug_SetPlayerPos(seam);
        int turn = game.Turn;

        game.ApplyKey('g');
        Assert.Equal(turn, game.Turn);
        Assert.Contains(seam, game.World.TarnIronSeams);

        game.Player.Weapon = GearCatalog.Create("grave_iron");
        game.ApplyKey('g');
        Assert.Equal(turn, game.Turn);
        Assert.Contains(seam, game.World.TarnIronSeams);

        var worn = GearCatalog.Create("woodaxe");
        worn.Wear = worn.MaxWear;
        game.Player.Weapon = worn;
        game.ApplyKey('g');
        Assert.Equal(turn, game.Turn);
        Assert.Contains(seam, game.World.TarnIronSeams);
    }

    [Fact]
    public void WorkingASeam_SpendsTimeAndWear_FeedsSurvival_AndExhaustsIt()
    {
        var game = new Game(42);
        FrontierTests.ClimbFells(game);
        var seam = game.World.TarnIronSeams[0];
        var tool = GearCatalog.Create("woodaxe");
        game.Player.Weapon = tool;
        game.Debug_SetPlayerPos(seam);
        int turn = game.Turn;
        int seams = game.World.TarnIronSeams.Count;

        game.ApplyKey('g');

        Assert.Equal(turn + FellIron.WorkTurns, game.Turn);
        Assert.Equal(FellIron.ToolWear, tool.Wear);
        Assert.Equal(1, game.Player.TarnIron);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Survival));
        Assert.Equal(seams - 1, game.World.TarnIronSeams.Count);
        Assert.Equal(Terrain.Heath, game.World.Fells[seam]);
        Assert.True(game.World.Facts.Exists("resource-state", $"tarn_iron_{seam.X}_{seam.Y}"));
        Assert.Equal(game.Player.TarnIron, game.TakeSnapshot().TarnIron);
    }

    [Fact]
    public void WolfWinter_OpensOneMorePieceFromTheSameSeam()
    {
        var fair = new Game(42);
        var winter = new Game(42);
        FrontierTests.ClimbFells(fair);
        FrontierTests.ClimbFells(winter);
        fair.Player.Weapon = GearCatalog.Create("woodaxe");
        winter.Player.Weapon = GearCatalog.Create("woodaxe");
        fair.Debug_SetPlayerPos(fair.World.TarnIronSeams[0]);
        winter.Debug_SetPlayerPos(winter.World.TarnIronSeams[0]);
        winter.Debug_SetFellWinter(FellWinter.Ticks);

        fair.ApplyKey('g');
        winter.ApplyKey('g');

        Assert.Equal(fair.Player.TarnIron + FellIron.WinterYield, winter.Player.TarnIron);
    }

    [Fact]
    public void ForgeAndGuild_CloseTheLoop_AndFeedTheirCrafts()
    {
        var game = new Game(42);
        game.Player.TarnIron = 3;
        game.Player.Coin = FellIron.SmeltCoin;
        EnterTown(game);
        Bump(game, "npc_townsmith");
        Assert.Contains(game.Offers, o => o.Good == TradeGood.TarnSmelt && o.Label.Contains("3 raw"));

        game.ApplyKey(OfferKey(game, TradeGood.TarnSmelt));

        Assert.Equal(0, game.Player.TarnIron);
        Assert.Equal(3, game.Player.IronBloom);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Smithing));
        game.ApplyKey(' ');

        Bump(game, "npc_guildmaster");
        Assert.Contains(game.Offers, o => o.Good == TradeGood.IronBloom && o.Label.Contains("3 at 4c, 12 coin"));
        game.ApplyKey(OfferKey(game, TradeGood.IronBloom));

        Assert.Equal(0, game.Player.IronBloom);
        Assert.Equal(3 * FellIron.BloomPrice, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Commerce));
        Assert.Equal(0, game.TakeSnapshot().IronBloom);
    }

    [Fact]
    public void HeldRoad_TithesTheForgeAndGuildAsOfficialTownBusiness()
    {
        var game = GameAt(WorldTwist.HeldRoad);
        game.Player.TarnIron = 2;
        game.Player.Coin = 10;
        EnterTown(game);
        Bump(game, "npc_townsmith");
        game.ApplyKey(OfferKey(game, TradeGood.TarnSmelt));
        Assert.Equal(1, game.RoadTithes);
        Assert.Equal(10 - FellIron.SmeltCoin - WorldTwistCatalog.RoadTithe, game.Player.Coin);
        game.ApplyKey(' ');

        Bump(game, "npc_guildmaster");
        game.ApplyKey(OfferKey(game, TradeGood.IronBloom));
        Assert.Equal(2, game.RoadTithes);
        Assert.Equal(10 - FellIron.SmeltCoin - WorldTwistCatalog.RoadTithe
            + 2 * FellIron.BloomPrice - WorldTwistCatalog.RoadTithe, game.Player.Coin);
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
    }

    private static Game GameAt(WorldTwist twist)
    {
        ulong seed = Enumerable.Range(1, 500).Select(i => (ulong)i)
            .First(s => WorldTwistCatalog.ForCycle(s, WorldTwistCatalog.FirstTier) == twist);
        var game = new Game(seed);
        while (game.Cycle < WorldTwistCatalog.FirstTier)
        {
            game.Debug_SetMode(MapMode.Overworld);
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        return game;
    }

    private static void EnterTown(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
    }

    private static void Bump(Game game, string id)
    {
        var npc = game.NpcsHere.Single(n => n.Id == id);
        var beside = Directions.All8.Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p) && !game.NpcsHere.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static HashSet<Pos> Reachable(GameMap map, Pos from)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var (dx, dy) in Directions.Cardinal)
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
