namespace Aegis.Core;

public enum SiteKind { GoblinCamp, Barrow, Hollow, Threshold }

/// <summary>A monster placed at generation time: kind, cell, and generated stats (D-011).</summary>
public readonly record struct MonsterSpawn(MonsterKind Kind, Pos Pos, int Hp);

/// <summary>
/// An enterable place on the overworld with its own map (D-033). Cleared/looted state
/// lives here because a Site's lifetime IS its world's: crossings regenerate both.
/// </summary>
public sealed class Site
{
    public required string Id { get; init; }
    public required SiteKind Kind { get; init; }
    public required GameMap Map { get; init; }
    public required Pos OverworldPos { get; init; }
    public required Pos EntryPos { get; init; }
    public required List<MonsterSpawn> Spawns { get; init; }
    public required Pos ChestPos { get; init; }
    public bool ChestLooted { get; set; }
    public bool Cleared { get; set; }
}

public sealed class World
{
    public required ulong Seed { get; init; }
    public required int Tier { get; init; }
    public required string Name { get; init; }
    public required string SettlementName { get; init; }
    public required FactGraph Facts { get; init; }
    public required GameMap Overworld { get; init; }
    public required Pos ShrinePos { get; init; }
    public required Pos GatePos { get; init; }
    public required List<Site> Sites { get; init; }
    public required List<Npc> Npcs { get; init; }

    /// <summary>Storylets compiled from this world's story template, cast-bound (D-032).</summary>
    public required List<Storylet> StoryStorylets { get; init; }

    public Site CampSite => Sites.First(s => s.Kind == SiteKind.GoblinCamp);
    public Site? BarrowSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Barrow);

    /// <summary>The wandering mender (D-034): cast into every world, every tier.</summary>
    public Npc Unbinder => Npcs.First(n => n.Kind == NpcKind.Unbinder);

    /// <summary>The stone ring where a severed one waits (D-037, tier 2+).</summary>
    public Site? HollowSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Hollow);

    /// <summary>The severed bearer at peace (D-038): tier 3+, met as a person, never a foe.</summary>
    public Npc? SeveredNpc => Npcs.FirstOrDefault(n => n.Kind == NpcKind.Severed);

    /// <summary>The last stair (D-039): tier 5+, the arc's fixed final stage. The door below answers to flags, not tiers.</summary>
    public Site? ThresholdSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Threshold);

    // Convenience views of the camp, the site every world has (and tests lean on).
    public GameMap Camp => CampSite.Map;
    public Pos CampPos => CampSite.OverworldPos;
    public Pos CampEntryPos => CampSite.EntryPos;
    public Pos ChestPos => CampSite.ChestPos;
    public List<Pos> GoblinSpawns => [.. CampSite.Spawns.Select(s => s.Pos)];
}

/// <summary>
/// The wandering mender's per-world guises (D-034): a trade name and the work line
/// its talk topic opens with. One deck, so guise and voice never drift apart.
/// </summary>
public static class UnbinderGuises
{
    public static readonly string[] Roles =
        ["tinker", "knife-grinder", "bone-setter", "mapmaker", "dowser", "chandler"];

    public static string WorkLine(string role) => role switch
    {
        "tinker" => "Pots mended, latches trued, small things made whole.",
        "knife-grinder" => "Edges brought back to what they were before the world wore them.",
        "bone-setter" => "Bones set, joints eased, old aches argued with.",
        "mapmaker" => "Roads drawn as they run, not as folk wish they ran.",
        "dowser" => "Water found where it hides from the thirsty.",
        "chandler" => "Candles for the hours the sun does not keep.",
        _ => "This and that, mended.",
    };
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

