using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Aegis.Core;

/// <summary>The narrative surfaces owned by the prose-variety audit (D-159).</summary>
public enum ProseSurfaceKind
{
    FactDetail,
    Rumor,
    Topic,
    Ledger,
    Song,
    Epitaph,
    Storylet,
    Scene,
}

/// <summary>The authored variation budget declared by one prose family.</summary>
public enum ProseReusePolicy { Fixed, Rare, Standard, Frequent }

/// <summary>One enumerable, provenance-bearing narrative surface.</summary>
public sealed record ProseSurface(
    string SourceId,
    ProseSurfaceKind Kind,
    string? FamilyId,
    string VariantId,
    string RawText,
    string NormalizedSkeleton,
    ProseReusePolicy ReusePolicy,
    string Origin);

/// <summary>One compatible authored bundle. Parts are selected and rendered together.</summary>
public sealed record ProseVariant(string Id, string[] Parts);

/// <summary>One surface kind and its compatible authored bundles.</summary>
public sealed record ProseRendering(
    string SourceId,
    ProseSurfaceKind Kind,
    ProseVariant[] Variants);

/// <summary>A fact pattern and the authored surfaces through which it may be told.</summary>
public sealed record ProseFamily(
    string Id,
    FactPattern Pattern,
    ProseReusePolicy ReusePolicy,
    string[] Tokens,
    ProseRendering[] Renderings);

public sealed class ProseValidationException(string message) : InvalidOperationException(message);

/// <summary>
/// Structured values supplied to an authored template. Duplicate keys and
/// malformed names fail at construction, before any prose can render.
/// </summary>
public sealed class ProseContext
{
    private static readonly Regex KeyPattern = new("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Values => _values;

    public ProseContext(IEnumerable<KeyValuePair<string, string>> values)
    {
        foreach (var (key, value) in values)
        {
            if (!KeyPattern.IsMatch(key))
                throw new ProseValidationException($"invalid prose context key '{key}'");
            if (!_values.TryAdd(key, value))
                throw new ProseValidationException($"duplicate prose context key '{key}'");
        }
    }

    public static ProseContext Of(params (string Key, string Value)[] values) =>
        new(values.Select(v => new KeyValuePair<string, string>(v.Key, v.Value)));

    public ProseContext With(params (string Key, string Value)[] values)
    {
        var merged = new Dictionary<string, string>(_values, StringComparer.Ordinal);
        foreach (var (key, value) in values) merged[key] = value;
        return new ProseContext(merged);
    }

    public bool TryGet(string key, out string value) => _values.TryGetValue(key, out value!);
}

/// <summary>One family and surface kind's observed variation in a batch.</summary>
public sealed record ProseVariationMeasure(
    string FamilyId,
    string Kind,
    int AuthoredVariants,
    int ObservedVariants,
    int Surfaces);

/// <summary>The family-aware repetition and validation result carried by WorldEval.</summary>
public sealed record ProseAuditSummary(
    int Surfaces,
    int FixedSurfaces,
    int VariableSurfaces,
    int Families,
    Dictionary<string, int> PerKind,
    Dictionary<string, int> FamilyCoverage,
    List<ProseVariationMeasure> Variation,
    List<string> Failures,
    List<string> Warnings);

/// <summary>One world's complete prose inventory for batch auditing.</summary>
public sealed record ProseWorldInventory(ulong Seed, int Tier, List<ProseSurface> Surfaces);

/// <summary>One line in the structured curation dump.</summary>
public sealed record ProseDumpRecord(
    ulong Seed,
    int Tier,
    string SourceId,
    ProseSurfaceKind Kind,
    string? FamilyId,
    string VariantId,
    string RawText,
    string NormalizedSkeleton,
    ProseReusePolicy ReusePolicy,
    string Origin);

/// <summary>
/// The five authored families in the V1-03 vertical slice. Selection is a pure
/// derivation of world seed, fact id, family id, and surface kind. No shared
/// stream is read and no selection state is stored.
/// </summary>
public static class ProseCatalog
{
    private static ProseVariant V(string id, string text) => new(id, [text]);
    private static ProseRendering R(string source, ProseSurfaceKind kind, params ProseVariant[] variants) =>
        new(source, kind, variants);

