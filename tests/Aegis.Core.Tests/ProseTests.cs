using System.Text.Json;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The fact-keyed prose catalog, inventory, and audit (D-159).</summary>
public class ProseTests
{
    [Fact]
    public void TheCatalog_MeetsEveryDeclaredBudget_AndTheBroadSurfaceContract()
    {
        Assert.Empty(ProseCatalog.Validate());
        Assert.Equal(5, ProseCatalog.Families.Count);
        Assert.All(ProseCatalog.Families, family =>
        {
            Assert.True(family.Renderings.Select(r => r.Kind).Distinct().Count() >= 2);
            Assert.All(family.Renderings, rendering =>
                Assert.True(rendering.Variants.Length >= ProseAudit.RequiredVariants(family.ReusePolicy)));
        });
        Assert.Contains(ProseCatalog.Families,
            family => family.Renderings.Select(r => r.Kind).Distinct().Count() >= 4);
    }

    [Fact]
    public void WorldInventory_EnumeratesFactsTopicsStoryletsAndScenes_WithStableSources()
    {
        var world = WorldGen.Generate(42, tier: 4);
        var a = WorldEval.ProseSurfaces(world);
        var b = WorldEval.ProseSurfaces(WorldGen.Generate(42, tier: 4));

        Assert.Equal(a, b);
        Assert.Equal(a.Count, a.Select(s => s.SourceId).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(a, s => s.Kind == ProseSurfaceKind.FactDetail);
        Assert.Contains(a, s => s.Kind == ProseSurfaceKind.Topic);
        Assert.Contains(a, s => s.Kind == ProseSurfaceKind.Rumor);
        Assert.Contains(a, s => s.Kind == ProseSurfaceKind.Ledger);
        Assert.Contains(a, s => s.Kind == ProseSurfaceKind.Storylet);
        Assert.Contains(a, s => s.Kind == ProseSurfaceKind.Scene);
        Assert.True(a.Count(s => s.Origin == "legacy-topic") > 50,
            "the unvisited topic catalog must remain visible to WorldEval");
        Assert.All(a, s =>
        {
            Assert.NotEmpty(s.SourceId);
            Assert.NotEmpty(s.VariantId);
            Assert.NotEmpty(s.RawText);
            Assert.NotEmpty(s.NormalizedSkeleton);
            Assert.NotEmpty(s.Origin);
        });
        Assert.Equal(ProseCatalog.Families.Select(f => f.Id).Order(),
            a.Where(s => s.FamilyId is not null).Select(s => s.FamilyId!).Distinct().Order());
    }

    [Fact]
    public void EveryLiveTopic_HasMatchingEnumerableMetadata()
    {
        var game = new Game(42);
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);

        Assert.Equal(game.Topics.Count, game.TopicSurfaces.Count);
        Assert.Equal(game.Topics.Select(t => t.Answer), game.TopicSurfaces.Select(s => s.RawText));
        Assert.All(game.TopicSurfaces, surface => Assert.Equal(ProseSurfaceKind.Topic, surface.Kind));
        Assert.Equal(game.TopicSurfaces.Count,
            game.TopicSurfaces.Select(s => s.SourceId).Distinct(StringComparer.Ordinal).Count());

        var first = game.TopicSurfaces.Select(s => (s.SourceId, s.VariantId, s.RawText)).ToList();
        game.ApplyKey('z');
        NpcTests.BumpNpc(game, villager);
        Assert.Equal(first, game.TopicSurfaces.Select(s => (s.SourceId, s.VariantId, s.RawText)).ToList());
    }