    /// <summary>
    /// Generates a world at a Hostility Tier (D-011): the tier is a GENERATION input
    /// (more and tougher goblins), never a post-hoc multiplier. Tier 1 with a given
    /// seed produces exactly what earlier versions produced from that seed alone,
    /// which is what keeps old save journals replayable.
    /// </summary>
    public static World Generate(ulong worldSeed, int tier = 1)
    {
        var facts = new FactGraph();

        var nameRng = new Rng(SeedTree.Derive(worldSeed, "names"));
        string worldName = NameGen.World(ref nameRng);
        string settlementName = NameGen.Settlement(ref nameRng);

        var overworld = GenerateOverworld(worldSeed);
        var placeRng = new Rng(SeedTree.Derive(worldSeed, "placement"));

        Pos settlement = FindOpenSpot(overworld, ref placeRng, new Pos(OverworldW / 4, OverworldH / 2), 12);
        PlaceSettlement(overworld, settlement);
        Pos shrine = settlement.Plus(0, 2);
        overworld[shrine] = Terrain.Shrine;

        Pos camp = FindDistantSpot(overworld, ref placeRng, settlement, minDistance: 30);
        overworld[camp] = Terrain.CampEntrance;
        CarvePathIfDisconnected(overworld, shrine, camp);

        Pos gate = FindDistantSpot(overworld, ref placeRng, settlement, minDistance: 22);
        while (gate == camp || gate.Manhattan(camp) < 5)
            gate = FindDistantSpot(overworld, ref placeRng, settlement, minDistance: 22);
        overworld[gate] = Terrain.Waygate;
        CarvePathIfDisconnected(overworld, shrine, gate);

        // Tier 2+ content band (D-033): the barrow. Placed after the tier-1 draws so
        // tier-1 worlds consume exactly the RNG they always did.
        Pos barrow = default;
        if (tier >= 2)
        {
            barrow = FindDistantSpot(overworld, ref placeRng, settlement, minDistance: 18);
            while (barrow == camp || barrow == gate || barrow.Manhattan(camp) < 5 || barrow.Manhattan(gate) < 5)
                barrow = FindDistantSpot(overworld, ref placeRng, settlement, minDistance: 18);
            overworld[barrow] = Terrain.BarrowEntrance;
            CarvePathIfDisconnected(overworld, shrine, barrow);
        }

        int goblinCount = Math.Min(3 + (tier - 1), 6);
        int goblinHp = 8 + 2 * (tier - 1);
        var (campMap, entry, goblinSpawns, chest) = GenerateCamp(worldSeed, goblinCount);
        var sites = new List<Site>
        {
            new()
            {
                Id = "goblin-camp",
                Kind = SiteKind.GoblinCamp,
                Map = campMap,
                OverworldPos = camp,
                EntryPos = entry,
                Spawns = [.. goblinSpawns.Select(p => new MonsterSpawn(MonsterKind.Goblin, p, goblinHp))],
                ChestPos = chest,
            },
        };

        if (tier >= 2)
        {
            int wightCount = Math.Min(2 + (tier - 2), 5);
            int wightHp = 12 + 2 * (tier - 2);
            var (barrowMap, barrowEntry, wightSpawns, barrowChest) = GenerateBarrow(worldSeed, wightCount);
            sites.Add(new Site
            {
                Id = "barrow",
                Kind = SiteKind.Barrow,
                Map = barrowMap,
                OverworldPos = barrow,
                EntryPos = barrowEntry,
                Spawns = [.. wightSpawns.Select(p => new MonsterSpawn(MonsterKind.Wight, p, wightHp))],
                ChestPos = barrowChest,
            });
        }

        var npcs = CastNpcs(overworld, ref placeRng, ref nameRng, settlement, shrine);

        // The story template selects and compiles against the villagers only: the
        // Unbinder (cast below) must never be picked for a world-story role.
        var storyRng = new Rng(SeedTree.Derive(worldSeed, "world-story"));
        var storyStorylets = WorldStories.CompileForWorld(ref storyRng,
            new StoryTemplateContext(npcs, settlementName, facts, sites, tier));

        // The Unbinder (D-034): a fresh guise every world, its own seed stream, placed
        // well away from the stead. Their tile stays plain ground: nothing on the map
        // marks them as anything but a camped wanderer.
        var unbinderRng = new Rng(SeedTree.Derive(worldSeed, "unbinder"));
        string guiseName = NameGen.Person(ref unbinderRng);
        string guiseRole = unbinderRng.Pick(UnbinderGuises.Roles);
        Pos unbinderPos = FindDistantSpot(overworld, ref unbinderRng, settlement, minDistance: 10);
        while (overworld[unbinderPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills))
            unbinderPos = FindDistantSpot(overworld, ref unbinderRng, settlement, minDistance: 10);
        CarvePathIfDisconnected(overworld, shrine, unbinderPos);
        var unbinder = new Npc
        {
            Id = "npc_unbinder",
            Name = guiseName,
            Role = guiseRole,
            Pos = unbinderPos,
            Kind = NpcKind.Unbinder,
        };
        npcs.Add(unbinder);

        // The hollow (D-037): tier 2+, on its own stream so every existing draw
        // stays put. A severed one keeps the ring's fire in every world deep
        // enough to hold one; the encounter is optional, like the barrow.
        Pos hollowPos = default;
        if (tier >= 2)
        {
            var hollowRng = new Rng(SeedTree.Derive(worldSeed, "hollow"));
            hollowPos = FindDistantSpot(overworld, ref hollowRng, settlement, minDistance: 14);
            while (hollowPos == camp || hollowPos == gate || hollowPos == barrow
                   || hollowPos.Manhattan(camp) < 5 || hollowPos.Manhattan(gate) < 5
                   || hollowPos.Manhattan(barrow) < 5 || npcs.Any(n => n.Pos == hollowPos))
                hollowPos = FindDistantSpot(overworld, ref hollowRng, settlement, minDistance: 14);
            overworld[hollowPos] = Terrain.HollowEntrance;
            CarvePathIfDisconnected(overworld, shrine, hollowPos);

            var (hollowMap, hollowEntry, severedPos, hollowChest) = GenerateHollow();
            sites.Add(new Site
            {
                Id = "hollow",
                Kind = SiteKind.Hollow,
                Map = hollowMap,
                OverworldPos = hollowPos,
                EntryPos = hollowEntry,
                Spawns = [new MonsterSpawn(MonsterKind.Severed, severedPos, 16 + 2 * (tier - 2))],
                ChestPos = hollowChest,
            });
            facts.Add("site", "hollow", $"{hollowPos.X},{hollowPos.Y}",
                "A ring of standing stones, older than the barrow's lintels. Someone keeps a fire there who never buys food, never ages, and never leaves.");
            facts.Add("bearer_myth", "tomb_of_the_undying", settlementName,
                "In the hills stands a tomb raised for one who, the old folk swear, did not die. Its door has never been opened, because it has never been needed.");
        }

        // The one at peace (D-038): tier 3+ worlds also hold a severed bearer who
        // chose the cutting and sits easy with it, camped at a small fire far from
        // stead and ring alike. Own stream, placed after every existing draw, so
        // pinned worlds keep their layouts and only gain a person.
        if (tier >= 3)
        {
            var calmRng = new Rng(SeedTree.Derive(worldSeed, "severed-calm"));
            string calmName = NameGen.Person(ref calmRng);
            Pos calmPos = FindDistantSpot(overworld, ref calmRng, settlement, minDistance: 12);
            while (overworld[calmPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || npcs.Any(n => n.Pos == calmPos) || calmPos.Manhattan(unbinderPos) < 5)
                calmPos = FindDistantSpot(overworld, ref calmRng, settlement, minDistance: 12);
            CarvePathIfDisconnected(overworld, shrine, calmPos);
            npcs.Add(new Npc
            {
                Id = "npc_severed_calm",
                Name = calmName,
                Role = "hermit",
                Pos = calmPos,
                Kind = NpcKind.Severed,
            });
        }

        // The last stair (D-039): tier 5+ worlds hold the arc's fixed final stage, a
        // bottle site inside a procedural world (arc sec 6, cycle 5). Own stream,
        // placed after every existing draw, so pinned deep worlds keep their layouts.
        // The stair exists whether or not this bearer is ready: the door at its foot
        // answers to the arc's flags, never to the map.
        if (tier >= 5)
        {
            var stairRng = new Rng(SeedTree.Derive(worldSeed, "threshold"));
            Pos stairPos = FindDistantSpot(overworld, ref stairRng, settlement, minDistance: 26);
            while (overworld[stairPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || stairPos.Manhattan(camp) < 5 || stairPos.Manhattan(gate) < 5
                   || stairPos.Manhattan(barrow) < 5 || stairPos.Manhattan(hollowPos) < 5
                   || npcs.Any(n => n.Pos == stairPos))
                stairPos = FindDistantSpot(overworld, ref stairRng, settlement, minDistance: 26);
            overworld[stairPos] = Terrain.ThresholdEntrance;
            CarvePathIfDisconnected(overworld, shrine, stairPos);

            var (stairMap, stairEntry, _) = GenerateThreshold();
            sites.Add(new Site
            {
                Id = "threshold",
                Kind = SiteKind.Threshold,
                Map = stairMap,
                OverworldPos = stairPos,
                EntryPos = stairEntry,
                Spawns = [],
                ChestPos = stairEntry,
                ChestLooted = true,
            });
            facts.Add("site", "threshold", $"{stairPos.X},{stairPos.Y}",
                "A stair going down into the hill where no door should be, cut clean and swept clean, though nothing lives near to sweep it.");
        }

        facts.Add("world_name", worldName, "");
        facts.Add("settlement", settlementName, $"{settlement.X},{settlement.Y}", "A small stead under the Aegis-shrine.");
        facts.Add("rest_point", "shrine", $"{shrine.X},{shrine.Y}", $"The shrine at {settlementName}. The Aegis anchors here.");
        facts.Add("site", "goblin_camp", $"{camp.X},{camp.Y}", "A cave the goblins have made their own.");
        facts.Add("site", "waygate", $"{gate.X},{gate.Y}", "An arch of black iron links, older than the stones around it.");
        if (tier >= 2)
            facts.Add("site", "barrow", $"{barrow.X},{barrow.Y}",
                "A long mound of turf over lintel stones, older than the waygate's iron. The dead under it do not lie easy.");
        facts.Add("grievance", "goblin_camp", settlementName, $"Goblins from the cave raid {settlementName}'s stores by night.");
        foreach (var npc in npcs)
            facts.Add("person", npc.Id, npc.Name, npc.Kind switch
            {
                NpcKind.Unbinder => $"{npc.Name}, a wandering {npc.Role}, camped away from the stead.",
                NpcKind.Severed => $"{npc.Name}, a hermit at a fire in the wilds. No one remembers them arriving.",
                _ => $"{npc.Name}, {npc.Role} of {settlementName}.",
            });
        facts.Add("wanderer", unbinder.Id, $"{unbinderPos.X},{unbinderPos.Y}",
            $"A {guiseRole} called {guiseName} is camped to the {Game.Compass(shrine, unbinderPos)}. Mends what pinches, they say, and asks no coin for it.");

        return new World
        {
            Seed = worldSeed,
            Tier = tier,
            Name = worldName,
            SettlementName = settlementName,
            Facts = facts,
            Overworld = overworld,
            ShrinePos = shrine,
            GatePos = gate,
            Sites = sites,
            Npcs = npcs,
            StoryStorylets = storyStorylets,
        };
    }

    /// <summary>
    /// Casts the settlement's people (D-031): role slots filled with generated names,
    /// standing on walkable tiles beside their houses. The column between shrine and
    /// settlement center stays clear so bump-to-talk never blocks the road.
    /// </summary>
    private static List<Npc> CastNpcs(GameMap overworld, ref Rng placeRng, ref Rng nameRng, Pos settlement, Pos shrine)
    {
        var candidates = new List<Pos>();
        for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
            {
                var p = settlement.Plus(dx, dy);
                if (!overworld.Walkable(p) || p == shrine || p.X == settlement.X) continue;
                bool byHouse = false;
                foreach (var (ddx, ddy) in Directions.All8)
                {
                    var q = p.Plus(ddx, ddy);
                    if (overworld.InBounds(q) && overworld[q] == Terrain.House) { byHouse = true; break; }
                }
                if (byHouse) candidates.Add(p);
            }

        var npcs = new List<Npc>();
        foreach (string role in (string[])["steadholder", "herbwife", "woodward"])
        {
            if (candidates.Count == 0) break;
            var pos = placeRng.Pick(candidates);
            candidates.Remove(pos);
            npcs.Add(new Npc
            {
                Id = $"npc_{role}",
                Name = NameGen.Person(ref nameRng),
                Role = role,
                Pos = pos,
            });
        }
        return npcs;
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

    private static (GameMap Map, Pos Entry, List<Pos> Goblins, Pos Chest) GenerateCamp(ulong worldSeed, int goblinCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-goblin-camp"));
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
        while (goblins.Count < goblinCount)
        {
            var p = rng.Pick(deep);
            if (p != entry && !goblins.Contains(p)) goblins.Add(p);
        }

        Pos chest = rng.Pick(deep);
        while (chest == entry || goblins.Contains(chest)) chest = rng.Pick(deep);

        return (map, entry, goblins, chest);
    }

    public const int BarrowW = 34;
    public const int BarrowH = 13;

    /// <summary>
    /// The barrow (D-033, tier 2+): a long central passage with burial chambers off it
    /// on alternating sides. Deliberately not the camp's drunkard-walk cave: sites
    /// should read differently at a glance. Wights keep to the chambers; the grave
    /// goods sit in the deepest one.
    /// </summary>
    private static (GameMap Map, Pos Entry, List<Pos> Wights, Pos Chest) GenerateBarrow(ulong worldSeed, int wightCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-barrow"));
        var map = new GameMap("barrow", BarrowW, BarrowH, Terrain.Wall);
        int mid = BarrowH / 2;

        var entry = new Pos(2, mid);
        for (int x = 2; x <= BarrowW - 3; x++) map[new Pos(x, mid)] = Terrain.Floor;
        map[entry] = Terrain.ExitLadder;

        var chambers = new List<List<Pos>>();
        bool up = rng.Chance(0.5);
        for (int cx = 7; cx <= BarrowW - 5; cx += 6)
        {
            int cy = up ? mid - 3 : mid + 3;
            var cells = new List<Pos>();
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var p = new Pos(cx + dx, cy + dy);
                    map[p] = Terrain.Floor;
                    cells.Add(p);
                }
            map[new Pos(cx, up ? mid - 1 : mid + 1)] = Terrain.Floor;
            chambers.Add(cells);
            up = !up;
        }

        var deepFirst = ((IEnumerable<List<Pos>>)chambers).Reverse().ToList();
        Pos chest = deepFirst[0][4]; // center cell of the deepest chamber

        var wights = new List<Pos>();
        int chamber = 0;
        while (wights.Count < wightCount)
        {
            var p = rng.Pick(deepFirst[chamber % deepFirst.Count]);
            if (p == chest || wights.Contains(p)) continue;
            wights.Add(p);
            chamber++;
        }

        return (map, entry, wights, chest);
    }

    public const int HollowW = 21;
    public const int HollowH = 11;

    /// <summary>
    /// The hollow (D-037): a broken ring of standing stones with its tenant at the
    /// center. Authored layout, no carve RNG: the arc's recurring encounter ground
    /// reads the same in every world, the way its keeper does.
    /// </summary>
    private static (GameMap Map, Pos Entry, Pos SeveredPos, Pos Chest) GenerateHollow()
    {
        var map = new GameMap("hollow", HollowW, HollowH, Terrain.Floor);
        for (int x = 0; x < HollowW; x++)
        {
            map[new Pos(x, 0)] = Terrain.Wall;
            map[new Pos(x, HollowH - 1)] = Terrain.Wall;
        }
        for (int y = 0; y < HollowH; y++)
        {
            map[new Pos(0, y)] = Terrain.Wall;
            map[new Pos(HollowW - 1, y)] = Terrain.Wall;
        }

        var center = new Pos(14, 5);
        foreach (var (dx, dy) in ((int, int)[])[(3, 0), (-3, 0), (0, 3), (0, -3), (2, 2), (-2, 2), (2, -2), (-2, -2)])
            map[center.Plus(dx, dy)] = Terrain.Wall;

        var entry = new Pos(2, 5);
        map[entry] = Terrain.ExitLadder;
        return (map, entry, center, new Pos(17, 7));
    }

    public const int ThresholdW = 30;
    public const int ThresholdH = 9;

    /// <summary>
    /// The last stair (D-039): a long corridor opening into the keeping chamber,
    /// the Hearth at its heart. Fully authored, no carve RNG, no spawns: the arc's
    /// final stage is the same room at the bottom of every world deep enough to
    /// reach it, because it is, in fiction, the same room.
    /// </summary>
    private static (GameMap Map, Pos Entry, Pos Hearth) GenerateThreshold()
    {
        var map = new GameMap("threshold", ThresholdW, ThresholdH, Terrain.Wall);
        int mid = ThresholdH / 2;

        for (int x = 2; x <= 20; x++) map[new Pos(x, mid)] = Terrain.Floor;
        for (int y = 2; y <= ThresholdH - 3; y++)
            for (int x = 21; x <= ThresholdW - 3; x++)
                map[new Pos(x, y)] = Terrain.Floor;

        var hearth = new Pos(26, mid);
        map[hearth] = Terrain.Hearth;
        var entry = new Pos(2, mid);
        map[entry] = Terrain.ExitLadder;
        return (map, entry, hearth);
    }
}

public static class Directions
{
    public static readonly (int dx, int dy)[] Cardinal = [(0, -1), (0, 1), (-1, 0), (1, 0)];
    public static readonly (int dx, int dy)[] All8 =
        [(0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)];
}