    public static readonly IReadOnlyList<ProseFamily> Families =
    [
        new ProseFamily(
            "settlement",
            new FactPattern("settlement"),
            ProseReusePolicy.Frequent,
            ["settlement", "road", "word"],
            [
                R("family.settlement.fact", ProseSurfaceKind.FactDetail,
                    V("fact-1", "A small stead under the Aegis-shrine."),
                    V("fact-2", "A small stead gathered close beneath the Aegis-shrine."),
                    V("fact-3", "A low stead keeps its roofs around the Aegis-shrine."),
                    V("fact-4", "A small stead holds the ground below the Aegis-shrine.")),
                R("family.settlement.topic", ProseSurfaceKind.Topic,
                    V("topic-1", "A small stead under the Aegis-shrine. \"We hold on. That is the whole craft of it.{road}{word}\""),
                    V("topic-2", "A small stead gathered close beneath the Aegis-shrine. \"Holding is plain work and all-day work. We do it.{road}{word}\""),
                    V("topic-3", "A low stead keeps its roofs around the Aegis-shrine. \"Roof, field, neighbor: keep those three and a stead keeps itself.{road}{word}\""),
                    V("topic-4", "A small stead holds the ground below the Aegis-shrine. \"Nothing clever in our craft. We mend, sow, and stand.{road}{word}\"")),
                R("family.settlement.rumor", ProseSurfaceKind.Rumor,
                    V("rumor-1", "They say {settlement} keeps close to its shrine and closer to its own."),
                    V("rumor-2", "Road talk calls {settlement} a small place with a long memory."),
                    V("rumor-3", "The roofs of {settlement} sit low, but the folk under them stand high."),
                    V("rumor-4", "Ask along the road for {settlement}, and every answer points toward the shrine.")),
                R("family.settlement.ledger", ProseSurfaceKind.Ledger,
                    V("ledger-1", "{settlement}: a stead under the shrine, inhabited and holding."),
                    V("ledger-2", "{settlement}: roofs, fields, and shrine kept in one account."),
                    V("ledger-3", "{settlement}: a small settled holding beneath the shrine."),
                    V("ledger-4", "{settlement}: the valley stead and its shrine-ground.")),
            ]),
        new ProseFamily(
            "waygate",
            new FactPattern("site", "waygate"),
            ProseReusePolicy.Standard,
            ["camp_state"],
            [
                R("family.waygate.fact", ProseSurfaceKind.FactDetail,
                    V("fact-1", "An arch of black iron links, older than the stones around it."),
                    V("fact-2", "Black iron links make an arch among stones that came later."),
                    V("fact-3", "An old arch stands in black linked iron, with younger stone at its feet.")),
                R("family.waygate.topic", ProseSurfaceKind.Topic,
                    V("topic-1", "An arch of black iron links, older than the stones around it.{camp_state}"),
                    V("topic-2", "Black iron links make an arch among stones that came later.{camp_state}"),
                    V("topic-3", "An old arch stands in black linked iron, with younger stone at its feet.{camp_state}")),
            ]),
        new ProseFamily(
            "goblin-grievance",
            new FactPattern("grievance", "goblin_camp"),
            ProseReusePolicy.Standard,
            ["settlement", "raided", "crowded", "order", "muster"],
            [
                R("family.goblin-grievance.fact", ProseSurfaceKind.FactDetail,
                    V("fact-1", "Goblins from the cave raid {settlement}'s stores by night."),
                    V("fact-2", "Night raiders from the goblin cave keep taking from {settlement}'s stores."),
                    V("fact-3", "The cave goblins come after dark for the stores at {settlement}.")),
                R("family.goblin-grievance.topic", ProseSurfaceKind.Topic,
                    V("topic-1", "\"Goblins from the cave raid {settlement}'s stores by night. We have fed them to keep the peace. It has not bought much peace.{raided}{crowded}{order}{muster}\""),
                    V("topic-2", "\"Night raiders from the cave keep taking from {settlement}'s stores. Bread left outside bought one quiet week at a time, until it did not.{raided}{crowded}{order}{muster}\""),
                    V("topic-3", "\"The cave goblins come after dark for the stores at {settlement}. We paid in grain for peace and learned peace can eat without filling.{raided}{crowded}{order}{muster}\"")),
            ]),
        new ProseFamily(
            "hard-winter",
            new FactPattern("event", "hard_winter"),
            ProseReusePolicy.Rare,
            ["settlement"],
            [
                R("family.hard-winter.fact", ProseSurfaceKind.FactDetail,
                    V("fact-1", "The hard winter the signs promised came down on {settlement}: snow to the sills, and the lofts feeding every mouth through it."),
                    V("fact-2", "The promised hard winter closed around {settlement}, with snow at the sills and the lofts opened for every mouth.")),
                R("family.hard-winter.topic", ProseSurfaceKind.Topic,
                    V("topic-1", "\"That winter was a fist. Snow to the sills and the fords iced shut; we fed every mouth from the lofts and burned green wood, and we are still counting what it cost us.\""),
                    V("topic-2", "\"The winter came exactly as the signs said and harder than the saying. Snow banked at every sill, the fords locked, and the lofts carried us through. We still count the measures.\"")),
            ]),
        new ProseFamily(
            "camp-cleared",
            new FactPattern("deed", "camp_cleared"),
            ProseReusePolicy.Standard,
            ["settlement"],
            [
                R("family.camp-cleared.fact", ProseSurfaceKind.FactDetail,
                    V("fact-1", "The goblin cave was emptied. {settlement}'s stores are safe."),
                    V("fact-2", "The raiders were driven from their cave, and {settlement}'s stores stand safe."),
                    V("fact-3", "The cave camp fell quiet. No raider comes from it now for {settlement}'s grain.")),
                R("family.camp-cleared.topic", ProseSurfaceKind.Topic,
                    V("topic-1", "\"The raids are ended, and everyone knows whose doing that was. {settlement} sleeps whole again.\""),
                    V("topic-2", "\"No fire shows at the cave now, and no hand bars a shutter at dusk. {settlement} sleeps through the dark again.\""),
                    V("topic-3", "\"The camp is quiet and the lofts are ours to count. Folk in {settlement} remember the hand that made it so.\"")),
            ]),
    ];

