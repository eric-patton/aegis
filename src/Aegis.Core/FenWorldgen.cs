namespace Aegis.Core;

public static partial class WorldGen
{
    public const int FensW = 52;
    public const int FensH = 22;
    public const int FenPansPerWorld = 3;

    private sealed record FenGeneration(
        GameMap Map,
        Pos RoadMouth,
        Pos HomeMouth,
        string HamletName);

    /// <summary>
    /// Builds generator 1's fourth country from named streams that are derived
    /// after every pre-1.0 world component. Existing maps, casts, stories, and
    /// rare-family selection therefore keep their established draws.
    /// </summary>
    private static FenGeneration GenerateFens(
        ulong worldSeed,
        int tier,
        GameMap road,
        Pos roadHome,
        string townName,
        List<Region> regions,
        List<Site> sites,
        List<Npc> npcs,
        FactGraph facts)
    {
        ulong fenSeed = SeedTree.Derive(worldSeed, "salt-fen");
        var nameRng = new Rng(SeedTree.Derive(fenSeed, "names"));
        string regionName = NameGen.Region(ref nameRng, [.. regions.Select(r => r.Name)]);
        string hamletName = NameGen.Settlement(ref nameRng);
        regions.Add(new Region { Id = "fens", Name = regionName });

        Pos roadMouth = FindFenRoadMouth(road, roadHome);
        road[roadMouth] = Terrain.FenMouth;
        CarvePathIfDisconnected(road, roadHome, roadMouth);

        var fens = GenerateFenMap(fenSeed);
        var home = new Pos(2, FensH / 2);
        fens[home] = Terrain.FenMouth;

        var anchors = new[]
        {
            new Pos(10, 5),
            new Pos(18, 16),
            new Pos(29, 5),
            new Pos(39, 16),
            new Pos(48, 6),
        };
        foreach (var anchor in anchors)
            CarveFenCauseway(fens, home, anchor);
        fens[home] = Terrain.FenMouth;

        var hamletMap = GenerateFenHamlet(hamletName);
        var hamletEntry = new Pos(1, hamletMap.Height / 2);
        var hamletPos = anchors[0];
        fens[hamletPos] = Terrain.HamletEntrance;
        var fenFolk = CastFenFolk(fenSeed, hamletName, hamletMap);
        npcs.AddRange(fenFolk);
        sites.Add(new Site
        {
            Id = "fen-hamlet",
            Kind = SiteKind.FenHamlet,
            Map = hamletMap,
            OverworldPos = hamletPos,
            EntryPos = hamletEntry,
            Area = Area.Fens,
            Spawns = [],
            ChestPos = hamletEntry,
            ChestLooted = true,
        });

        var saltwork = GenerateSaltworks();
        var saltPos = anchors[1];
        fens[saltPos] = Terrain.SaltworkEntrance;
        sites.Add(new Site
        {
            Id = "fen-saltworks",
            Kind = SiteKind.Saltworks,
            Map = saltwork.Map,
            OverworldPos = saltPos,
            EntryPos = saltwork.Entry,
            Area = Area.Fens,
            Spawns = [],
            ChestPos = saltwork.Entry,
            ChestLooted = true,
            SaltPans = saltwork.Pans,
        });

        int adderHp = 7 + tier / 2;
        var fenWilds = GenerateFenWilds(fenSeed);
        var wildsPos = anchors[2];
        fens[wildsPos] = Terrain.FenWildsEntrance;
        sites.Add(new Site
        {
            Id = "fen-wilds",
            Kind = SiteKind.FenWilds,
            Map = fenWilds.Map,
            OverworldPos = wildsPos,
            EntryPos = fenWilds.Entry,
            Area = Area.Fens,
            Spawns = [.. fenWilds.Adders.Select(p => new MonsterSpawn(MonsterKind.FenAdder, p, adderHp))],
            ChestPos = fenWilds.Entry,
            ChestLooted = true,
        });

        var fenWatch = GenerateFenWatch(fenSeed);
        var watchPos = anchors[3];
        fens[watchPos] = Terrain.FenWatchEntrance;
        sites.Add(new Site
        {
            Id = "fen-watch",
            Kind = SiteKind.FenWatch,
            Map = fenWatch.Map,
            OverworldPos = watchPos,
            EntryPos = fenWatch.Entry,
            Area = Area.Fens,
            Spawns =
            [
                .. fenWatch.Adders.Select(p => new MonsterSpawn(MonsterKind.FenAdder, p, adderHp + 1)),
                .. fenWatch.Warders.Select(p => new MonsterSpawn(MonsterKind.Warder, p, 9 + tier)),
            ],
            ChestPos = fenWatch.Chest,
            StonePos = fenWatch.Stone,
            CofferPos = fenWatch.Coffer,
        });

        var fenVault = GenerateFenVault(fenSeed);
        var vaultPos = anchors[4];
        fens[vaultPos] = Terrain.FenVaultEntrance;
        sites.Add(new Site
        {
            Id = "fen-vault",
            Kind = SiteKind.FenVault,
            Map = fenVault.Map,
            OverworldPos = vaultPos,
            EntryPos = fenVault.Entry,
            Area = Area.Fens,
            Spawns =
            [
                .. fenVault.Adders.Select(p => new MonsterSpawn(MonsterKind.FenAdder, p, adderHp + 2)),
                .. fenVault.Wights.Select(p => new MonsterSpawn(MonsterKind.Wight, p, 10 + tier)),
            ],
            ChestPos = fenVault.Chest,
            StonePos = fenVault.Stone,
        });

        facts.Add("region", "fens", regionName,
            $"The {regionName}: salt water, reed ground, and raised ways beyond {townName}'s east road, with {hamletName} keeping the compact's roofs.");
        facts.Add("site", "fen-hamlet", hamletName,
            $"{hamletName} stands roofed on a firm bank in the {regionName}, where the salters' compact keeps its measures and the carriers take freight west.");
        facts.Add("site", "fen-saltworks", regionName,
            $"Three workable pans stand on the raised salt ground of the {regionName}; still air or a salt wind will dry them, while rain and frost turn the work away.");
        facts.Add("site", "fen-wilds", regionName,
            $"A reed-bank beyond {hamletName} is known adder ground, hunted for meat and hide when the water lies low enough to read.");
        facts.Add("site", "fen-watch", regionName,
            $"An old bank-watch stands among the causeways, its kept ground tied to the compact's present pressure.");
        facts.Add("site", "fen-vault", regionName,
            $"A drowned counting-house keeps one end of the old salt road, and the compact still measures its losses by what remains there.");
        foreach (var person in fenFolk)
            facts.Add("person", person.Id, person.Name, $"{person.Name}, {person.Role} of {hamletName}.");

        return new FenGeneration(fens, roadMouth, home, hamletName);
    }

