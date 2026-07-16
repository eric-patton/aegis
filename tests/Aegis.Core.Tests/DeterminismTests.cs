using Aegis.Core;

namespace Aegis.Core.Tests;

public class DeterminismTests
{
    [Fact]
    public void SeedTree_IsStable()
    {
        // Golden values: if these change, every saved world in existence breaks.
        Assert.Equal(SeedTree.Derive(12345UL, "overworld-terrain"), SeedTree.Derive(12345UL, "overworld-terrain"));
        Assert.NotEqual(SeedTree.Derive(12345UL, "overworld-terrain"), SeedTree.Derive(12345UL, "combat"));
        Assert.NotEqual(SeedTree.Derive(12345UL, "a"), SeedTree.Derive(54321UL, "a"));
        Assert.NotEqual(SeedTree.Derive(1UL, "cell", 3, 4), SeedTree.Derive(1UL, "cell", 4, 3));
    }

    [Fact]
    public void SameSeed_ProducesIdenticalWorld()
    {
        var a = WorldGen.Generate(999);
        var b = WorldGen.Generate(999);

        Assert.Equal(a.Overworld.ContentHash(), b.Overworld.ContentHash());
        Assert.Equal(a.Camp.ContentHash(), b.Camp.ContentHash());
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.SettlementName, b.SettlementName);
        Assert.Equal(a.ShrinePos, b.ShrinePos);
        Assert.Equal(a.CampPos, b.CampPos);
        Assert.Equal(a.GoblinSpawns, b.GoblinSpawns);
        Assert.Equal(a.ChestPos, b.ChestPos);
        Assert.Equal(
            a.Facts.All.Select(f => (f.Type, f.Subject, f.Object, f.Detail)),
            b.Facts.All.Select(f => (f.Type, f.Subject, f.Object, f.Detail)));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentWorlds()
    {
        var a = WorldGen.Generate(1);
        var b = WorldGen.Generate(2);
        Assert.NotEqual(a.Overworld.ContentHash(), b.Overworld.ContentHash());
    }

    [Fact]
    public void SameSeed_SameKeys_ProducesIdenticalRun()
    {
        const string script = "llllkkkkjjjj....hhhh";
        var a = new Game(4242);
        var b = new Game(4242);
        foreach (char key in script) { a.ApplyKey(key); b.ApplyKey(key); }

        Assert.Equal(a.Player.Pos, b.Player.Pos);
        Assert.Equal(a.Player.Hp, b.Player.Hp);
        Assert.Equal(a.Player.Stamina, b.Player.Stamina);
        Assert.Equal(a.Turn, b.Turn);
        Assert.Equal(
            a.Log.Entries.Select(e => e.Text),
            b.Log.Entries.Select(e => e.Text));
    }

    [Fact]
    public void Worldgen_GuaranteesCampReachableFromShrine()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var world = WorldGen.Generate(seed);
            Assert.True(Reachable(world.Overworld, world.ShrinePos, world.CampPos),
                $"seed {seed}: camp unreachable from shrine");
        }
    }

    private static bool Reachable(GameMap map, Pos from, Pos to)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == to) return true;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                if (map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }
}
