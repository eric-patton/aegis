namespace Aegis.Core;

public enum SiteKind { GoblinCamp, Barrow, Hollow, Threshold, Quarry, Hall, Ringfort, Songhall, Leaguer, Wilds }

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

    /// <summary>
    /// The graven stone (D-091): one old word set deep in the site's fabric,
    /// where the fabric is older than the stead. Null where none stands.
    /// </summary>
    public Pos? StonePos { get; set; }

    /// <summary>Whether the stone's word has been read (D-091): a stone gives its word once.</summary>
    public bool StoneRead { get; set; }

    /// <summary>Whether the veil-word has been said on this floor (D-091): pretenders drawn for what they are.</summary>
    public bool Unveiled { get; set; }
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

    /// <summary>
    /// What the wood sets out (D-052): forest spots worth gathering, placed in
    /// every world whether or not anyone can see them. Only a bearer taught the
    /// gleaning finds them; gathering removes the spot, and the far gate
    /// regenerates the rest along with everything else.
    /// </summary>
    public required List<Pos> Gleanings { get; init; }

    /// <summary>
    /// What the wood grows for the picking (D-074): forest spots holding herbs, placed
    /// in every world on their own stream. Unlike the gleanings, no lesson is needed to
    /// see or take them; foraging one removes the spot, and the far gate regrows them.
    /// </summary>
    public required List<Pos> Herbs { get; init; }

    /// <summary>
    /// Doors already robbed this world (D-086): a house gives its ration's worth
    /// once. Runtime state like a looted chest, rebuilt by replay, regenerated
    /// whole (and innocent) with everything else at the crossing.
    /// </summary>
    public List<Pos> PilferedHouses { get; } = [];

    /// <summary>Doors made right (D-086): sills that hold their restitution coin. A repaid house is closed both ways.</summary>
    public List<Pos> RepaidHouses { get; } = [];

    /// <summary>
    /// The terms this world was crossed into under (D-047): oaths sworn at the
    /// previous world's waygate. A generation input like the tier; they lapse at
    /// this world's far gate. Empty for a first world and for a plain crossing.
    /// </summary>
    public required IReadOnlyList<OathId> Oaths { get; init; }

    /// <summary>The summed weight of the standing terms: the visible Threat score (D-011).</summary>
    public int Burden => Oaths.Sum(o => OathCatalog.Def(o).Weight);

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

    /// <summary>The old quarry (D-040): tier 3+, where the graven men stand. Optional depth, like the barrow.</summary>
    public Site? QuarrySite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Quarry);

    /// <summary>The fallen hall (D-044): tier 4+, where the iron hounds run. Optional depth, like the quarry.</summary>
    public Site? HallSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Hall);

    /// <summary>The tier-5+ band's site (D-053): the old watch, and the bow's answer.</summary>
    public Site? RingfortSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Ringfort);
    public Site? LeaguerSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Leaguer);

    /// <summary>The wilds (D-070): tier 2+, where the game runs. The hunt's ground, not a fight; optional depth like the barrow.</summary>
    public Site? WildsSite => Sites.FirstOrDefault(s => s.Kind == SiteKind.Wilds);

    /// <summary>The stead's smith (D-041): every world, every tier. Sells the plain three, mends what use has dulled.</summary>
    public Npc Smith => Npcs.First(n => n.Kind == NpcKind.Smith);

    /// <summary>The stead's songhall (D-054): every world, every tier. The place where the third ledger is kept.</summary>
    public Site SonghallSite => Sites.First(s => s.Kind == SiteKind.Songhall);

    /// <summary>The songhall's keeper (D-054): stands at the hall door, reads the songs' weighing, takes the pledges.</summary>
    public Npc Skald => Npcs.First(n => n.Kind == NpcKind.Skald);

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
    /// which is what keeps old save journals replayable. <paramref name="prevStory"/>
    /// is the finished world's story id (D-040): a generation input like the tier,
    /// legitimate because it is itself a pure function of the seed lineage.
    /// <paramref name="oaths"/> is the terms sworn at the previous waygate (D-047):
    /// a generation input that is player choice, which stays deterministic because
    /// the choosing keys are journaled before this is ever called. With no oaths
    /// the draws are exactly what they always were, so pinned worlds survive.
    /// <paramref name="takenNames"/> is the names of the character's walked worlds
    /// (D-049): the world-name weave rerolls against them so the long song never
    /// repeats a verse. The list is itself journal-derived, so still deterministic.
    /// </summary>
    public static World Generate(ulong worldSeed, int tier = 1, string? prevStory = null, IReadOnlyList<OathId>? oaths = null, IReadOnlyCollection<string>? takenNames = null)
    {
        oaths ??= [];
        // The crowded dark (D-047): every den holds one more than the tier asks.
        int crowd = oaths.Contains(OathId.CrowdedDark) ? 1 : 0;
        var facts = new FactGraph();

        var nameRng = new Rng(SeedTree.Derive(worldSeed, "names"));
        string worldName = NameGen.World(ref nameRng, takenNames);
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

        int goblinCount = Math.Min(3 + (tier - 1), 6) + crowd;
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
            int wightCount = Math.Min(2 + (tier - 2), 5) + crowd;
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
            new StoryTemplateContext(npcs, settlementName, facts, sites, tier), prevStory);

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
        Pos stairPos = default;
        if (tier >= 5)
        {
            var stairRng = new Rng(SeedTree.Derive(worldSeed, "threshold"));
            stairPos = FindDistantSpot(overworld, ref stairRng, settlement, minDistance: 26);
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

        // The old quarry (D-040): tier 3+ worlds hold the deep band's site, where
        // the graven men stand. Own stream, placed after every existing draw
        // (including the stair's), so pinned worlds keep their layouts exactly.
        Pos quarryPos = default;
        if (tier >= 3)
        {
            var quarryRng = new Rng(SeedTree.Derive(worldSeed, "quarry"));
            quarryPos = FindDistantSpot(overworld, ref quarryRng, settlement, minDistance: 16);
            while (overworld[quarryPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || quarryPos.Manhattan(camp) < 5 || quarryPos.Manhattan(gate) < 5
                   || quarryPos.Manhattan(barrow) < 5 || quarryPos.Manhattan(hollowPos) < 5
                   || (tier >= 5 && quarryPos.Manhattan(stairPos) < 5)
                   || npcs.Any(n => n.Pos == quarryPos))
                quarryPos = FindDistantSpot(overworld, ref quarryRng, settlement, minDistance: 16);
            overworld[quarryPos] = Terrain.QuarryEntrance;
            CarvePathIfDisconnected(overworld, shrine, quarryPos);

            int gravenCount = Math.Min(3 + (tier - 3), 5) + crowd;
            int gravenHp = 18 + 2 * (tier - 3);
            var (quarryMap, quarryEntry, gravenSpawns, quarryChest) = GenerateQuarry(worldSeed, gravenCount);
            sites.Add(new Site
            {
                Id = "quarry",
                Kind = SiteKind.Quarry,
                Map = quarryMap,
                OverworldPos = quarryPos,
                EntryPos = quarryEntry,
                Spawns = [.. gravenSpawns.Select(p => new MonsterSpawn(MonsterKind.Graven, p, gravenHp))],
                ChestPos = quarryChest,
            });
            facts.Add("site", "quarry", $"{quarryPos.X},{quarryPos.Y}",
                "An old quarry, worked and abandoned before the stead's first stone was laid. The carvers left mid-stroke, and their figures still stand in the pit.");
        }

        // The smith (D-041): every stead has one, at every tier. Own stream, placed
        // after every existing draw, so pinned worlds keep their layouts and only
        // gain a person at the forge. Stands beside a house like the villagers, off
        // the shrine column, never on an occupied tile.
        var smithRng = new Rng(SeedTree.Derive(worldSeed, "smith"));
        string smithName = NameGen.Person(ref smithRng);
        var npcList = npcs;
        var smithSpots = HouseAdjacentCandidates(overworld, settlement, shrine)
            .Where(p => !npcList.Any(n => n.Pos == p))
            // Nobody gets walled in: the forge must not take any neighbor's last
            // open cardinal tile, and the smith must keep one of their own.
            .Where(p => npcList.All(n => HasOpenCardinalNeighbor(overworld, npcList, n.Pos, alsoOccupied: p))
                        && HasOpenCardinalNeighbor(overworld, npcList, p, alsoOccupied: p))
            .ToList();
        Pos smithPos;
        if (smithSpots.Count > 0)
        {
            smithPos = smithRng.Pick(smithSpots);
        }
        else
        {
            // Deterministic fallback: the first open tile ringing the settlement.
            smithPos = settlement;
            foreach (var (fdx, fdy) in Directions.All8)
            {
                var q = settlement.Plus(fdx, fdy);
                if (overworld.Walkable(q) && q != shrine && !npcs.Any(n => n.Pos == q)) { smithPos = q; break; }
            }
        }
        npcs.Add(new Npc
        {
            Id = "npc_smith",
            Name = smithName,
            Role = "smith",
            Pos = smithPos,
            Kind = NpcKind.Smith,
        });

        // The songhall (D-054): every stead keeps one, at every tier, because the
        // songs are kept where people live, not where they die. Own stream, placed
        // after every existing draw (and before the gleanings, whose forest check
        // must see its door tile), so pinned worlds keep their layouts and only
        // gain the hall and its keeper. It stands at the stead's edge: the one
        // site a bearer can walk to between errands.
        var songhallRng = new Rng(SeedTree.Derive(worldSeed, "songhall"));
        string skaldName = NameGen.Person(ref songhallRng);
        Pos songhallPos = settlement;
        for (int r = 3; r <= 9 && songhallPos == settlement; r++)
        {
            var ring = new List<Pos>();
            for (int y = settlement.Y - r; y <= settlement.Y + r; y++)
                for (int x = settlement.X - r; x <= settlement.X + r; x++)
                {
                    var p = new Pos(x, y);
                    if (p.Chebyshev(settlement) != r || !overworld.InBounds(p)) continue;
                    if (overworld[p] != Terrain.Grass || p.Chebyshev(shrine) < 2) continue;
                    if (npcs.Any(n => n.Pos == p)) continue;
                    ring.Add(p);
                }
            if (ring.Count > 0) songhallPos = songhallRng.Pick(ring);
        }
        overworld[songhallPos] = Terrain.SonghallEntrance;
        CarvePathIfDisconnected(overworld, shrine, songhallPos);

        var skaldSpots = new List<Pos>();
        foreach (var (sdx, sdy) in Directions.All8)
        {
            var q = songhallPos.Plus(sdx, sdy);
            if (overworld.Walkable(q) && q != shrine && !npcs.Any(n => n.Pos == q)) skaldSpots.Add(q);
        }
        npcs.Add(new Npc
        {
            Id = "npc_skald",
            Name = skaldName,
            Role = "skald",
            Pos = skaldSpots.Count > 0 ? songhallRng.Pick(skaldSpots) : songhallPos.Plus(0, 1),
            Kind = NpcKind.Skald,
        });

        var (songhallMap, songhallEntry) = GenerateSonghall();
        sites.Add(new Site
        {
            Id = "songhall",
            Kind = SiteKind.Songhall,
            Map = songhallMap,
            OverworldPos = songhallPos,
            EntryPos = songhallEntry,
            Spawns = [],
            ChestPos = songhallEntry,
            ChestLooted = true,
        });
        facts.Add("site", "songhall", $"{songhallPos.X},{songhallPos.Y}",
            "The stead's songhall: turf roof, one long hearth, and the year's songs cut into the east wall. Walkers' verses too, when the road sends any worth the cutting.");

        // The fallen hall (D-044): tier 4+ worlds hold the third band's site, where
        // the iron hounds run. Own stream, placed after every existing draw
        // (including the smith's), so pinned worlds keep their layouts exactly.
        Pos hallPos = default;
        if (tier >= 4)
        {
            var hallRng = new Rng(SeedTree.Derive(worldSeed, "hall"));
            hallPos = FindDistantSpot(overworld, ref hallRng, settlement, minDistance: 20);
            while (overworld[hallPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || hallPos.Manhattan(camp) < 5 || hallPos.Manhattan(gate) < 5
                   || hallPos.Manhattan(barrow) < 5 || hallPos.Manhattan(hollowPos) < 5
                   || hallPos.Manhattan(quarryPos) < 5
                   || (tier >= 5 && hallPos.Manhattan(stairPos) < 5)
                   || npcs.Any(n => n.Pos == hallPos))
                hallPos = FindDistantSpot(overworld, ref hallRng, settlement, minDistance: 20);
            overworld[hallPos] = Terrain.HallEntrance;
            CarvePathIfDisconnected(overworld, shrine, hallPos);

            int houndCount = Math.Min(5 + (tier - 4), 8) + crowd;
            int houndHp = 10 + 2 * (tier - 4);
            var (hallMap, hallEntry, houndSpawns, hallChest) = GenerateHall(worldSeed, houndCount);
            sites.Add(new Site
            {
                Id = "hall",
                Kind = SiteKind.Hall,
                Map = hallMap,
                OverworldPos = hallPos,
                EntryPos = hallEntry,
                Spawns = [.. houndSpawns.Select(p => new MonsterSpawn(MonsterKind.Hound, p, houndHp))],
                ChestPos = hallChest,
            });
            facts.Add("site", "hall", $"{hallPos.X},{hallPos.Y}",
                "A roofless hall of grey stone, older than any stead-tale. At dusk, things lope from its doorway that were never whelped.");
        }

        // The ringfort (D-053): tier 5+ worlds hold the fourth band's site, where
        // the old watch stands. Own streams, placed after every existing draw
        // (and before the gleanings, whose forest check must see its gate tile),
        // so pinned tier-1-4 worlds keep their layouts exactly.
        Pos fortPos = default;
        if (tier >= 5)
        {
            var fortRng = new Rng(SeedTree.Derive(worldSeed, "ringfort"));
            fortPos = FindDistantSpot(overworld, ref fortRng, settlement, minDistance: 20);
            while (overworld[fortPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || fortPos.Manhattan(camp) < 5 || fortPos.Manhattan(gate) < 5
                   || fortPos.Manhattan(barrow) < 5 || fortPos.Manhattan(hollowPos) < 5
                   || fortPos.Manhattan(quarryPos) < 5 || fortPos.Manhattan(hallPos) < 5
                   || fortPos.Manhattan(stairPos) < 5
                   || npcs.Any(n => n.Pos == fortPos))
                fortPos = FindDistantSpot(overworld, ref fortRng, settlement, minDistance: 20);
            overworld[fortPos] = Terrain.RingfortEntrance;
            CarvePathIfDisconnected(overworld, shrine, fortPos);

            int carlCount = Math.Min(4 + (tier - 5), 6) + crowd;
            int carlHp = 14 + 2 * (tier - 5);
            int boarHp = 20 + 2 * (tier - 5);
            // The sword-thegn (D-058): the endless country's forts, past the
            // arc's own march, post a veteran of the watch who fights by reading
            // the bearer. Tier 7+ only, so tier-5 and -6 forts are untouched.
            int thegnCount = tier >= 7 ? Math.Min(1 + (tier - 7) / 2, 3) : 0;
            // Tanky on purpose: a veteran you cannot burst down in one blow is a
            // veteran the heave stays tempting against, and the temptation is the
            // trap. It outlasts a single heave, so the reader gets its turn.
            int thegnHp = 28 + 2 * (tier - 7);
            var (fortMap, fortEntry, carlSpawns, boarSpawns, thegnSpawns, fortChest) = GenerateRingfort(worldSeed, carlCount, thegnCount);
            sites.Add(new Site
            {
                Id = "ringfort",
                Kind = SiteKind.Ringfort,
                Map = fortMap,
                OverworldPos = fortPos,
                EntryPos = fortEntry,
                Spawns = [.. carlSpawns.Select(p => new MonsterSpawn(MonsterKind.Carl, p, carlHp)),
                          .. boarSpawns.Select(p => new MonsterSpawn(MonsterKind.Boar, p, boarHp)),
                          .. thegnSpawns.Select(p => new MonsterSpawn(MonsterKind.Thegn, p, thegnHp))],
                ChestPos = fortChest,
            });
            facts.Add("site", "ringfort", $"{fortPos.X},{fortPos.Y}",
                "A ring-walled fort older than the war anyone can name. The watch on its walls was never stood down, and the beasts they kept have not gone tame.");
            if (thegnCount > 0)
                facts.Add("site_note", "sword_thegn", $"{fortPos.X},{fortPos.Y}",
                    "Among the watch stands one who keeps to the old drill: never the first blow, only the answer. It has kept it long past anyone left to answer for.");
        }

        // The fen-leaguer (D-057): tier 6+ worlds hold the fifth band's site,
        // the siege that outlived its object. Own streams, placed after every
        // existing draw and before the gleanings (whose forest check must see
        // its bank tile), so pinned tier-1-5 worlds keep their layouts exactly.
        if (tier >= 6)
        {
            var leaguerRng = new Rng(SeedTree.Derive(worldSeed, "leaguer"));
            Pos merePos = FindDistantSpot(overworld, ref leaguerRng, settlement, minDistance: 20);
            while (overworld[merePos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || merePos.Manhattan(camp) < 5 || merePos.Manhattan(gate) < 5
                   || merePos.Manhattan(barrow) < 5 || merePos.Manhattan(hollowPos) < 5
                   || merePos.Manhattan(quarryPos) < 5 || merePos.Manhattan(hallPos) < 5
                   || merePos.Manhattan(fortPos) < 5 || merePos.Manhattan(stairPos) < 5
                   || npcs.Any(n => n.Pos == merePos))
                merePos = FindDistantSpot(overworld, ref leaguerRng, settlement, minDistance: 20);
            overworld[merePos] = Terrain.LeaguerEntrance;
            CarvePathIfDisconnected(overworld, shrine, merePos);

            int warderCount = Math.Min(5 + (tier - 6), 8) + crowd;
            int warderHp = 12 + 2 * (tier - 6);
            var (mereMap, mereEntry, warderSpawns, mereChest) = GenerateLeaguer(worldSeed, warderCount);
            sites.Add(new Site
            {
                Id = "leaguer",
                Kind = SiteKind.Leaguer,
                Map = mereMap,
                OverworldPos = merePos,
                EntryPos = mereEntry,
                Spawns = [.. warderSpawns.Select(p => new MonsterSpawn(MonsterKind.Warder, p, warderHp))],
                ChestPos = mereChest,
            });
            facts.Add("site", "leaguer", $"{merePos.X},{merePos.Y}",
                "A broad black mere ringed with old siege-works. The watch on the banks has never lifted its leaguer, and stones still fall on the causeway.");
        }

        // The wilds (D-070): tier 2+ worlds hold a game-trail where the deer run.
        // Own stream, placed after every existing draw, so pinned worlds keep their
        // layouts and only gain a glade to hunt. Nothing in it fights, so it needs no
        // distance from the deep sites, only from the stead and the two nearest mouths;
        // the terrain check excludes every stamped entrance for free.
        if (tier >= 2)
        {
            var wildsRng = new Rng(SeedTree.Derive(worldSeed, "wilds"));
            var wildsPos = FindDistantSpot(overworld, ref wildsRng, settlement, minDistance: 12);
            while (overworld[wildsPos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills)
                   || wildsPos.Manhattan(camp) < 6 || wildsPos.Manhattan(gate) < 6
                   || npcs.Any(n => n.Pos == wildsPos))
                wildsPos = FindDistantSpot(overworld, ref wildsRng, settlement, minDistance: 12);
            overworld[wildsPos] = Terrain.WildsEntrance;
            CarvePathIfDisconnected(overworld, shrine, wildsPos);

            int hartCount = Math.Min(3 + (tier - 2) / 2, 6);
            const int hartHp = 6;
            var (wildsMap, wildsEntry, hartSpawns) = GenerateWilds(worldSeed, hartCount);
            sites.Add(new Site
            {
                Id = "wilds",
                Kind = SiteKind.Wilds,
                Map = wildsMap,
                OverworldPos = wildsPos,
                EntryPos = wildsEntry,
                Spawns = [.. hartSpawns.Select(p => new MonsterSpawn(MonsterKind.Hart, p, hartHp))],
                ChestPos = wildsEntry,   // no chest in the wilds: the yield is the game itself.
                ChestLooted = true,
            });
            facts.Add("site", "wilds", $"{wildsPos.X},{wildsPos.Y}",
                "A break in the tree-line where the deer come down to graze. The stead calls it good hunting, and dangerous walking after dark.");
        }

        // The gleanings (D-052): what the wood sets out for taught eyes. Placed in
        // every world on their own stream, after every other draw, so pinned worlds
        // keep their layouts; only the gleaning lesson makes them visible, so
        // worldgen never reads the character. Forest-only, spread out, and clear
        // of anyone's stand: the entrances have already overwritten their tiles,
        // so a Terrain.Forest check excludes every special place for free.
        var gleanRng = new Rng(SeedTree.Derive(worldSeed, "gleanings"));
        var gleanings = new List<Pos>();
        for (int attempt = 0; attempt < 400 && gleanings.Count < 4; attempt++)
        {
            var p = new Pos(gleanRng.Range(2, OverworldW - 2), gleanRng.Range(2, OverworldH - 2));
            if (overworld[p] != Terrain.Forest || p.Manhattan(settlement) < 6
                || npcs.Any(n => n.Pos == p) || gleanings.Any(g => g.Manhattan(p) < 8))
                continue;
            gleanings.Add(p);
        }

        // The herbs (D-074): what the wood grows for the picking. Own stream, drawn
        // after the gleanings (so pinned worlds and the gleanings' own layout are
        // untouched), forest-only and spread out like them, and clear of a gleaning
        // spot so no tile is both. No lesson gates them; anyone can stoop and pick.
        var herbRng = new Rng(SeedTree.Derive(worldSeed, "herbs"));
        var herbs = new List<Pos>();
        for (int attempt = 0; attempt < 400 && herbs.Count < 4; attempt++)
        {
            var p = new Pos(herbRng.Range(2, OverworldW - 2), herbRng.Range(2, OverworldH - 2));
            if (overworld[p] != Terrain.Forest || p.Manhattan(settlement) < 6
                || npcs.Any(n => n.Pos == p) || gleanings.Contains(p)
                || herbs.Any(h => h.Manhattan(p) < 8))
                continue;
            herbs.Add(p);
        }

        // The graven stones (D-091): one old word set deep in each fighting
        // site whose fabric predates the stead (the camp squats in a cave that
        // was something else first). Own stream per site, drawn after every
        // existing draw, so pinned worlds keep their layouts and only gain a
        // stone. Worldgen never reads the character: the stone stands for
        // everyone; which word it yields is decided at the reading.
        foreach (var site in sites)
        {
            if (site.Kind is not (SiteKind.GoblinCamp or SiteKind.Barrow or SiteKind.Quarry
                or SiteKind.Hall or SiteKind.Ringfort or SiteKind.Leaguer)) continue;
            var stoneRng = new Rng(SeedTree.Derive(worldSeed, "graven-stone-" + site.Id));
            var floor = new List<Pos>();
            for (int y = 0; y < site.Map.Height; y++)
                for (int x = 0; x < site.Map.Width; x++)
                {
                    var p = new Pos(x, y);
                    if (site.Map[p] != Terrain.Floor || p == site.ChestPos) continue;
                    if (site.Spawns.Any(s => s.Pos == p)) continue;
                    floor.Add(p);
                }
            if (floor.Count == 0) continue;
            int deepest = floor.Max(p => p.Manhattan(site.EntryPos));
            var deep = floor.Where(p => p.Manhattan(site.EntryPos) >= Math.Max(1, deepest - 3)).ToList();
            site.StonePos = stoneRng.Pick(deep);
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
            Gleanings = gleanings,
            Herbs = herbs,
            Oaths = oaths,
        };
    }

    /// <summary>
    /// Casts the settlement's people (D-031): role slots filled with generated names,
    /// standing on walkable tiles beside their houses. The column between shrine and
    /// settlement center stays clear so bump-to-talk never blocks the road.
    /// </summary>
    private static List<Npc> CastNpcs(GameMap overworld, ref Rng placeRng, ref Rng nameRng, Pos settlement, Pos shrine)
    {
        var candidates = HouseAdjacentCandidates(overworld, settlement, shrine);
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

    /// <summary>Whether a tile keeps at least one walkable cardinal neighbor free of people (and of one further occupied tile).</summary>
    private static bool HasOpenCardinalNeighbor(GameMap map, List<Npc> npcs, Pos pos, Pos alsoOccupied)
    {
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var q = pos.Plus(dx, dy);
            if (map.Walkable(q) && q != alsoOccupied && !npcs.Any(n => n.Pos == q)) return true;
        }
        return false;
    }

    /// <summary>Walkable tiles beside a house, off the shrine column, where the stead's people stand.</summary>
    private static List<Pos> HouseAdjacentCandidates(GameMap overworld, Pos settlement, Pos shrine)
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
        return candidates;
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

    public const int SonghallW = 26;
    public const int SonghallH = 9;

    /// <summary>The floor tile under the east wall's verses (D-054): where the songs are read.</summary>
    public static readonly Pos SonghallVersePos = new(24, 4);

    /// <summary>
    /// The songhall (D-054): one long room, fully authored, no carve RNG and no
    /// spawns, because it is the same hall in every stead the way the stead's
    /// life is the same life: door west, plinth by the door, the long hearth at
    /// the middle, and the east wall scored with verses. What varies between
    /// worlds is not the room but what the bearer's patronage has put in it.
    /// </summary>
    private static (GameMap Map, Pos Entry) GenerateSonghall()
    {
        var map = new GameMap("songhall", SonghallW, SonghallH, Terrain.Wall);
        for (int y = 1; y <= SonghallH - 2; y++)
            for (int x = 1; x <= SonghallW - 2; x++)
                map[new Pos(x, y)] = Terrain.Floor;

        map[new Pos(13, 4)] = Terrain.Hearth;
        map[new Pos(5, 2)] = Terrain.Plinth;
        var entry = new Pos(2, 4);
        map[entry] = Terrain.ExitLadder;
        return (map, entry);
    }

    public const int QuarryW = 32;
    public const int QuarryH = 15;

    /// <summary>
    /// The old quarry (D-040): one open pit, deliberately unlike the camp's warren
    /// and the barrow's passage, because its fight is fought in the open: the graven
    /// men throw, and the scattered pillars are the only cover. A pillar is only
    /// placed where all eight neighbors are open floor, so cover can never touch
    /// other cover or the pit wall, and reachability holds by construction.
    /// </summary>
    private static (GameMap Map, Pos Entry, List<Pos> Graven, Pos Chest) GenerateQuarry(ulong worldSeed, int gravenCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-quarry"));
        var map = new GameMap("quarry", QuarryW, QuarryH, Terrain.Floor);
        for (int x = 0; x < QuarryW; x++)
        {
            map[new Pos(x, 0)] = Terrain.Wall;
            map[new Pos(x, QuarryH - 1)] = Terrain.Wall;
        }
        for (int y = 0; y < QuarryH; y++)
        {
            map[new Pos(0, y)] = Terrain.Wall;
            map[new Pos(QuarryW - 1, y)] = Terrain.Wall;
        }

        var entry = new Pos(2, QuarryH / 2);

        int pillars = 0;
        for (int attempt = 0; attempt < 200 && pillars < 14; attempt++)
        {
            var p = new Pos(rng.Range(4, QuarryW - 3), rng.Range(2, QuarryH - 2));
            bool clear = p.Manhattan(entry) > 3;
            for (int dy = -1; dy <= 1 && clear; dy++)
                for (int dx = -1; dx <= 1 && clear; dx++)
                    if (map[p.Plus(dx, dy)] != Terrain.Floor) clear = false;
            if (!clear) continue;
            map[p] = Terrain.Wall;
            pillars++;
        }

        var open = new List<Pos>();
        for (int y = 1; y < QuarryH - 1; y++)
            for (int x = 1; x < QuarryW - 1; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == Terrain.Floor && p.Manhattan(entry) > 12) open.Add(p);
            }

        var graven = new List<Pos>();
        int guard = 4000;
        while (graven.Count < gravenCount)
        {
            var p = rng.Pick(open);
            bool spaced = guard-- <= 0 || graven.All(q => q.Manhattan(p) >= 4);
            if (!graven.Contains(p) && spaced) graven.Add(p);
        }

        Pos chest = rng.Pick(open);
        while (graven.Contains(chest) || chest.X < QuarryW - 8) chest = rng.Pick(open);

        map[entry] = Terrain.ExitLadder;
        return (map, entry, graven, chest);
    }

    public const int WildsW = 30;
    public const int WildsH = 16;

    /// <summary>
    /// The wilds (D-070): an open glade walled by treeline, unlike every other site
    /// because nothing in it fights. A few thicket-clumps give cover to corner a hart
    /// against (the quarry's all-eight-neighbours-open rule, so cover never seals the
    /// glade or a run off), and a handful of gaps cut in the far treeline are the runs
    /// the game bolts through: a hart that reaches a gap is gone. The bearer enters at
    /// the near edge; the game grazes deep, so the hunt is a stalk to bow-range and a
    /// shot, or a herding into a corner, never a footrace (a hart runs at the bearer's
    /// own speed, so feet alone never close on one).
    /// </summary>
    private static (GameMap Map, Pos Entry, List<Pos> Harts) GenerateWilds(ulong worldSeed, int hartCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-wilds"));
        var map = new GameMap("wilds", WildsW, WildsH, Terrain.Floor);
        for (int x = 0; x < WildsW; x++)
        {
            map[new Pos(x, 0)] = Terrain.Wall;
            map[new Pos(x, WildsH - 1)] = Terrain.Wall;
        }
        for (int y = 0; y < WildsH; y++)
        {
            map[new Pos(0, y)] = Terrain.Wall;
            map[new Pos(WildsW - 1, y)] = Terrain.Wall;
        }

        var entry = new Pos(2, WildsH / 2);

        // The runs: gaps cut in the far treeline the game bolts through. Kept to the
        // far half and off the entry wall, so a hart cannot break out beside the bearer
        // the moment it wakes. A hart on any walkable border cell is one that ran.
        int gaps = 0;
        for (int attempt = 0; attempt < 200 && gaps < 4; attempt++)
        {
            int side = rng.Range(0, 2);
            var g = side switch
            {
                0 => new Pos(rng.Range(WildsW / 2, WildsW - 2), 0),
                1 => new Pos(rng.Range(WildsW / 2, WildsW - 2), WildsH - 1),
                _ => new Pos(WildsW - 1, rng.Range(2, WildsH - 3)),
            };
            if (map[g] != Terrain.Wall) continue;
            map[g] = Terrain.Floor;
            gaps++;
        }

        // Thickets: cover to corner a hart against, under the quarry's placement rule
        // (all eight neighbours open floor), so a clump can never seal the glade off.
        int thickets = 0;
        for (int attempt = 0; attempt < 200 && thickets < 8; attempt++)
        {
            var p = new Pos(rng.Range(4, WildsW - 3), rng.Range(2, WildsH - 2));
            bool clear = p.Manhattan(entry) > 3;
            for (int dy = -1; dy <= 1 && clear; dy++)
                for (int dx = -1; dx <= 1 && clear; dx++)
                    if (map[p.Plus(dx, dy)] != Terrain.Floor) clear = false;
            if (!clear) continue;
            map[p] = Terrain.Wall;
            thickets++;
        }

        // The game grazes deep, well off the near edge and spaced from each other, and
        // never on a border cell (that would be a hart already gone before turn one).
        var open = new List<Pos>();
        for (int y = 2; y < WildsH - 2; y++)
            for (int x = 2; x < WildsW - 2; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == Terrain.Floor && p.Manhattan(entry) > 8) open.Add(p);
            }

        var harts = new List<Pos>();
        int guard = 4000;
        while (harts.Count < hartCount && open.Count > 0)
        {
            var p = rng.Pick(open);
            bool spaced = guard-- <= 0 || harts.All(q => q.Manhattan(p) >= 3);
            if (!harts.Contains(p) && spaced) harts.Add(p);
        }

        map[entry] = Terrain.ExitLadder;
        return (map, entry, harts);
    }

    public const int HallW = 34;
    public const int HallH = 17;

    /// <summary>
    /// The fallen hall (D-044): a narrow porch opening into one great roofless
    /// room, with two side chambers behind door-slots a single body wide. Unlike
    /// the camp's warren, the barrow's passage, and the quarry's open pit, because
    /// its fight is about ground: the pack flanks anything caught in the open
    /// room, and the porch and door-slots are where a bearer denies them that.
    /// Roof-fall rubble follows the quarry's placement rule (all eight neighbors
    /// open floor), so it can never disconnect the room.
    /// </summary>
    private static (GameMap Map, Pos Entry, List<Pos> Hounds, Pos Chest) GenerateHall(ulong worldSeed, int houndCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-hall"));
        var map = new GameMap("hall", HallW, HallH, Terrain.Wall);
        int mid = HallH / 2;

        // The porch: the first door the pack cannot come through two abreast.
        var entry = new Pos(2, mid);
        for (int x = 2; x <= 5; x++) map[new Pos(x, mid)] = Terrain.Floor;

        // The great room.
        for (int y = 2; y <= HallH - 3; y++)
            for (int x = 6; x <= 24; x++)
                map[new Pos(x, y)] = Terrain.Floor;

        // Two side chambers east, each behind a door-slot one body wide.
        foreach (int cy in (int[])[4, HallH - 5])
        {
            for (int y = cy - 2; y <= cy + 2; y++)
                for (int x = 27; x <= 31; x++)
                    map[new Pos(x, y)] = Terrain.Floor;
            map[new Pos(25, cy)] = Terrain.Floor;
            map[new Pos(26, cy)] = Terrain.Floor;
        }

        // The coffer keeps to one chamber; which one is the seed's business.
        Pos chest = rng.Chance(0.5) ? new Pos(29, 4) : new Pos(29, HallH - 5);

        // Roof-fall in the great room: scattered cover, never touching cover.
        int rubble = 0;
        for (int attempt = 0; attempt < 200 && rubble < 6; attempt++)
        {
            var p = new Pos(rng.Range(8, 24), rng.Range(3, HallH - 3));
            bool clear = true;
            for (int dy = -1; dy <= 1 && clear; dy++)
                for (int dx = -1; dx <= 1 && clear; dx++)
                    if (map[p.Plus(dx, dy)] != Terrain.Floor) clear = false;
            if (!clear) continue;
            map[p] = Terrain.Wall;
            rubble++;
        }

        var open = new List<Pos>();
        for (int y = 1; y < HallH - 1; y++)
            for (int x = 1; x < HallW - 1; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == Terrain.Floor && p.Manhattan(entry) > 14 && p != chest) open.Add(p);
            }

        var hounds = new List<Pos>();
        int guard = 4000;
        while (hounds.Count < houndCount)
        {
            var p = rng.Pick(open);
            bool spaced = guard-- <= 0 || hounds.All(q => q.Manhattan(p) >= 2);
            if (!hounds.Contains(p) && spaced) hounds.Add(p);
        }

        map[entry] = Terrain.ExitLadder;
        return (map, entry, hounds, chest);
    }

    public const int RingfortW = 35;
    public const int RingfortH = 19;

    /// <summary>
    /// The ringfort (D-053): two ring-walls and the lanes between them. The
    /// outer wall is the map's own border; the inner rampart stands whole but
    /// for one gate, never on the entry's side, so reaching the ward means
    /// walking the long straight lanes of the courtyard: the same clean lines
    /// a bowman wants are the lanes the war-boars run. The carls keep the ward;
    /// its rubble follows the quarry's rule (all eight neighbors open floor),
    /// so cover can never disconnect the ground it breaks.
    /// </summary>
    private static (GameMap Map, Pos Entry, List<Pos> Carls, List<Pos> Boars, List<Pos> Thegns, Pos Chest)
        GenerateRingfort(ulong worldSeed, int carlCount, int thegnCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-ringfort"));
        var map = new GameMap("ringfort", RingfortW, RingfortH, Terrain.Floor);
        int mid = RingfortH / 2;

        for (int x = 0; x < RingfortW; x++)
        {
            map[new Pos(x, 0)] = Terrain.Wall;
            map[new Pos(x, RingfortH - 1)] = Terrain.Wall;
        }
        for (int y = 0; y < RingfortH; y++)
        {
            map[new Pos(0, y)] = Terrain.Wall;
            map[new Pos(RingfortW - 1, y)] = Terrain.Wall;
        }

        var entry = new Pos(2, mid);

        // The inner rampart, whole but for its one gate.
        for (int x = 8; x <= 26; x++)
        {
            map[new Pos(x, 4)] = Terrain.Wall;
            map[new Pos(x, 14)] = Terrain.Wall;
        }
        for (int y = 4; y <= 14; y++)
        {
            map[new Pos(8, y)] = Terrain.Wall;
            map[new Pos(26, y)] = Terrain.Wall;
        }
        Pos innerGate = rng.Range(0, 3) switch
        {
            0 => new Pos(26, mid),
            1 => new Pos(17, 4),
            _ => new Pos(17, 14),
        };
        map[innerGate] = Terrain.Floor;

        // Rubble in the ward: scattered cover, never touching cover.
        int rubble = 0;
        for (int attempt = 0; attempt < 200 && rubble < 4; attempt++)
        {
            var p = new Pos(rng.Range(10, 25), rng.Range(6, 13));
            bool clear = true;
            for (int dy = -1; dy <= 1 && clear; dy++)
                for (int dx = -1; dx <= 1 && clear; dx++)
                    if (map[p.Plus(dx, dy)] != Terrain.Floor) clear = false;
            if (!clear) continue;
            map[p] = Terrain.Wall;
            rubble++;
        }

        var ward = new List<Pos>();
        for (int y = 5; y <= 13; y++)
            for (int x = 9; x <= 25; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == Terrain.Floor) ward.Add(p);
            }

        Pos chest = rng.Pick(ward);
        while (chest.Chebyshev(innerGate) < 6) chest = rng.Pick(ward);

        var carls = new List<Pos>();
        int guard = 4000;
        while (carls.Count < carlCount)
        {
            var p = rng.Pick(ward);
            bool spaced = guard-- <= 0 || carls.All(q => q.Manhattan(p) >= 3);
            if (p != chest && !carls.Contains(p) && spaced) carls.Add(p);
        }

        // The boars have the run of the courtyard lanes, far from the gate.
        var courtyard = new List<Pos>();
        for (int y = 1; y < RingfortH - 1; y++)
            for (int x = 1; x < RingfortW - 1; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == Terrain.Floor && (x < 8 || x > 26 || y < 4 || y > 14)
                    && p.Manhattan(entry) > 10)
                    courtyard.Add(p);
            }
        var boars = new List<Pos>();
        guard = 4000;
        while (boars.Count < 2)
        {
            var p = rng.Pick(courtyard);
            bool spaced = guard-- <= 0 || boars.All(q => q.Manhattan(p) >= 6);
            if (!boars.Contains(p) && spaced) boars.Add(p);
        }

        // The veteran of the watch (D-058): the deepest forts post a sword-thegn
        // in the ward among the carls. Its own stream ("site-thegn"), drawn only
        // when a fort holds one, so the carls, boars, chest, and rubble a
        // shallower fort drew stand byte-identical: no tier-5 or -6 layout moves.
        // It keeps the ward, spaced off the carls and clear of the inner gate.
        var thegns = new List<Pos>();
        if (thegnCount > 0)
        {
            var thegnRng = new Rng(SeedTree.Derive(worldSeed, "site-thegn"));
            guard = 4000;
            while (thegns.Count < thegnCount)
            {
                var p = thegnRng.Pick(ward);
                bool spaced = guard-- <= 0
                    || (carls.All(q => q.Manhattan(p) >= 3) && thegns.All(q => q.Manhattan(p) >= 4));
                if (p != chest && p.Chebyshev(innerGate) >= 2 && spaced
                    && !carls.Contains(p) && !thegns.Contains(p)) thegns.Add(p);
            }
        }

        map[entry] = Terrain.ExitLadder;
        return (map, entry, carls, boars, thegns, chest);
    }

    public const int LeaguerW = 33;
    public const int LeaguerH = 21;

    // The holm's bounds, published so the storylet on its crown can read them.
    public const int HolmMinX = 14, HolmMaxX = 18, HolmMinY = 8, HolmMaxY = 12;

    /// <summary>
    /// The fen-leaguer (D-057): a broad black mere inside a ring of siege-works,
    /// one causeway out to the bare holm the works were raised against. No other
    /// site holds water, so the read is instant: the mere stops feet and not
    /// eyes, the banks are the warders' ground, and the causeway is the one
    /// road to the cist. Mounds on the works follow the quarry's rule (all
    /// eight neighbors open floor), so cover never disconnects the ring; the
    /// ring, the causeway, and the holm connect by construction.
    /// </summary>
    private static (GameMap Map, Pos Entry, List<Pos> Warders, Pos Chest)
        GenerateLeaguer(ulong worldSeed, int warderCount)
    {
        var rng = new Rng(SeedTree.Derive(worldSeed, "site-leaguer"));
        var map = new GameMap("leaguer", LeaguerW, LeaguerH, Terrain.Floor);
        int midY = LeaguerH / 2;

        for (int x = 0; x < LeaguerW; x++)
        {
            map[new Pos(x, 0)] = Terrain.Wall;
            map[new Pos(x, LeaguerH - 1)] = Terrain.Wall;
        }
        for (int y = 0; y < LeaguerH; y++)
        {
            map[new Pos(0, y)] = Terrain.Wall;
            map[new Pos(LeaguerW - 1, y)] = Terrain.Wall;
        }

        // The mere, leaving three strides of works inside the outer bank.
        for (int y = 4; y <= LeaguerH - 5; y++)
            for (int x = 4; x <= LeaguerW - 5; x++)
                map[new Pos(x, y)] = Terrain.Water;

        // The holm, and the one causeway out to it from the western works.
        for (int y = HolmMinY; y <= HolmMaxY; y++)
            for (int x = HolmMinX; x <= HolmMaxX; x++)
                map[new Pos(x, y)] = Terrain.Floor;
        for (int x = 4; x < HolmMinX; x++)
            map[new Pos(x, midY)] = Terrain.Floor;

        var entry = new Pos(2, midY);

        // Mounds on the works: the eight-open rule keeps the ring whole, and
        // keeps them off the causeway for free (its neighbors are water). The
        // holm stays bare: nothing was ever built on the besieged ground.
        int mounds = 0;
        for (int attempt = 0; attempt < 300 && mounds < 6; attempt++)
        {
            var p = new Pos(rng.Range(2, LeaguerW - 2), rng.Range(2, LeaguerH - 2));
            bool clear = p.Manhattan(entry) >= 3
                && (p.X < HolmMinX - 1 || p.X > HolmMaxX + 1 || p.Y < HolmMinY - 1 || p.Y > HolmMaxY + 1);
            for (int dy = -1; dy <= 1 && clear; dy++)
                for (int dx = -1; dx <= 1 && clear; dx++)
                    if (map[p.Plus(dx, dy)] != Terrain.Floor) clear = false;
            if (!clear) continue;
            map[p] = Terrain.Wall;
            mounds++;
        }

        // The warders stand the works, spread the whole way around, none of
        // them close enough to the entry to make the first step a fight.
        var works = new List<Pos>();
        for (int y = 1; y < LeaguerH - 1; y++)
            for (int x = 1; x < LeaguerW - 1; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == Terrain.Floor && (x <= 3 || x >= LeaguerW - 4 || y <= 3 || y >= LeaguerH - 4)
                    && p.Manhattan(entry) > 6)
                    works.Add(p);
            }
        var warders = new List<Pos>();
        int guard = 4000;
        while (warders.Count < warderCount)
        {
            var p = rng.Pick(works);
            bool spaced = guard-- <= 0 || warders.All(q => q.Manhattan(p) >= 4);
            if (!warders.Contains(p) && spaced) warders.Add(p);
        }

        // The cist sits on the holm's crown: the far end of the one road.
        var chest = new Pos((HolmMinX + HolmMaxX) / 2, midY);

        map[entry] = Terrain.ExitLadder;
        return (map, entry, warders, chest);
    }
}

public static class Directions
{
    public static readonly (int dx, int dy)[] Cardinal = [(0, -1), (0, 1), (-1, 0), (1, 0)];
    public static readonly (int dx, int dy)[] All8 =
        [(0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)];
}
