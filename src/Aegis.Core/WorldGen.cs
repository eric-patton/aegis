namespace Aegis.Core;

public sealed class World
{
    public required ulong Seed { get; init; }
    public required string Name { get; init; }
    public required string SettlementName { get; init; }
    public required FactGraph Facts { get; init; }
    public required GameMap Overworld { get; init; }
    public required GameMap Camp { get; init; }
    public required Pos ShrinePos { get; init; }
    public required Pos CampPos { get; init; }
    public required Pos CampEntryPos { get; init; }
    public required List<Pos> GoblinSpawns { get; init; }
    public required Pos ChestPos { get; init; }
}

/// <summary>
/// Slice worldgen: one overworld (value-noise biomes, a settlement with a shrine,
/// a goblin camp site) and one cave map for the camp. Every step draws from its own
/// derived seed stream so subsystems never share RNG state.
/// </summary>
public static class WorldGen
{
    public const int OverworldW = 64;
    public const int OverworldH = 36;
    public const int CampW = 30;
    public const int CampH = 18;

    public static World Generate(ulong masterSeed)
    {
        var facts = new FactGraph();

        var nameRng = new Rng(SeedTree.Derive(masterSeed, "names"));
        string worldName = NameGen.World(ref nameRng);
        string settlementName = NameGen.Settlement(ref nameRng);

        var overworld = GenerateOverworld(masterSeed);
        var placeRng = new Rng(SeedTree.Derive(masterSeed, "placement"));

        Pos settlement = FindOpenSpot(overworld, ref placeRng, new Pos(OverworldW / 4, OverworldH / 2), 12);
        PlaceSettlement(overworld, settlement);
        Pos shrine = settlement.Plus(0, 2);
        overworld[shrine] = Terrain.Shrine;

        Pos camp = FindDistantSpot(overworld, ref placeRng, settlement, minDistance: 30);
        overworld[camp] = Terrain.CampEntrance;
        CarvePathIfDisconnected(overworld, shrine, camp);

        var (campMap, entry, goblinSpawns, chest) = GenerateCamp(masterSeed);

        facts.Add("world_name", worldName, "");
        facts.Add("settlement", settlementName, $"{settlement.X},{settlement.Y}", "A small stead under the Aegis-shrine.");
        facts.Add("rest_point", "shrine", $"{shrine.X},{shrine.Y}", $"The shrine at {settlementName}. The Aegis anchors here.");
        facts.Add("site", "goblin_camp", $"{camp.X},{camp.Y}", "A cave the goblins have made their own.");
        facts.Add("grievance", "goblin_camp", settlementName, $"Goblins from the cave raid {settlementName}'s stores by night.");

        return new World
        {
            Seed = masterSeed,
            Name = worldName,
            SettlementName = settlementName,
            Facts = facts,
            Overworld = overworld,
            Camp = campMap,
            ShrinePos = shrine,
            CampPos = camp,
            CampEntryPos = entry,
            GoblinSpawns = goblinSpawns,
            ChestPos = chest,
        };
    }

    private static GameMap GenerateOverworld(ulong masterSeed)
    {
        ulong terrainSeed = SeedTree.Derive(masterSeed, "overworld-terrain");
        var map = new GameMap("overworld", OverworldW, OverworldH, Terrain.Grass);

        for (int y = 0; y < OverworldH; y++)
        {
            for (int x = 0; x < OverworldW; x++)
            {
                double n = 0.65 * ValueNoise(terrainSeed, x / 9.0, y / 9.0)
                         + 0.35 * ValueNoise(terrainSeed, x / 4.0, y / 4.0);
                map[new Pos(x, y)] = n switch
                {
                    < 0.34 => Terrain.Water,
                    < 0.58 => Terrain.Grass,
                    < 0.76 => Terrain.Forest,
                    _ => Terrain.Hills,
                };
            }
        }
        return map;
    }

    private static double ValueNoise(ulong seed, double x, double y)
    {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        double fx = x - x0, fy = y - y0;
        double sx = fx * fx * (3 - 2 * fx);
        double sy = fy * fy * (3 - 2 * fy);

        double v00 = Lattice(seed, x0, y0);
        double v10 = Lattice(seed, x0 + 1, y0);
        double v01 = Lattice(seed, x0, y0 + 1);
        double v11 = Lattice(seed, x0 + 1, y0 + 1);

        double top = v00 + (v10 - v00) * sx;
        double bottom = v01 + (v11 - v01) * sx;
        return top + (bottom - top) * sy;
    }

    private static double Lattice(ulong seed, int x, int y)
    {
        var rng = new Rng(SeedTree.Derive(seed, "lattice", x, y));
        return rng.NextDouble();
    }