    public static ProseFamily? FamilyFor(Fact fact) => Families.SingleOrDefault(f => f.Pattern.Matches(fact));

    public static ProseContext ContextFor(World world, Fact fact)
    {
        string settlement = fact.Object.Length > 0 ? fact.Object : world.SettlementName;
        return FamilyFor(fact)?.Id switch
        {
            "settlement" => ProseContext.Of(("settlement", fact.Subject), ("road", ""), ("word", "")),
            "waygate" => ProseContext.Of(("camp_state", "")),
            "goblin-grievance" => ProseContext.Of(("settlement", settlement), ("raided", ""),
                ("crowded", ""), ("order", ""), ("muster", "")),
            "hard-winter" => ProseContext.Of(("settlement", settlement)),
            "camp-cleared" => ProseContext.Of(("settlement", settlement)),
            _ => ProseContext.Of(),
        };
    }

    public static ProseSurface Render(World world, Fact fact, ProseSurfaceKind kind, ProseContext? context = null)
    {
        var family = FamilyFor(fact)
            ?? throw new ProseValidationException($"fact {fact.Type}:{fact.Subject} has no prose family");
        context ??= ContextFor(world, fact);
        return ProseComposer.Render(world, fact, family, kind, context);
    }

    public static List<ProseSurface> RenderAll(World world, Fact fact) =>
        FamilyFor(fact) is { } family
            ? [.. family.Renderings.Select(r => Render(world, fact, r.Kind))]
            : [];

    /// <summary>Runtime families absent at generation still belong in the curation view.</summary>
    public static IEnumerable<Fact> MissingFamilySamples(World world)
    {
        int id = 900_000;
        foreach (var family in Families)
        {
            if (world.Facts.All.Any(family.Pattern.Matches)) continue;
            yield return family.Id switch
            {
                "hard-winter" => new Fact(id++, "event", "hard_winter", world.SettlementName, ""),
                "camp-cleared" => new Fact(id++, "deed", "camp_cleared", world.SettlementName, ""),
                _ => throw new ProseValidationException($"family '{family.Id}' has no generated fact or audit sample"),
            };
        }
    }