    private static Pos FindFenRoadMouth(GameMap road, Pos roadHome)
    {
        for (int x = RoadW - 5; x >= RoadW / 2; x--)
            for (int y = RoadH - 2; y >= 1; y--)
            {
                var p = new Pos(x, y);
                if (road[p] is Terrain.Grass or Terrain.Forest or Terrain.Hills)
                    return p;
            }
        return roadHome.Plus(1, 0);
    }

    private static GameMap GenerateFenMap(ulong fenSeed)
    {
        var map = new GameMap("fens", FensW, FensH, Terrain.Bog);
        for (int y = 1; y < FensH - 1; y++)
            for (int x = 1; x < FensW - 1; x++)
            {
                double n = ValueNoise(fenSeed, x * 0.14, y * 0.18);
                map[new Pos(x, y)] = n switch
                {
                    < 0.30 => Terrain.Water,
                    < 0.48 => Terrain.Bog,
                    < 0.72 => Terrain.Reed,
                    _ => Terrain.Grass,
                };
            }
        return map;
    }

    private static void CarveFenCauseway(GameMap map, Pos from, Pos to)
    {
        var p = from;
        while (p.X != to.X)
        {
            map[p] = Terrain.Causeway;
            p = p.Plus(Math.Sign(to.X - p.X), 0);
        }
        while (p.Y != to.Y)
        {
            map[p] = Terrain.Causeway;
            p = p.Plus(0, Math.Sign(to.Y - p.Y));
        }
        map[p] = Terrain.Causeway;
    }

    private static GameMap GenerateFenHamlet(string name)
    {
        var map = new GameMap($"fen-hamlet:{name}", 26, 14, Terrain.Wall);
        for (int y = 2; y < map.Height - 2; y++)
            for (int x = 1; x < map.Width - 1; x++)
                map[new Pos(x, y)] = Terrain.Floor;
        for (int x = 4; x < map.Width - 3; x += 6)
        {
            map[new Pos(x, 3)] = Terrain.House;
            map[new Pos(x + 1, 3)] = Terrain.House;
            map[new Pos(x, map.Height - 4)] = Terrain.House;
            map[new Pos(x + 1, map.Height - 4)] = Terrain.House;
        }
        map[new Pos(0, map.Height / 2)] = Terrain.ExitLadder;
        map[new Pos(1, map.Height / 2)] = Terrain.ExitLadder;
        return map;
    }