    [Fact]
    public void GeneratedFactReaders_UseTheComposedSettlementGrievanceAndWaygateFamilies()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));

        Assert.Contains(game.TopicSurfaces, s => s.FamilyId == "settlement");
        Assert.Contains(game.TopicSurfaces, s => s.FamilyId == "goblin-grievance");
        Assert.Contains(game.TopicSurfaces, s => s.FamilyId == "waygate");
    }

    [Fact]
    public void RuntimeEventReader_UsesTheComposedHardWinterFamily()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        for (int i = 0; i < SteadRaids.TickTurns * 6
             && !game.World.Facts.Exists("event", "hard_winter"); i++)
            game.Apply(Command.Wait);
        Assert.True(game.World.Facts.Exists("event", "hard_winter"));
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Keeper));

        var surface = Assert.Single(game.TopicSurfaces, s => s.FamilyId == "hard-winter");
        Assert.Contains(game.Topics, t => t.Label == "The season's news" && t.Answer == surface.RawText);
    }

    [Fact]
    public void ConsequenceReader_UsesTheComposedCampClearedFamily()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));

        var surface = Assert.Single(game.TopicSurfaces, s => s.FamilyId == "camp-cleared");
        Assert.Contains(game.Topics, t => t.Label == "The quiet nights" && t.Answer == surface.RawText);
    }

    [Fact]
    public void Selection_IsPureStableAndIndependentBySurfaceKind()
    {
        var world = WorldGen.Generate(42);
        var fact = world.Facts.Find("settlement", world.SettlementName)!;
        string digest = WorldEval.Digest(world);

        var topicA = ProseCatalog.Render(world, fact, ProseSurfaceKind.Topic);
        var topicB = ProseCatalog.Render(world, fact, ProseSurfaceKind.Topic);
        var ledger = ProseCatalog.Render(world, fact, ProseSurfaceKind.Ledger);

        Assert.Equal(topicA, topicB);
        Assert.NotEqual(topicA.SourceId, ledger.SourceId);
        Assert.NotEqual(topicA.VariantId, ledger.VariantId);
        Assert.Equal(digest, WorldEval.Digest(world));

        var variants = Enumerable.Range(1, 40)
            .Select(seed =>
            {
                var generated = WorldGen.Generate((ulong)seed);
                var generatedFact = generated.Facts.Find("settlement", generated.SettlementName)!;
                return ProseCatalog.Render(generated, generatedFact, ProseSurfaceKind.Topic).VariantId;
            })
            .Distinct()
            .ToList();
        Assert.True(variants.Count > 1);
    }

    [Fact]
    public void CompatibleBundleParts_NeverMixAcrossVariants()
    {
        var family = Family("bundle", ProseReusePolicy.Rare, ["name"],
            new ProseRendering("bundle.fact", ProseSurfaceKind.FactDetail,
            [
                new ProseVariant("a", ["A {name}", "A tail"]),
                new ProseVariant("b", ["B {name}", "B tail"]),
            ]));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var world = WorldGen.Generate(seed);
            var rendered = ProseComposer.Render(world, new Fact(7, "sample", "one", "", ""),
                family, ProseSurfaceKind.FactDetail, ProseContext.Of(("name", "stone")));
            Assert.Contains(rendered.RawText, new[] { "A stone A tail", "B stone B tail" });
            seen.Add(rendered.VariantId);
        }
        Assert.Equal(2, seen.Count);
    }

    [Fact]
    public void Context_RejectsDuplicateMissingUnknownAndUnresolvedValues()
    {
        Assert.Throws<ProseValidationException>(() => new ProseContext(
        [
            new KeyValuePair<string, string>("name", "one"),
            new KeyValuePair<string, string>("name", "two"),
        ]));

        var world = WorldGen.Generate(42);
        var fact = new Fact(7, "sample", "one", "", "");
        var missing = Family("missing", ProseReusePolicy.Fixed, ["name"],
            new ProseRendering("missing.fact", ProseSurfaceKind.FactDetail,
                [new ProseVariant("only", ["Hello {name}"])]));
        Assert.Throws<ProseValidationException>(() => ProseComposer.Render(world, fact, missing,
            ProseSurfaceKind.FactDetail, ProseContext.Of()));
        Assert.Throws<ProseValidationException>(() => ProseComposer.Render(world, fact, missing,
            ProseSurfaceKind.FactDetail, ProseContext.Of(("name", "one"), ("extra", "two"))));

        var unresolved = Family("unresolved", ProseReusePolicy.Fixed, [],
            new ProseRendering("unresolved.fact", ProseSurfaceKind.FactDetail,
                [new ProseVariant("only", ["Hello {Bad}"])]));
        Assert.Throws<ProseValidationException>(() => ProseComposer.Render(world, fact, unresolved,
            ProseSurfaceKind.FactDetail, ProseContext.Of()));
    }

    [Fact]
    public void Validation_FailsEveryStructuralCatalogError()
    {
        var broken = new ProseFamily("broken", new FactPattern("sample"), ProseReusePolicy.Frequent,
            ["known", "known"],
            [
                new ProseRendering("same", ProseSurfaceKind.FactDetail,
                [
                    new ProseVariant("dup", ["Same {known}"]),
                    new ProseVariant("dup", ["Same {known}"]),
                    new ProseVariant("empty", [""]),
                ]),
                new ProseRendering("same", ProseSurfaceKind.FactDetail,
                    [new ProseVariant("bad", ["Unknown {other} and {Bad}"])]),
            ]);
        var failures = ProseAudit.ValidateFamilies([broken, broken]);

        Assert.Contains(failures, f => f.Contains("duplicate family id"));
        Assert.Contains(failures, f => f.Contains("duplicate source id"));
        Assert.Contains(failures, f => f.Contains("duplicate context tokens"));
        Assert.Contains(failures, f => f.Contains("surface kind twice"));
        Assert.Contains(failures, f => f.Contains("needs 4"));
        Assert.Contains(failures, f => f.Contains("duplicate variant ids"));
        Assert.Contains(failures, f => f.Contains("is empty"));
        Assert.Contains(failures, f => f.Contains("unknown token"));
        Assert.Contains(failures, f => f.Contains("invalid placeholder"));
        Assert.Contains(failures, f => f.Contains("identical normalized variants"));
    }

    [Fact]
    public void Normalization_StrikesGeneratedNames_AndCollapsesWhitespace()
    {
        var world = WorldGen.Generate(42);
        string npc = world.Npcs[0].Name;
        string normalized = ProseNormalizer.Normalize(
            $"  {world.SettlementName}   sent {npc} toward {world.TownName}.  ", world);
        Assert.Equal("{settlement} sent {person} toward {town}.", normalized);
    }

    [Fact]
    public void FamilyAudit_SeparatesFixedAndVariable_AndKeepsWarningsAdvisory()
    {
        var inventories = Enumerable.Range(1, 8)
            .Select(seed =>
            {
                var world = WorldGen.Generate((ulong)seed, tier: 3);
                return new ProseWorldInventory((ulong)seed, 3, WorldEval.ProseSurfaces(world));
            })
            .ToList();
        var audit = ProseAudit.Audit(inventories);

        Assert.Empty(audit.Failures);
        Assert.True(audit.FixedSurfaces > 0);
        Assert.True(audit.VariableSurfaces > 0);
        Assert.Equal(5, audit.Families);
        Assert.All(audit.FamilyCoverage.Values, kinds => Assert.True(kinds >= 2));
        Assert.NotEmpty(audit.Warnings);
        Assert.Contains(audit.Warnings, w => w.Contains("legacy surface"));
    }

    [Fact]
    public void Audit_FailsWhenDeclaredVariableContentIsAbsent()
    {
        var family = FourKindFamily("absent");
        var audit = ProseAudit.Audit([], [family]);
        Assert.Contains(audit.Failures, f => f.Contains("absent from the curated catalog"));
    }

    [Fact]
    public void Audit_FailsDuplicateSourcesEmptyTextAndUnresolvedComposition()
    {
        var family = FourKindFamily("present");
        var surfaces = new List<ProseSurface>
        {
            new("same", ProseSurfaceKind.FactDetail, "present", "fact-a", "", "", ProseReusePolicy.Rare, "curated-family"),
            new("same", ProseSurfaceKind.Topic, "present", "topic-a", "Still {missing}", "Still {missing}", ProseReusePolicy.Rare, "curated-family"),
        };
        var audit = ProseAudit.Audit([new ProseWorldInventory(1, 1, surfaces)], [family]);

        Assert.Contains(audit.Failures, f => f.Contains("duplicate source"));
        Assert.Contains(audit.Failures, f => f.Contains("rendered empty"));
        Assert.Contains(audit.Failures, f => f.Contains("unresolved placeholder"));
    }

    [Fact]
    public void BothDumpShapes_CarrySourceFamilyVariantReuseAndOrigin()
    {
        var world = WorldGen.Generate(42);
        var surface = WorldEval.ProseSurfaces(world).First(s => s.FamilyId is not null);
        var human = ProseDump.HumanLines([surface]).ToList();
        Assert.Contains(human, line => line.Contains($"family {surface.FamilyId}"));
        Assert.Contains(human, line => line.Contains(surface.SourceId) && line.Contains($"variant={surface.VariantId}"));
        Assert.Contains(surface.RawText, human);

        string json = ProseDump.JsonLine(world.Seed, world.Tier, surface);
        Assert.DoesNotContain('\n', json);
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(world.Seed, parsed.RootElement.GetProperty("seed").GetUInt64());
        Assert.Equal(surface.SourceId, parsed.RootElement.GetProperty("sourceId").GetString());
        Assert.Equal(surface.FamilyId, parsed.RootElement.GetProperty("familyId").GetString());
        Assert.Equal(surface.VariantId, parsed.RootElement.GetProperty("variantId").GetString());
        Assert.Equal(surface.Origin, parsed.RootElement.GetProperty("origin").GetString());
    }

    private static ProseFamily Family(
        string id,
        ProseReusePolicy reuse,
        string[] tokens,
        params ProseRendering[] renderings) =>
        new(id, new FactPattern("sample"), reuse, tokens, renderings);

    private static ProseFamily FourKindFamily(string id)
    {
        ProseRendering Rendering(ProseSurfaceKind kind) => new($"{id}.{kind}", kind,
        [
            new ProseVariant($"{kind}-a", [$"{kind} one"]),
            new ProseVariant($"{kind}-b", [$"{kind} two"]),
        ]);
        return Family(id, ProseReusePolicy.Rare, [],
            Rendering(ProseSurfaceKind.FactDetail),
            Rendering(ProseSurfaceKind.Topic),
            Rendering(ProseSurfaceKind.Rumor),
            Rendering(ProseSurfaceKind.Ledger));
    }
}