    public static List<string> Validate() => ProseAudit.ValidateFamilies(Families);
}

/// <summary>Pure rendering over one explicit family and structured context.</summary>
public static class ProseComposer
{
    public static ProseSurface Render(
        World world,
        Fact fact,
        ProseFamily family,
        ProseSurfaceKind kind,
        ProseContext context)
    {
        var rendering = family.Renderings.SingleOrDefault(r => r.Kind == kind)
            ?? throw new ProseValidationException($"family '{family.Id}' has no {kind} rendering");
        var allowed = family.Tokens.ToHashSet(StringComparer.Ordinal);
        foreach (string key in context.Values.Keys)
            if (!allowed.Contains(key))
                throw new ProseValidationException($"family '{family.Id}' received unknown value '{key}'");

        ulong choice = SeedTree.Derive(world.Seed, $"prose:{fact.Id}:{family.Id}:{kind}");
        var variant = rendering.Variants[(int)(choice % (ulong)rendering.Variants.Length)];
        string raw = string.Join(" ", variant.Parts.Select(p => Expand(p, context)));
        return new ProseSurface(rendering.SourceId, kind, family.Id, variant.Id, raw,
            ProseNormalizer.Normalize(raw, world), family.ReusePolicy, "curated-family");
    }

    private static string Expand(string template, ProseContext context)
    {
        string expanded = ProseAudit.TokenPattern.Replace(template, match =>
        {
            string key = match.Groups[1].Value;
            if (!context.TryGet(key, out string value))
                throw new ProseValidationException($"missing prose value '{key}'");
            return value;
        });
        if (ProseAudit.AnyPlaceholder.IsMatch(expanded))
            throw new ProseValidationException($"unresolved prose placeholder in '{expanded}'");
        return expanded;
    }
}

/// <summary>Normalization shared by live surfaces and the WorldEval audit.</summary>
public static class ProseNormalizer
{
    private static readonly Regex Spaces = new("\\s+", RegexOptions.CultureInvariant);

    public static string Normalize(string surface, World world)
    {
        var names = new List<(string Name, string Token)>
        {
            (world.SettlementName, "{settlement}"),
            (world.Name, "{world}"),
            (world.TownName, "{town}"),
        };
        foreach (var npc in world.Npcs) names.Add((npc.Name, "{person}"));
        foreach (var spawn in world.Sites.SelectMany(s => s.Spawns))
            if (spawn.Epithet is not null) names.Add((spawn.Epithet, "{raider}"));

        foreach (var (name, token) in names.Where(n => n.Name.Length > 0).OrderByDescending(n => n.Name.Length))
            surface = Regex.Replace(surface, $@"\b{Regex.Escape(name)}\b", token);
        return Spaces.Replace(surface.Trim(), " ");
    }

    public static string Authored(string template) =>
        Spaces.Replace(ProseAudit.TokenPattern.Replace(template.Trim(), "{value}"), " ")
            .ToLowerInvariant();

    public static string Slug(string value)
    {
        string slug = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 0 ? slug : "surface";
    }

    public static string StableTag(string value)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in value)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return (hash & 0xffffffffUL).ToString("x8");
    }
}

/// <summary>Hard validation and advisory family-aware repetition findings.</summary>
public static class ProseAudit
{
    internal static readonly Regex TokenPattern = new("\\{([a-z][a-z0-9_]*)\\}", RegexOptions.CultureInvariant);
    internal static readonly Regex AnyPlaceholder = new("\\{[^{}]+\\}", RegexOptions.CultureInvariant);

    public static int RequiredVariants(ProseReusePolicy policy) => policy switch
    {
        ProseReusePolicy.Fixed => 1,
        ProseReusePolicy.Rare => 2,
        ProseReusePolicy.Standard => 3,
        _ => 4,
    };

