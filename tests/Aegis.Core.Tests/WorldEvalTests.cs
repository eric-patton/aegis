using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The worldgen evaluation harness's measuring half (D-137, plan 2026-07 D4):
/// pure reads over a generated world, batched by `aegis worldgen` into the
/// expressive-range charts. The tests hold the reads honest: the measure sees
/// what the generator built, the digest is pure, the skeleton dump strikes
/// every generated name, and the audit counts what actually repeats.
/// </summary>
public class WorldEvalTests
{
    [Fact]
    public void TheMeasure_ReadsTheWorldItWasHanded()
    {
        var w = WorldGen.Generate(42, tier: 1);
        var m = WorldEval.Measure(w);

        Assert.Equal(42UL, m.Seed);
        Assert.Equal(1, m.Tier);
        Assert.Equal(w.Name, m.WorldName);
        Assert.Equal(w.SettlementName, m.SettlementName);
        // Tier 1 has exactly one eligible template, so there is no draw to vary.
        Assert.Equal(RaidedSteadTemplate.Id, m.Story);
        Assert.Equal(4, m.BreadBase);
        Assert.Equal(w.Npcs.Count, m.Npcs);
        Assert.Equal(w.Facts.All.Count, m.Facts);
        Assert.Equal(m.Facts, m.FactTypes.Values.Sum());
        Assert.Equal(w.StoryStorylets.Count, m.StoryStorylets);

        // The tier-1 band: camp, songhall, and harrow stand; the deeper mouths do not.
        var kinds = m.Sites.Select(s => s.Kind).ToList();
        Assert.Contains("goblincamp", kinds);
        Assert.Contains("songhall", kinds);
        Assert.Contains("harrow", kinds);
        Assert.DoesNotContain("barrow", kinds);
        Assert.DoesNotContain("threshold", kinds);
    }

    [Fact]
    public void TheDeepBands_ShowUpInTheMeasure()
    {
        var m = WorldEval.Measure(WorldGen.Generate(42, tier: 6));
        var kinds = m.Sites.Select(s => s.Kind).ToList();
        Assert.Contains("barrow", kinds);
        Assert.Contains("hollow", kinds);
        Assert.Contains("quarry", kinds);
        Assert.Contains("hall", kinds);
        Assert.Contains("ringfort", kinds);
        Assert.Contains("leaguer", kinds);
        Assert.Contains("threshold", kinds);
        Assert.Contains("wilds", kinds);

        // The camp holds its raiders and the harness names their kind.
        var camp = m.Sites.Single(s => s.Kind == "goblincamp");
        Assert.True(camp.Spawns > 0);
        Assert.Equal("goblin", camp.Monsters);
        Assert.True(camp.Stone);
        Assert.True(camp.Coffer);
    }

    [Fact]
    public void TheDigest_IsPure_AndTellsSeedsApart()
    {
        Assert.Equal(
            WorldEval.Digest(WorldGen.Generate(7, tier: 3)),
            WorldEval.Digest(WorldGen.Generate(7, tier: 3)));
        Assert.NotEqual(
            WorldEval.Digest(WorldGen.Generate(7, tier: 3)),
            WorldEval.Digest(WorldGen.Generate(8, tier: 3)));
        Assert.NotEqual(
            WorldEval.Digest(WorldGen.Generate(7, tier: 3)),
            WorldEval.Digest(WorldGen.Generate(7, tier: 4)));
    }

    [Fact]
    public void TheSkeletons_StrikeEveryGeneratedName()
    {
        var w = WorldGen.Generate(99, tier: 5);
        var skeletons = WorldEval.Skeletons(w);
        Assert.Equal(WorldEval.RawSurfaces(w).Count, skeletons.Count);
        Assert.NotEmpty(skeletons);

        var names = w.Npcs.Select(n => n.Name)
            .Append(w.SettlementName)
            .Append(w.Name)
            .Concat(w.Sites.SelectMany(s => s.Spawns).Where(s => s.Epithet is not null).Select(s => s.Epithet!));
        foreach (var skeleton in skeletons)
            foreach (var name in names)
                Assert.DoesNotContain($" {name} ", skeleton);

        // The striking leaves tokens, not holes: the stead's own facts name it.
        Assert.Contains(skeletons, s => s.Contains("{settlement}"));
        Assert.Contains(skeletons, s => s.Contains("{person}") || s.Contains("{raider}"));
    }

    [Fact]
    public void TheAudit_CountsWhatRepeats_AcrossWorlds()
    {
        var worlds = new List<(ulong, int, List<string>)>
        {
            (1, 1, ["the same line", "only here"]),
            (2, 1, ["the same line", "its own line"]),
            (3, 1, ["the same line"]),
        };
        var audit = WorldEval.AuditSkeletons(worlds);

        Assert.Equal(5, audit.Surfaces);
        Assert.Equal(3, audit.DistinctSkeletons);
        var repeat = Assert.Single(audit.TopRepeats);
        Assert.Equal("the same line", repeat.Skeleton);
        Assert.Equal(3, repeat.Count);
        Assert.Equal(3, repeat.Worlds);
        Assert.Equal(0.6, audit.RepeatShare, 3);
    }

    [Fact]
    public void TheTierSummary_FoldsItsWorlds()
    {
        var measures = Enumerable.Range(0, 5)
            .Select(i => WorldEval.Measure(WorldGen.Generate((ulong)(100 + i), tier: 4)))
            .ToList();
        var t = WorldEval.Summarize(4, measures);

        Assert.Equal(4, t.Tier);
        Assert.Equal(5, t.Worlds);
        Assert.Equal(5, t.Stories.Values.Sum());
        Assert.InRange(t.AvgFacts, t.MinFacts, t.MaxFacts);
        Assert.InRange(t.AvgStorylets, t.MinStorylets, t.MaxStorylets);
        // Every tier-4 world raises the same mandatory mouths: one camp, one songhall,
        // one harrow apiece, so the site tally lands exactly one per world.
        Assert.Equal(5, t.SiteKinds["goblincamp"]);
        Assert.Equal(5, t.SiteKinds["songhall"]);
        Assert.Equal(5, t.SiteKinds["harrow"]);
        Assert.InRange(t.DistinctWorldNames, 1, 5);
        Assert.InRange(t.DistinctSettlementNames, 1, 5);
    }
}
