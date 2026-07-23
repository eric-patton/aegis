using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The worldgen evaluation harness (D-137, plan 2026-07 D4): `aegis worldgen`
/// batch-generates worlds across seeds and tiers, straight through
/// <see cref="WorldGen.Generate"/> with no play in between, and charts the
/// generator's expressive range: story mixes, fact-graph composition, site
/// tenancy, name variety, and the prose-skeleton repetition audit that the
/// journey sweep structurally cannot run (most prose is talk-gated and
/// pilot-unexercised by design). Every world is generated twice and its
/// digests compared, so a generator that grows a hidden input fails loud.
/// `--json` is the CI shape; `--dump` is the generate-then-curate feed the
/// prose-variety work (plan D3) reads, every surface a world can produce.
/// </summary>
public static class WorldgenRunner
{
    public static int Run(string[] args)
    {
        int seeds = 30;
        ulong start = 1;
        int tierLo = 1, tierHi = 8;
        bool json = false;
        bool dump = false;
        int top = 12;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seeds": seeds = int.Parse(args[++i]); break;
                case "--start": start = ulong.Parse(args[++i]); break;
                case "--tiers":
                    string spec = args[++i];
                    int dash = spec.IndexOf('-');
                    if (dash < 0) { tierLo = tierHi = int.Parse(spec); }
                    else { tierLo = int.Parse(spec[..dash]); tierHi = int.Parse(spec[(dash + 1)..]); }
                    break;
                case "--json": json = true; break;
                case "--dump": dump = true; break;
                case "--top": top = int.Parse(args[++i]); break;
                default:
                    Console.Error.WriteLine($"aegis worldgen: unexpected argument '{args[i]}'");
                    return 1;
            }
        }
        if (tierLo < 1 || tierHi < tierLo || seeds < 1)
        {
            Console.Error.WriteLine("aegis worldgen: --seeds must be >= 1 and --tiers a valid range from 1 up");
            return 1;
        }

        var measures = new List<WorldMeasure>();
        var skeletons = new List<(ulong Seed, int Tier, List<string> Skeletons)>();
        var proseWorlds = new List<ProseWorldInventory>();
        int mismatches = 0;

        for (int tier = tierLo; tier <= tierHi; tier++)
        {
            for (int i = 0; i < seeds; i++)
            {
                ulong seed = start + (ulong)i;
                var world = WorldGen.Generate(seed, tier);

                // The purity check: the generator is a function of (seed, tier)
                // and nothing else. A second call must land the same digest.
                if (WorldEval.Digest(WorldGen.Generate(seed, tier)) != WorldEval.Digest(world))
                {
                    mismatches++;
                    Console.Error.WriteLine($"aegis worldgen: seed {seed} tier {tier} did not regenerate identically");
                }

                var prose = WorldEval.ProseSurfaces(world);
                proseWorlds.Add(new ProseWorldInventory(seed, tier, prose));

                if (dump)
                {
                    if (json)
                    {
                        foreach (var surface in prose)
                            Console.WriteLine(ProseDump.JsonLine(seed, tier, surface));
                    }
                    else
                    {
                        var m = WorldEval.Measure(world);
                        Console.WriteLine($"== seed {seed} tier {tier}  \"{m.WorldName}\" / {m.SettlementName}  ({m.Story}, {m.Twist}) ==");
                        foreach (string line in ProseDump.HumanLines(prose)) Console.WriteLine(line);
                        Console.WriteLine();
                    }
                    continue;
                }

                measures.Add(WorldEval.Measure(world));
                skeletons.Add((seed, tier, [.. prose.Select(s => s.NormalizedSkeleton)]));
            }
        }

        var proseAudit = WorldEval.AuditProse(proseWorlds);
        if (mismatches > 0)
            proseAudit.Failures.Add($"generator impurity: {mismatches} world(s) did not regenerate identically");
        if (dump)
        {
            foreach (string failure in proseAudit.Failures)
                Console.Error.WriteLine($"aegis worldgen: prose failure: {failure}");
            return proseAudit.Failures.Count == 0 ? 0 : 2;
        }

        var tiers = measures
            .GroupBy(m => m.Tier)
            .OrderBy(g => g.Key)
            .Select(g => WorldEval.Summarize(g.Key, [.. g]))
            .ToList();
        var audit = WorldEval.AuditSkeletons(skeletons, top);
        var weatherCoverage = measures
            .SelectMany(m => m.WeatherFamilies)
            .GroupBy(kv => kv.Key)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value));

        if (json)
        {
            var report = new WorldgenReport(
                Seeds: seeds, Start: start, TierLo: tierLo, TierHi: tierHi,
                Worlds: measures.Count, DigestMismatches: mismatches,
                WeatherCoverage: weatherCoverage,
                Prose: proseAudit,
                Tiers: tiers, Skeletons: audit, Measures: measures);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, PilotJsonPretty.Default.WorldgenReport));
            return proseAudit.Failures.Count == 0 ? 0 : 2;
        }

        var w = Console.Out;
        w.WriteLine($"AEGIS WORLDGEN   {seeds} seed(s) from {start}, tiers {tierLo}-{tierHi}   ({measures.Count} worlds)");
        w.WriteLine(new string('=', 70));
        foreach (var t in tiers)
        {
            w.WriteLine();
            w.WriteLine($"tier {t.Tier}  ({t.Worlds} worlds)");
            w.WriteLine($"  stories:   {string.Join(", ", t.Stories.Select(kv => $"{kv.Key} x{kv.Value}"))}");
            w.WriteLine($"  twists:    {string.Join(", ", t.Twists.Select(kv => $"{kv.Key} x{kv.Value}"))}");
            w.WriteLine($"  facts:     avg {t.AvgFacts} (min {t.MinFacts}, max {t.MaxFacts})");
            w.WriteLine($"  storylets: avg {t.AvgStorylets} (min {t.MinStorylets}, max {t.MaxStorylets})");
            w.WriteLine($"  sites:     {string.Join(", ", t.SiteKinds.Select(kv => $"{kv.Key} x{kv.Value}"))}");
            w.WriteLine($"  names:     {t.DistinctWorldNames}/{t.Worlds} world names distinct, "
                        + $"{t.DistinctSettlementNames}/{t.Worlds} settlement names distinct");
        }
        w.WriteLine();
        w.WriteLine(new string('-', 70));
        w.WriteLine($"weather hands: {string.Join(", ", weatherCoverage.Select(kv => $"{kv.Key} x{kv.Value}"))}");
        w.WriteLine();
        w.WriteLine($"prose skeletons (names struck out, {audit.Surfaces} surfaces):");
        w.WriteLine($"  {audit.DistinctSkeletons} distinct; {audit.RepeatShare:P1} of surfaces reuse a skeleton across worlds.");
        w.WriteLine($"  most reused:");
        foreach (var r in audit.TopRepeats)
        {
            string text = r.Skeleton.Length > 88 ? r.Skeleton[..85] + "..." : r.Skeleton;
            w.WriteLine($"    x{r.Count,-4} ({r.Worlds} worlds)  {text}");
        }
        w.WriteLine($"  family audit: {proseAudit.FixedSurfaces} fixed, {proseAudit.VariableSurfaces} variable, {proseAudit.Families} families");
        w.WriteLine($"  per kind: {string.Join(", ", proseAudit.PerKind.Select(kv => $"{kv.Key} {kv.Value}"))}");
        w.WriteLine($"  family coverage: {string.Join(", ", proseAudit.FamilyCoverage.Select(kv => $"{kv.Key} {kv.Value} kind(s)"))}");
        foreach (string warning in proseAudit.Warnings) w.WriteLine($"  warning: {warning}");
        foreach (string failure in proseAudit.Failures) w.WriteLine($"  FAILURE: {failure}");
        w.WriteLine();
        w.WriteLine(proseAudit.Failures.Count == 0
            ? "purity: every world regenerated identically and every prose hard gate passed."
            : $"PURITY FAILURE: {proseAudit.Failures.Count} hard failure(s).");
        return proseAudit.Failures.Count == 0 ? 0 : 2;
    }
}

/// <summary>The harness's machine-readable report (D-137): the prose report's facts as data, for CI charting.</summary>
internal sealed record WorldgenReport(
    int Seeds, ulong Start, int TierLo, int TierHi, int Worlds, int DigestMismatches,
    Dictionary<string, int> WeatherCoverage,
    ProseAuditSummary Prose,
    List<TierSummary> Tiers, SkeletonSummary Skeletons, List<WorldMeasure> Measures);
