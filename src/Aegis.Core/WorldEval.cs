namespace Aegis.Core;

/// <summary>One site as the harness reads it: what stands there, and what it holds.</summary>
public sealed record SiteMeasure(
    string Id, string Kind, int Spawns, string Monsters, bool Stone, bool Coffer,
    int FishingReaches, int SaltPans);

/// <summary>
/// One generated world reduced to the numbers the expressive-range charts want
/// (D-137, plan 2026-07 D4): names, the story drawn, the cast, the fact graph's
/// composition, the compiled storylets, and the sites with their tenants.
/// </summary>
public sealed record WorldMeasure(
    ulong Seed, int Tier, int GeneratorVersion, string WorldName, string SettlementName, string Story,
    string Twist, string TwistVariant, int Waystones,
    int Npcs, int Villagers, int Facts, Dictionary<string, int> FactTypes,
    int StoryStorylets, List<string> StoryletIds,
    List<SiteMeasure> Sites, int Gleanings, int HerbSpots, int TarnIronSeams, bool WildPony,
    Dictionary<string, int> WeatherFamilies, int BreadBase,
    string FenRegion, string FenHamlet, string FenHash,
    int FenWalkable, int FenBog, int FenWater);

/// <summary>One tier's slice of the batch: how the generator spreads itself at that depth.</summary>
public sealed record TierSummary(
    int Tier, int Worlds, Dictionary<string, int> Stories, Dictionary<string, int> Twists,
    double AvgFacts, int MinFacts, int MaxFacts,
    double AvgStorylets, int MinStorylets, int MaxStorylets,
    Dictionary<string, int> SiteKinds,
    int DistinctWorldNames, int DistinctSettlementNames);

/// <summary>One prose skeleton and how often the batch reused it (D-137: the research/13 repetition audit).</summary>
public sealed record SkeletonRepeat(string Skeleton, int Count, int Worlds);

/// <summary>
/// The batch's prose ledger: how many surfaces were generated, how many distinct
/// skeletons they reduce to once names are struck out, and the most-reused ones.
/// A skeleton shared by every world is authored texture being honest about
/// itself; a skeleton meant to vary that lands here anyway is the finding.
/// </summary>
public sealed record SkeletonSummary(
    int Surfaces, int DistinctSkeletons, double RepeatShare, List<SkeletonRepeat> TopRepeats);

/// <summary>
/// The worldgen evaluation harness's measuring half (D-137, plan 2026-07 D4):
/// pure reads over a generated <see cref="World"/>, no I/O, no RNG, no Game.
/// The CLI's `aegis worldgen` batches these across seeds and tiers; the tests
/// hold the reads honest. Built now, before the generator grows again (B1's
/// road and B2's town), so every later generator change lands with its
/// expressive range charted instead of guessed at.
/// </summary>
public static class WorldEval
{
    /// <summary>Reduces one generated world to its measure. Pure: same world, same numbers.</summary>
    public static WorldMeasure Measure(World w)
    {
        var factTypes = w.Facts.All
            .GroupBy(f => f.Type)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count());

        var sites = w.Sites
            .OrderBy(s => (int)s.Kind)
            .Select(s => new SiteMeasure(
                s.Id, s.Kind.ToString().ToLowerInvariant(), s.Spawns.Count,
                string.Join(",", s.Spawns.Select(sp => sp.Kind).Distinct().Select(k => k.ToString().ToLowerInvariant())),
                s.StonePos is not null, s.CofferPos is not null, s.FishingReaches.Count, s.SaltPans.Count))
            .ToList();

        string story = w.Facts.OfType("story").FirstOrDefault()?.Subject ?? "none";