    private static Pos FindOpenSpot(GameMap map, ref Rng rng, Pos near, int radius)
    {
        for (int attempt = 0; attempt < 500; attempt++)
        {
            var p = new Pos(
                Math.Clamp(near.X + rng.Range(-radius, radius + 1), 2, map.Width - 4),
                Math.Clamp(near.Y + rng.Range(-radius, radius + 1), 2, map.Height - 4));
            if (AreaIsLand(map, p)) return p;
        }
        // Deterministic fallback: scan for the first viable spot.
        for (int y = 2; y < map.Height - 3; y++)
            for (int x = 2; x < map.Width - 3; x++)
                if (AreaIsLand(map, new Pos(x, y))) return new Pos(x, y);
        throw new InvalidOperationException("Worldgen produced no land for the settlement.");
    }

    private static bool AreaIsLand(GameMap map, Pos center)
    {
        for (int dy = -1; dy <= 2; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var p = center.Plus(dx, dy);
                if (!map.InBounds(p) || map[p] == Terrain.Water) return false;
            }
        return true;
    }

    private static void PlaceSettlement(GameMap map, Pos center)
    {
        map[center.Plus(-1, 0)] = Terrain.House;
        map[center.Plus(1, 0)] = Terrain.House;
        map[center.Plus(0, -1)] = Terrain.House;
        map[center] = Terrain.Grass;
    }

    private static Pos FindDistantSpot(GameMap map, ref Rng rng, Pos from, int minDistance)
    {
        Pos best = from;
        int bestDist = -1;
        for (int attempt = 0; attempt < 800; attempt++)
        {
            var p = new Pos(rng.Range(2, map.Width - 2), rng.Range(2, map.Height - 2));
            if (map[p] == Terrain.Water || map[p] == Terrain.House || map[p] == Terrain.Shrine) continue;
            int d = p.Manhattan(from);
            if (d >= minDistance) return p;
            if (d > bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    /// <summary>Guarantees the camp is reachable from the shrine by carving a walkable line if needed.</summary>
    private static void CarvePathIfDisconnected(GameMap map, Pos from, Pos to)
    {
        if (Reachable(map, from, to)) return;

        Pos p = from;
        while (p != to)
        {
            if (p.X != to.X) p = p.Plus(Math.Sign(to.X - p.X), 0);
            else p = p.Plus(0, Math.Sign(to.Y - p.Y));
            if (map[p] is Terrain.Water or Terrain.House) map[p] = Terrain.Grass;
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

    private static (GameMap Map, Pos Entry, List<Pos> Goblins, Pos Chest) GenerateCamp(ulong masterSeed)
    {
        var rng = new Rng(SeedTree.Derive(masterSeed, "site-goblin-camp"));
        var map = new GameMap("goblin-camp", CampW, CampH, Terrain.Wall);

        var entry = new Pos(2, CampH / 2);
        var carved = new List<Pos>();

        // Drunkard's walk carve until ~42 percent floor.
        Pos cursor = entry;
        map[cursor] = Terrain.Floor;
        carved.Add(cursor);
        int target = (int)(CampW * CampH * 0.42);
        int guard = CampW * CampH * 60;
        while (carved.Count < target && guard-- > 0)
        {
            var (dx, dy) = rng.Pick(Directions.Cardinal);
            var next = cursor.Plus(dx, dy);
            if (next.X < 1 || next.X >= CampW - 1 || next.Y < 1 || next.Y >= CampH - 1) continue;
            cursor = next;
            if (map[cursor] == Terrain.Wall)
            {
                map[cursor] = Terrain.Floor;
                carved.Add(cursor);
            }
        }

        map[entry] = Terrain.ExitLadder;

        var deep = carved.Where(p => p.Manhattan(entry) > 10).ToList();
        if (deep.Count < 5) deep = carved.Where(p => p.Manhattan(entry) > 4).ToList();
        if (deep.Count < 5) deep = carved;

        var goblins = new List<Pos>();
        while (goblins.Count < 3)
        {
            var p = rng.Pick(deep);
            if (p != entry && !goblins.Contains(p)) goblins.Add(p);
        }

        Pos chest = rng.Pick(deep);
        while (chest == entry || goblins.Contains(chest)) chest = rng.Pick(deep);

        return (map, entry, goblins, chest);
    }
}

public static class Directions
{
    public static readonly (int dx, int dy)[] Cardinal = [(0, -1), (0, 1), (-1, 0), (1, 0)];
    public static readonly (int dx, int dy)[] All8 =
        [(0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)];
}