    public static List<string> ValidateFamilies(IEnumerable<ProseFamily> input)
    {
        var failures = new List<string>();
        var families = input.ToList();
        if (families.Any(f => f.ReusePolicy != ProseReusePolicy.Fixed)
            && !families.Any(f => f.Renderings.Select(r => r.Kind).Distinct().Count() >= 4))
            failures.Add("no variable family exercises four surface kinds");
        foreach (var duplicate in families.GroupBy(f => f.Id, StringComparer.Ordinal).Where(g => g.Count() > 1))
            failures.Add($"duplicate family id '{duplicate.Key}'");

        var sources = new HashSet<string>(StringComparer.Ordinal);
        foreach (var family in families)
        {
            if (family.Id.Length == 0) failures.Add("empty family id");
            if (family.Renderings.Length == 0) failures.Add($"family '{family.Id}' has no renderings");
            if (family.ReusePolicy != ProseReusePolicy.Fixed
                && family.Renderings.Select(r => r.Kind).Distinct().Count() < 2)
                failures.Add($"variable family '{family.Id}' renders through fewer than two surface kinds");
            if (family.Tokens.Distinct(StringComparer.Ordinal).Count() != family.Tokens.Length)
                failures.Add($"family '{family.Id}' declares duplicate context tokens");
            if (family.Renderings.Select(r => r.Kind).Distinct().Count() != family.Renderings.Length)
                failures.Add($"family '{family.Id}' declares a surface kind twice");

            foreach (var rendering in family.Renderings)
            {
                if (!sources.Add(rendering.SourceId))
                    failures.Add($"duplicate source id '{rendering.SourceId}'");
                int needed = RequiredVariants(family.ReusePolicy);
                if (rendering.Variants.Length < needed)
                    failures.Add($"family '{family.Id}' source '{rendering.SourceId}' declares {rendering.Variants.Length} variant(s), needs {needed}");
                if (family.ReusePolicy == ProseReusePolicy.Fixed && rendering.Variants.Length != 1)
                    failures.Add($"fixed family '{family.Id}' source '{rendering.SourceId}' must declare exactly one variant");
                if (rendering.Variants.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != rendering.Variants.Length)
                    failures.Add($"source '{rendering.SourceId}' declares duplicate variant ids");

                var normalized = new HashSet<string>(StringComparer.Ordinal);
                foreach (var variant in rendering.Variants)
                {
                    if (variant.Id.Length == 0) failures.Add($"source '{rendering.SourceId}' has an empty variant id");
                    if (variant.Parts.Length == 0 || variant.Parts.Any(string.IsNullOrWhiteSpace))
                        failures.Add($"source '{rendering.SourceId}' variant '{variant.Id}' is empty");
                    foreach (string part in variant.Parts)
                    {
                        foreach (Match token in TokenPattern.Matches(part))
                            if (!family.Tokens.Contains(token.Groups[1].Value, StringComparer.Ordinal))
                                failures.Add($"family '{family.Id}' variant '{variant.Id}' uses unknown token '{token.Groups[1].Value}'");
                        string stripped = TokenPattern.Replace(part, "");
                        if (AnyPlaceholder.IsMatch(stripped))
                            failures.Add($"family '{family.Id}' variant '{variant.Id}' has an invalid placeholder");
                    }
                    string skeleton = ProseNormalizer.Authored(string.Join(" ", variant.Parts));
                    if (!normalized.Add(skeleton))
                        failures.Add($"source '{rendering.SourceId}' has identical normalized variants");
                }
            }
        }
        return [.. failures.Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal)];
    }

    public static ProseAuditSummary Audit(
        IReadOnlyList<ProseWorldInventory> worlds,
        IReadOnlyList<ProseFamily>? families = null)
    {
        families ??= ProseCatalog.Families;
        var failures = ValidateFamilies(families);
        var warnings = new List<string>();
        var surfaces = worlds.SelectMany(w => w.Surfaces).ToList();

        foreach (var world in worlds)
        {
            foreach (var duplicate in world.Surfaces.GroupBy(s => s.SourceId, StringComparer.Ordinal).Where(g => g.Count() > 1))
                failures.Add($"seed {world.Seed} tier {world.Tier} emitted duplicate source '{duplicate.Key}'");
        }
        foreach (var surface in surfaces)
        {
            if (string.IsNullOrWhiteSpace(surface.RawText)) failures.Add($"source '{surface.SourceId}' rendered empty prose");
            if (surface.FamilyId is not null && AnyPlaceholder.IsMatch(surface.RawText))
                failures.Add($"source '{surface.SourceId}' left an unresolved placeholder");
        }

        foreach (var family in families.Where(f => f.ReusePolicy != ProseReusePolicy.Fixed))
            if (!surfaces.Any(s => s.FamilyId == family.Id))
                failures.Add($"declared variable family '{family.Id}' is absent from the curated catalog");

        var perKind = Enum.GetValues<ProseSurfaceKind>()
            .ToDictionary(k => k.ToString().ToLowerInvariant(), k => surfaces.Count(s => s.Kind == k), StringComparer.Ordinal);
        var familyCoverage = families.OrderBy(f => f.Id, StringComparer.Ordinal)
            .ToDictionary(f => f.Id, f => surfaces.Where(s => s.FamilyId == f.Id).Select(s => s.Kind).Distinct().Count(), StringComparer.Ordinal);
        var variation = new List<ProseVariationMeasure>();
        foreach (var family in families.OrderBy(f => f.Id, StringComparer.Ordinal))
            foreach (var rendering in family.Renderings.OrderBy(r => r.Kind))
            {
                var observed = surfaces.Where(s => s.FamilyId == family.Id && s.Kind == rendering.Kind).ToList();
                int observedVariants = observed.Select(s => s.VariantId).Distinct(StringComparer.Ordinal).Count();
                variation.Add(new ProseVariationMeasure(family.Id, rendering.Kind.ToString().ToLowerInvariant(),
                    rendering.Variants.Length, observedVariants, observed.Count));
                if (observed.Count > 0 && observedVariants < rendering.Variants.Length)
                    warnings.Add($"distribution skew: family '{family.Id}' {rendering.Kind} observed {observedVariants} of {rendering.Variants.Length} authored variants");
            }

        int collisions = surfaces.Where(s => s.FamilyId is not null)
            .GroupBy(s => s.NormalizedSkeleton, StringComparer.Ordinal)
            .Count(g => g.Select(s => s.FamilyId).Distinct(StringComparer.Ordinal).Count() > 1);
        if (collisions > 0) warnings.Add($"{collisions} normalized skeleton collision(s) cross family boundaries");

        var fixedHeavy = Enum.GetValues<ProseSurfaceKind>()
            .Where(kind => surfaces.Count(s => s.Kind == kind) > 0)
            .Where(kind => surfaces.Count(s => s.Kind == kind && s.ReusePolicy == ProseReusePolicy.Fixed)
                >= surfaces.Count(s => s.Kind == kind) * 0.8)
            .Select(k => k.ToString().ToLowerInvariant())
            .ToList();
        if (fixedHeavy.Count > 0)
            warnings.Add($"fixed prose dominates: {string.Join(", ", fixedHeavy)}");
        int legacy = surfaces.Count(s => s.Origin.StartsWith("legacy", StringComparison.Ordinal));
        if (legacy > 0) warnings.Add($"{legacy} legacy surface(s) remain outside composition by design");

        return new ProseAuditSummary(
            surfaces.Count,
            surfaces.Count(s => s.ReusePolicy == ProseReusePolicy.Fixed),
            surfaces.Count(s => s.ReusePolicy != ProseReusePolicy.Fixed),
            families.Count,
            perKind,
            familyCoverage,
            variation,
            [.. failures.Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal)],
            [.. warnings.Distinct(StringComparer.Ordinal).OrderBy(w => w, StringComparer.Ordinal)]);
    }
}