    private static List<Npc> CastFenFolk(ulong fenSeed, string hamletName, GameMap map)
    {
        var rng = new Rng(SeedTree.Derive(fenSeed, "hamlet-cast"));
        return
        [
            new Npc { Id = "npc_compact_keeper", Name = NameGen.Person(ref rng), Role = "keeper of the compact", Pos = new Pos(8, 5), Kind = NpcKind.Fenfolk, Area = Area.Fens, SiteId = "fen-hamlet" },
            new Npc { Id = "npc_pan_reeve", Name = NameGen.Person(ref rng), Role = "reeve of the pans", Pos = new Pos(14, 8), Kind = NpcKind.Fenfolk, Area = Area.Fens, SiteId = "fen-hamlet" },
            new Npc { Id = "npc_fen_carrier", Name = NameGen.Person(ref rng), Role = "compact carrier", Pos = new Pos(20, 5), Kind = NpcKind.Fenfolk, Area = Area.Fens, SiteId = "fen-hamlet" },
        ];
    }

    private sealed record SaltworkGeneration(GameMap Map, Pos Entry, List<Pos> Pans);

    private static SaltworkGeneration GenerateSaltworks()
    {
        var map = BoundedFloor("fen-saltworks", 25, 13);
        var entry = new Pos(1, 6);
        map[new Pos(0, 6)] = Terrain.ExitLadder;
        map[entry] = Terrain.ExitLadder;
        var pans = new List<Pos> { new(8, 4), new(13, 8), new(19, 4) };
        foreach (var pan in pans) map[pan] = Terrain.SaltPan;
        for (int x = 3; x < map.Width - 2; x++)
            if (map[new Pos(x, 6)] == Terrain.Floor) map[new Pos(x, 6)] = Terrain.Causeway;
        return new SaltworkGeneration(map, entry, pans);
    }

    private sealed record FenWildsGeneration(GameMap Map, Pos Entry, List<Pos> Adders);

    private static FenWildsGeneration GenerateFenWilds(ulong fenSeed)
    {
        var map = BoundedFloor("fen-wilds", 30, 18);
        var entry = new Pos(1, 9);
        map[new Pos(0, 9)] = Terrain.ExitLadder;
        map[entry] = Terrain.ExitLadder;
        for (int y = 3; y < map.Height - 3; y += 4)
            for (int x = 7; x < map.Width - 3; x += 7)
                map[new Pos(x, y)] = (x + y) % 2 == 0 ? Terrain.Water : Terrain.Reed;
        return new FenWildsGeneration(map, entry, [new Pos(12, 5), new Pos(20, 12), new Pos(26, 6)]);
    }

    private sealed record FenWatchGeneration(GameMap Map, Pos Entry, List<Pos> Adders, List<Pos> Warders, Pos Chest, Pos Stone, Pos Coffer);

    private static FenWatchGeneration GenerateFenWatch(ulong fenSeed)
    {
        var map = BoundedFloor("fen-watch", 28, 16);
        var entry = new Pos(1, 8);
        map[new Pos(0, 8)] = Terrain.ExitLadder;
        map[entry] = Terrain.ExitLadder;
        for (int y = 2; y < map.Height - 2; y++)
            map[new Pos(12, y)] = y is 5 or 10 ? Terrain.Floor : Terrain.Water;
        return new FenWatchGeneration(map, entry,
            [new Pos(9, 4), new Pos(18, 11)],
            [new Pos(20, 4), new Pos(23, 11)],
            new Pos(25, 8), new Pos(24, 3), new Pos(24, 12));
    }

    private sealed record FenVaultGeneration(GameMap Map, Pos Entry, List<Pos> Adders, List<Pos> Wights, Pos Chest, Pos Stone);

    private static FenVaultGeneration GenerateFenVault(ulong fenSeed)
    {
        var map = BoundedFloor("fen-vault", 30, 18);
        var entry = new Pos(1, 9);
        map[new Pos(0, 9)] = Terrain.ExitLadder;
        map[entry] = Terrain.ExitLadder;
        for (int x = 6; x < map.Width - 3; x += 6)
            for (int y = 2; y < map.Height - 2; y++)
                if (y != 9) map[new Pos(x, y)] = Terrain.Wall;
        map[new Pos(12, 4)] = Terrain.Water;
        map[new Pos(18, 13)] = Terrain.Water;
        return new FenVaultGeneration(map, entry,
            [new Pos(9, 9), new Pos(21, 9)],
            [new Pos(15, 6), new Pos(26, 9)],
            new Pos(27, 9), new Pos(27, 4));
    }

    private static GameMap BoundedFloor(string id, int width, int height)
    {
        var map = new GameMap(id, width, height, Terrain.Wall);
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                map[new Pos(x, y)] = Terrain.Floor;
        return map;
    }
}
