namespace Aegis.Core;

/// <summary>The hook points storylets can fire on. See design/storylets.md sec 3.</summary>
public enum StoryletTrigger { Arrival, EnterTile, NearHouse, Rest, DeedWritten, AmbientTurn, Talk }

/// <summary>World-scoped storylets become eligible again after a crossing; Character-scoped never do.</summary>
public enum StoryletScope { World, Character }

/// <summary>
/// A declarative match against the fact graph: null fields are wildcards. The first
/// matching fact per Requires pattern is captured for line templates ({r0.detail} etc).
/// </summary>
public sealed record FactPattern(string Type, string? Subject = null, string? Object = null)
{
    public bool Matches(Fact f) =>
        f.Type == Type
        && (Subject is null || f.Subject == Subject)
        && (Object is null || f.Object == Object);
}

/// <summary>
/// One atomic, precondition-gated narrative beat (D-021, format spec in
/// design/storylets.md). Pure data except When and Effect, which stay small so the
/// format maps 1:1 onto external data files later.
/// </summary>
public sealed record Storylet
{
    public required string Id { get; init; }
    public required StoryletTrigger Trigger { get; init; }
    public Terrain? Tile { get; init; }
    public StoryletScope Scope { get; init; } = StoryletScope.World;
    public bool Once { get; init; } = true;
    public int CooldownTurns { get; init; }
    public int Weight { get; init; } = 10;

    /// <summary>
    /// Selection tier: only the highest-priority eligible candidates enter the
    /// weighted draw. Dramatic story beats (template-emitted) outrank asides and
    /// flavor so a plot moment is never lost to an ambient line.
    /// </summary>
    public int Priority { get; init; }
    public FactPattern[] Requires { get; init; } = [];
    public FactPattern[] Forbids { get; init; } = [];
    public Func<Game, bool>? When { get; init; }
    public required (string Text, LogTone Tone)[] Lines { get; init; }
    public Action<Game>? Effect { get; init; }

    /// <summary>
    /// The scene this storylet opens when it fires (D-117): the beat plays as a
    /// modal dialogue tree instead of log lines alone. Gating is unchanged; the
    /// scene is delivery, exactly as design/storylets.md sec 6 promised.
    /// </summary>
    public Scene? Scene { get; init; }
}

/// <summary>
/// Selection and lifecycle: collects eligible candidates at a hook, picks one by
/// weight from a dedicated per-world RNG stream, lands its lines, runs its effect,
/// and records the firing. Storylets are additive to simulation v1 (lines, facts,
/// small grants), which is what keeps pre-storylet save journals replaying unchanged.
/// </summary>
public sealed class StoryletEngine
{
    private const double AmbientChance = 0.04;

    private IReadOnlyList<Storylet> _catalog;
    private readonly HashSet<string> _firedCharacter = [];
    private readonly HashSet<string> _firedWorld = [];
    private readonly Dictionary<string, int> _lastFiredTurn = [];
    private Rng _rng;

    public int TotalFired { get; private set; }

    public StoryletEngine(ulong worldSeed, IReadOnlyList<Storylet> catalog)
    {
        _catalog = catalog;
        _rng = new Rng(SeedTree.Derive(worldSeed, "storylets"));
    }

    /// <summary>
    /// World-scoped eligibility resets, the stream re-derives from the new world, and
    /// the catalog swaps to the new world's (global content plus its compiled story).
    /// Character-scoped history survives.
    /// </summary>
    public void OnCrossing(ulong newWorldSeed, IReadOnlyList<Storylet> catalog)
    {
        _catalog = catalog;
        _firedWorld.Clear();
        _rng = new Rng(SeedTree.Derive(newWorldSeed, "storylets"));
    }

    public bool TryFire(Game game, StoryletTrigger trigger, Terrain? tile = null)
    {
        // The ambient die rolls every eligible turn whether or not content exists,
        // so the draw sequence stays stable as the catalog grows.
        if (trigger == StoryletTrigger.AmbientTurn && !_rng.Chance(AmbientChance))
            return false;

        var candidates = new List<(Storylet S, List<Fact> Captures)>();
        foreach (var s in _catalog)
        {
            if (s.Trigger != trigger) continue;
            if (s.Tile is { } t && t != tile) continue;

            var fired = s.Scope == StoryletScope.Character ? _firedCharacter : _firedWorld;
            if (s.Once && fired.Contains(s.Id)) continue;
            if (s.CooldownTurns > 0 && _lastFiredTurn.TryGetValue(s.Id, out int last)
                && game.Turn - last < s.CooldownTurns) continue;

            if (s.Forbids.Any(p => game.World.Facts.All.Any(p.Matches))) continue;

            var captures = new List<Fact>();
            bool requirementsHold = true;
            foreach (var pattern in s.Requires)
            {
                var match = game.World.Facts.All.FirstOrDefault(pattern.Matches);
                if (match is null) { requirementsHold = false; break; }
                captures.Add(match);
            }
            if (!requirementsHold) continue;
            if (s.When is not null && !s.When(game)) continue;

            candidates.Add((s, captures));
        }

        if (candidates.Count == 0) return false;

        int top = candidates.Max(c => c.S.Priority);
        candidates.RemoveAll(c => c.S.Priority < top);

        int roll = _rng.Next(candidates.Sum(c => c.S.Weight));
        var chosen = candidates[0];
        foreach (var candidate in candidates)
        {
            roll -= candidate.S.Weight;
            if (roll < 0) { chosen = candidate; break; }
        }

        Fire(game, chosen.S, chosen.Captures);
        return true;
    }

    private void Fire(Game game, Storylet s, List<Fact> captures)
    {
        foreach (var (text, tone) in s.Lines)
            game.Log.Add(game.Turn, Expand(text, game, captures), tone);
        s.Effect?.Invoke(game);
        if (s.Scene is not null) game.OpenScene(s.Scene, captures);

        (s.Scope == StoryletScope.Character ? _firedCharacter : _firedWorld).Add(s.Id);
        _lastFiredTurn[s.Id] = game.Turn;
        TotalFired++;
    }

    internal static string Expand(string text, Game game, List<Fact> captures)
    {
        text = text
            .Replace("{settlement}", game.World.SettlementName)
            .Replace("{world}", game.World.Name);
        for (int i = 0; i < captures.Count; i++)
            text = text
                .Replace($"{{r{i}.subject}}", captures[i].Subject)
                .Replace($"{{r{i}.object}}", captures[i].Object)
                .Replace($"{{r{i}.detail}}", captures[i].Detail);
        return text;
    }
}