/// <summary>The two curation shapes used by `worldgen --dump`.</summary>
public static class ProseDump
{
    public static IEnumerable<string> HumanLines(IEnumerable<ProseSurface> input)
    {
        foreach (var group in input.OrderBy(s => s.FamilyId ?? "~legacy", StringComparer.Ordinal)
                     .ThenBy(s => s.SourceId, StringComparer.Ordinal)
                     .GroupBy(s => s.FamilyId ?? "legacy", StringComparer.Ordinal))
        {
            yield return $"-- family {group.Key} --";
            foreach (var surface in group)
            {
                yield return $"[{surface.Kind.ToString().ToLowerInvariant()}] {surface.SourceId} variant={surface.VariantId} reuse={surface.ReusePolicy.ToString().ToLowerInvariant()} origin={surface.Origin}";
                yield return surface.RawText;
            }
        }
    }

    public static string JsonLine(ulong seed, int tier, ProseSurface surface) =>
        JsonSerializer.Serialize(new ProseDumpRecord(seed, tier, surface.SourceId, surface.Kind,
            surface.FamilyId, surface.VariantId, surface.RawText, surface.NormalizedSkeleton,
            surface.ReusePolicy, surface.Origin), ProseJsonContext.Default.ProseDumpRecord);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProseSurface))]
[JsonSerializable(typeof(ProseDumpRecord))]
internal partial class ProseJsonContext : JsonSerializerContext;