        return new WorldMeasure(
            Seed: w.Seed,
            Tier: w.Tier,
            GeneratorVersion: w.GeneratorVersion,
            WorldName: w.Name,
            SettlementName: w.SettlementName,
            Story: story,
            Twist: WorldTwistCatalog.IdOf(w.Twist),
            TwistVariant: w.RoadHolder?.ToString().ToLowerInvariant() ?? "",
            Waystones: w.Waystones.Count,
            Npcs: w.Npcs.Count,
            Villagers: w.Npcs.Count(n => n.Kind == NpcKind.Villager),
            Facts: w.Facts.All.Count,
            FactTypes: factTypes,
            StoryStorylets: w.StoryStorylets.Count,
            StoryletIds: [.. w.StoryStorylets.Select(s => s.Id)],
            Sites: sites,
            Gleanings: w.Gleanings.Count,
            HerbSpots: w.Herbs.Count,
            TarnIronSeams: w.TarnIronSeams.Count,
            WildPony: w.WildPonyPos is not null,
            WeatherFamilies: WeatherCoverage(w.Seed),
            // The one generation-time price spread today: the blight's thin larder
            // prices bread at 6 where every other story opens at 4, mirroring
            // RationPrice's base term. Grows into a real spread when per-region
            // prices land (plan 2026-07 B3); the column is here so the chart is.
            BreadBase: story == CreepingBlightTemplate.Id ? 6 : 4,
            FenRegion: w.FenRegion.Name,
            FenHamlet: w.FenHamletName,
            FenHash: w.Fens.ContentHash().ToString("x16"),
            FenWalkable: Count(w.Fens, t => TerrainInfo.Walkable(t)),
            FenBog: Count(w.Fens, t => t == Terrain.Bog),
            FenWater: Count(w.Fens, t => t == Terrain.Water));
    }

    private static int Count(GameMap map, Func<Terrain, bool> predicate)
    {
        int count = 0;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (predicate(map[new Pos(x, y)])) count++;
        return count;
    }

    /// <summary>
    /// Four full years of deterministic hands, enough for the broad evaluator
    /// to chart every band and family without advancing a live Game.
    /// </summary>
    public static Dictionary<string, int> WeatherCoverage(ulong worldSeed, int years = 4)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var band in Enum.GetValues<ClimateBand>())
            foreach (var family in Enum.GetValues<WeatherFamily>())
                counts[$"{band.ToString().ToLowerInvariant()}:{family.ToString().ToLowerInvariant()}"] = 0;

        for (int seasonIndex = 0; seasonIndex < years * 4; seasonIndex++)
            foreach (var band in Enum.GetValues<ClimateBand>())
                foreach (var family in WeatherCalendar.Hand(worldSeed, band, seasonIndex))
                    counts[$"{band.ToString().ToLowerInvariant()}:{family.ToString().ToLowerInvariant()}"]++;
        return counts;
    }

    /// <summary>Every owned narrative surface this world can produce (D-159).</summary>
    public static List<ProseSurface> ProseSurfaces(World w)
    {
        var surfaces = new List<ProseSurface>();

        foreach (var fact in w.Facts.All.Where(f => f.Detail.Length > 0))
        {
            if (ProseCatalog.FamilyFor(fact) is not null)
                surfaces.AddRange(ProseCatalog.RenderAll(w, fact));
            else
                surfaces.Add(Fixed(
                    $"fact.{ProseNormalizer.Slug(fact.Type)}.{ProseNormalizer.Slug(fact.Subject)}.{fact.Id}",
                    ProseSurfaceKind.FactDetail, fact.Detail, w, "legacy-fact"));
        }

        // Runtime facts also belong in a generate-then-curate view. Their
        // sample records carry structured fields, never copied rendered prose.
        foreach (var fact in ProseCatalog.MissingFamilySamples(w))
            surfaces.AddRange(ProseCatalog.RenderAll(w, fact));

        var storylets = StoryletCatalog.All.Concat(w.StoryStorylets)
            .GroupBy(s => s.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(s => s.Id, StringComparer.Ordinal);
        foreach (var storylet in storylets)
        {
            for (int i = 0; i < storylet.Lines.Length; i++)
                surfaces.Add(Fixed($"storylet.{ProseNormalizer.Slug(storylet.Id)}.line.{i + 1}",
                    ProseSurfaceKind.Storylet, storylet.Lines[i].Text, w, "legacy-storylet"));
            if (storylet.Scene is not { } scene) continue;
            foreach (var node in scene.Nodes)
                for (int i = 0; i < node.Lines.Length; i++)
                    surfaces.Add(Fixed(
                        $"scene.{ProseNormalizer.Slug(scene.Id)}.{ProseNormalizer.Slug(node.Id)}.line.{i + 1}",
                        ProseSurfaceKind.Scene, node.Lines[i].Text, w, "legacy-scene"));
        }
        surfaces.AddRange(Game.AuditTopicCatalog(w.Seed));
        return surfaces;
    }

    private static ProseSurface Fixed(
        string sourceId,
        ProseSurfaceKind kind,
        string raw,
        World world,
        string origin) =>
        new(sourceId, kind, null, "fixed", raw, ProseNormalizer.Normalize(raw, world),
            ProseReusePolicy.Fixed, origin);

    /// <summary>Runs the family-aware audit across a batch of world inventories.</summary>
    public static ProseAuditSummary AuditProse(IReadOnlyList<ProseWorldInventory> worlds) =>
        ProseAudit.Audit(worlds);

    /// <summary>
    /// Every prose skeleton this world owns, with generated names struck out,
    /// retained as the compatibility view for the original D-137 ledger.
    /// </summary>
    public static List<string> Skeletons(World w) =>
        [.. ProseSurfaces(w).Select(s => s.NormalizedSkeleton)];

    /// <summary>The same surfaces with their names left in: the curation dump's raw half.</summary>
    public static List<string> RawSurfaces(World w) => [.. ProseSurfaces(w).Select(s => s.RawText)];

    /// <summary>
    /// Strikes this world's generated names out of one surface: people, raiders,
    /// the stead, the world itself. Longest names first, whole words only, so a
    /// name that happens to sit inside another (or inside plain prose) never
    /// half-replaces. What survives is the authored skeleton.
    /// </summary>
    public static string Normalize(string surface, World w) => ProseNormalizer.Normalize(surface, w);

    /// <summary>Folds one tier's measures into its summary row.</summary>
    public static TierSummary Summarize(int tier, IReadOnlyList<WorldMeasure> measures)
    {
        var stories = measures
            .GroupBy(m => m.Story)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count());
        var siteKinds = measures
            .SelectMany(m => m.Sites)
            .GroupBy(s => s.Kind)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count());
        var twists = measures
            .GroupBy(m => m.Twist)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count());
        return new TierSummary(
            Tier: tier,
            Worlds: measures.Count,
            Stories: stories,
            Twists: twists,
            AvgFacts: Math.Round(measures.Average(m => m.Facts), 1),
            MinFacts: measures.Min(m => m.Facts),
            MaxFacts: measures.Max(m => m.Facts),
            AvgStorylets: Math.Round(measures.Average(m => m.StoryStorylets), 1),
            MinStorylets: measures.Min(m => m.StoryStorylets),
            MaxStorylets: measures.Max(m => m.StoryStorylets),
            SiteKinds: siteKinds,
            DistinctWorldNames: measures.Select(m => m.WorldName).Distinct().Count(),
            DistinctSettlementNames: measures.Select(m => m.SettlementName).Distinct().Count());
    }

    /// <summary>
    /// Folds every world's skeletons into the repetition ledger. A repeat is a
    /// skeleton seen in more than one world; the share is how much of the whole
    /// batch's prose that covers. Ties broken by text so the ledger is stable.
    /// </summary>
    public static SkeletonSummary AuditSkeletons(IReadOnlyList<(ulong Seed, int Tier, List<string> Skeletons)> worlds, int top = 12)
    {
        var occurrences = new Dictionary<string, (int Count, HashSet<(ulong, int)> Worlds)>();
        int total = 0;
        foreach (var (seed, tier, skeletons) in worlds)
            foreach (var s in skeletons)
            {
                total++;
                if (!occurrences.TryGetValue(s, out var o))
                    occurrences[s] = o = (0, []);
                occurrences[s] = (o.Count + 1, o.Worlds);
                o.Worlds.Add((seed, tier));
            }

        var repeats = occurrences.Where(kv => kv.Value.Worlds.Count > 1).ToList();
        int repeated = repeats.Sum(kv => kv.Value.Count);
        var topRepeats = repeats
            .OrderByDescending(kv => kv.Value.Count)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(top)
            .Select(kv => new SkeletonRepeat(kv.Key, kv.Value.Count, kv.Value.Worlds.Count))
            .ToList();

        return new SkeletonSummary(
            Surfaces: total,
            DistinctSkeletons: occurrences.Count,
            RepeatShare: total == 0 ? 0 : Math.Round((double)repeated / total, 3),
            TopRepeats: topRepeats);
    }

    /// <summary>
    /// A stable fingerprint of everything the measure and the dump read, for the
    /// harness's purity check: the same seed and tier generated twice must print
    /// the same digest, or the generator has grown a hidden input. FNV-1a, no
    /// dependencies, deterministic across runs and machines.
    /// </summary>
    public static string Digest(World w)
    {
        ulong h = 14695981039346656037UL;
        void Fold(string s)
        {
            foreach (char c in s)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            h ^= '\n';
            h *= 1099511628211UL;
        }

        var m = Measure(w);
        Fold($"{m.Seed}|{m.Tier}|{m.GeneratorVersion}|{m.WorldName}|{m.SettlementName}|{m.Story}|{m.Twist}|{m.TwistVariant}|{m.Waystones}|{m.Npcs}|{m.Facts}|{m.StoryStorylets}|{m.Gleanings}|{m.HerbSpots}|{m.TarnIronSeams}|{m.WildPony}|{m.FenRegion}|{m.FenHamlet}|{m.FenHash}|{m.FenWalkable}|{m.FenBog}|{m.FenWater}");
        foreach (var kv in m.FactTypes) Fold($"{kv.Key}={kv.Value}");
        foreach (var kv in m.WeatherFamilies) Fold($"{kv.Key}={kv.Value}");
        foreach (var id in m.StoryletIds) Fold(id);
        foreach (var s in m.Sites) Fold($"{s.Id}|{s.Kind}|{s.Spawns}|{s.Monsters}|{s.Stone}|{s.Coffer}|{s.FishingReaches}|{s.SaltPans}");
        foreach (var s in ProseSurfaces(w))
            Fold($"{s.SourceId}|{s.Kind}|{s.FamilyId}|{s.VariantId}|{s.ReusePolicy}|{s.Origin}|{s.RawText}|{s.NormalizedSkeleton}");
        return h.ToString("x16");
    }
}
