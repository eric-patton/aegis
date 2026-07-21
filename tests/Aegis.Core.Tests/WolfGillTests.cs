using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The wolf-gill (D-150): the fells' third site, a scree-walled ravine with
/// the pack's source at its deep end. The great she-wolf runs the pack's own
/// rules with a heavier jaw; her pelt is the fells' one trophy (a cold camp
/// under it mends whole), and the drover's cache among the bones is the one
/// coin a wolf's ground honestly holds. The tests hold the generation, the
/// walk-in through real keys, the harvest, and the pelt's keep.
/// </summary>
public class WolfGillTests
{
    [Fact]
    public void TheGill_CutsTheTops_Deterministically()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);
            var gill = a.FellGillSite;

            Assert.Equal(SiteKind.Gill, gill.Kind);
            Assert.Equal(Area.Fells, gill.Area);
            Assert.Equal(gill.Map.ContentHash(), b.FellGillSite.Map.ContentHash());
            Assert.Equal(gill.OverworldPos, b.FellGillSite.OverworldPos);
            Assert.Equal(Terrain.GillEntrance, a.Fells[gill.OverworldPos]);
            Assert.True(a.Facts.Exists("site", "fell-gill"));

            // The palette is the fells' own: carved heath through solid scree.
            var seen = new HashSet<Terrain>();
            for (int y = 0; y < gill.Map.Height; y++)
                for (int x = 0; x < gill.Map.Width; x++)
                    seen.Add(gill.Map[new Pos(x, y)]);
            Assert.True(seen.IsSubsetOf((Terrain[])[Terrain.Heath, Terrain.Scree, Terrain.ExitLadder]),
                $"seed {seed}: the gill left its palette");

            // One she-wolf over the pack, and every den on reachable floor.
            var she = Assert.Single(gill.Spawns, s => s.Kind == MonsterKind.GreatWolf);
            Assert.True(gill.Spawns.Count(s => s.Kind == MonsterKind.Wolf) >= 3);
            var reach = Reachable(gill.Map, gill.EntryPos);
            Assert.All(gill.Spawns, s => Assert.Contains(s.Pos, reach));
            Assert.Contains(gill.ChestPos, reach);
            Assert.True(she.Hp > gill.Spawns.Where(s => s.Kind == MonsterKind.Wolf).Max(s => s.Hp),
                $"seed {seed}: the she-wolf should outweigh her pack");

            // The mouth reachable from the fells' own track.
            Assert.Contains(gill.OverworldPos, Reachable(a.Fells, a.FellHomePos));
        }
    }

    [Fact]
    public void TheGill_IsEntered_AndPaysCacheAndPelt()
    {
        var game = new Game(42);
        FrontierTests.ClimbFells(game);
        var gill = game.World.FellGillSite;
        game.Debug_SetPlayerPos(gill.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
        Assert.Equal("fell-gill", game.CurrentSite!.Id);
        Assert.Contains(game.Monsters, m => m.Alive && m.Kind == MonsterKind.GreatWolf && m.SiteId == gill.Id);

        // The she-wolf harvested: game like her pack (no purse, no essence),
        // heavier in hides, and the pelt taken once ever.
        var she = game.Monsters.First(m => m.Kind == MonsterKind.GreatWolf);
        int hidesBefore = game.Player.Hide;
        int coinBefore = game.Player.Coin;
        int essenceBefore = game.Player.Essence;
        she.Hp = 1;
        var beside = Directions.All8
            .Select(d => she.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentSite!.Map.Walkable(p)
                && !game.Monsters.Any(m => m.Alive && m.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(she.Pos.X - beside.X, she.Pos.Y - beside.Y));
        Assert.False(she.Alive);
        Assert.True(game.Player.WolfPelt);
        Assert.True(game.Player.Hide >= hidesBefore + 3);
        Assert.Equal(coinBefore, game.Player.Coin);
        Assert.Equal(essenceBefore, game.Player.Essence);

        // The rest of the pack down, the site holds, and the cache pays.
        game.Debug_ClearSite(SiteKind.Gill);
        Assert.True(gill.Cleared);
        game.Debug_SetPlayerPos(gill.ChestPos);
        coinBefore = game.Player.Coin;
        game.ApplyKey('g');
        Assert.True(gill.ChestLooted);
        Assert.True(game.Player.Coin > coinBefore);
    }

    [Fact]
    public void TheGreatPelt_HoldsTheColdOut()
    {
        // Twin camps on the fells under a cold sky, raw meat in both packs;
        // the pelt is the only difference, so the mend gap is its keep.
        int Mend(bool pelt)
        {
            var game = new Game(42);
            game.Player.WolfPelt = pelt;
            FrontierTests.ClimbFells(game);
            game.Debug_SetSky(RoadSky.Cold);
            var fells = game.World.Fells;
            Pos heath = default;
            for (int y = 0; y < fells.Height && heath == default; y++)
                for (int x = 0; x < fells.Width && heath == default; x++)
                    if (fells[new Pos(x, y)] == Terrain.Heath) heath = new Pos(x, y);
            game.Debug_SetPlayerPos(heath);
            game.Player.Rations = 2;
            game.Player.Hp = 1;
            int before = game.Player.Hp;
            game.ApplyKey('m');
            return game.Player.Hp - before;
        }

        int cold = Mend(pelt: false);
        int warm = Mend(pelt: true);
        Assert.True(cold > 0);
        Assert.True(warm >= cold * 2, $"the pelt should hold the halving off (cold {cold}, warm {warm})");
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
        _ => 'n',
    };

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
                var next = p.Plus(dx, dy);
                if (map.InBounds(next) && map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return seen;
    }
}
