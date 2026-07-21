using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The named countries (D-143, plan 2026-07 B3): the region made an entity,
/// the box D-049 left open. Every world names its valley and its road's high
/// country on their own stream, in the world's own tongue, distinct from
/// every other name the world carries; the facts, the road's crossings, and
/// the doors at both ends make the naming perceivable (D-023).
/// </summary>
public class RegionTests
{
    [Fact]
    public void TheWorld_NamesItsCountries()
    {
        var game = new Game(42);
        // Two countries at D-143's landing; the fells made a third (D-146).
        Assert.Equal(3, game.World.Regions.Count);
        Assert.Equal(game.World.Regions[0], game.World.ValleyRegion);
        Assert.Equal(game.World.Regions[1], game.World.RoadRegion);
        Assert.Equal(game.World.Regions[2], game.World.FellRegion);
        Assert.False(string.IsNullOrEmpty(game.World.ValleyRegion.Name));
        Assert.False(string.IsNullOrEmpty(game.World.RoadRegion.Name));

        // The naming is written (D-023): a country nobody can hear of is a label.
        Assert.Equal(game.World.ValleyRegion.Name, game.World.Facts.Find("region", "valley")!.Object);
        Assert.Equal(game.World.RoadRegion.Name, game.World.Facts.Find("region", "road")!.Object);
    }

    [Fact]
    public void EverySeed_KeepsItsNamesApart()
    {
        // The D-049 rule carried forward: disjoint closer pools mean a region
        // can never share a full name with its world, its stead, its town,
        // or the world's other country (that last one by reroll).
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var world = WorldGen.Generate(seed);
            Assert.NotEqual(world.ValleyRegion.Name, world.RoadRegion.Name);
            foreach (var region in world.Regions)
            {
                Assert.NotEqual(world.Name, region.Name);
                Assert.NotEqual(world.SettlementName, region.Name);
                Assert.NotEqual(world.TownName, region.Name);
            }
        }
    }

    [Fact]
    public void TheSameSeed_DealsTheSameCountry()
    {
        var first = WorldGen.Generate(7);
        var again = WorldGen.Generate(7);
        Assert.Equal(first.ValleyRegion.Name, again.ValleyRegion.Name);
        Assert.Equal(first.RoadRegion.Name, again.RoadRegion.Name);
    }

    [Fact]
    public void TheRoad_IsTakenByName()
    {
        var game = new Game(42);
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains($"into the {game.World.RoadRegion.Name}"));

        game.ApplyKey('>');
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains($"into the {game.World.ValleyRegion.Name}"));
    }

    [Fact]
    public void TheDoors_KnowTheCountry_AtBothEnds()
    {
        // The valley's doors point east by name; the town claims its own
        // country in the same breath as its wall.
        var game = new Game(42);
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);
        Assert.Contains(game.World.RoadRegion.Name, game.Topics.First(t => t.Label == "The stead").Answer);
        game.ApplyKey(' ');

        NewsTests.EnterTown(game);
        NewsTests.BumpTowner(game, "npc_provisioner");
        Assert.Contains(game.World.RoadRegion.Name, game.Topics.First(t => t.Label == "The town").Answer);
    }
}
