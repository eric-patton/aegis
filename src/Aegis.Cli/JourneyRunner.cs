using System.Text;
using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The ladder-climbing driver (D-062): `aegis journey --seed N --cycles K` builds a real
/// world and hands it to <see cref="JourneyPilot"/>, which plays it through the key
/// surface, world after world, up to K crossings. In each world it clears every site it
/// can reach and win (the camp that gates the arch, and the barrow, hollow, quarry, hall,
/// ringfort, and leaguer besides), then crosses. It reports what it cleared, and above
/// all what the bearer's bestiary read on either side of each arch: the bank carried
/// whole, the read softened by the harder ground (D-061). Because the pilot is a pure
/// function of state, `--seed N` reruns identically, so the crossing evidence is
/// reproducible, not a one-off hand-driven session.
///
/// It drives the same <see cref="Game"/> the shipped binary runs, through
/// <see cref="Game.ApplyKey"/> alone: no debug hook, no shortcut. Every crossing it prints
/// is a crossing the engine actually made.
/// </summary>
public static class JourneyRunner
{
    private readonly record struct Read(MonsterKind Kind, int Bank, ReadTier Tier);

    private readonly record struct SiteOutcome(string Name, bool Cleared, bool Skipped);

    private sealed record Crossing(
        int FromCycle, string FromWorld, int ToCycle, string ToWorld,
        int Turn, int DeathsInWorld,
        IReadOnlyList<SiteOutcome> Sites, IReadOnlyList<Read> Before, IReadOnlyList<Read> After);

    public static int Run(string[] args)
    {
        ulong seed = 42;
        int cycles = 3;
        int maxKeys = 400000;
        int perWorldBudget = 60000;
        int siteKeyBudget = 3000;   // in-site keys spent on one site before writing it off.
        int siteDeathBudget = 8;    // deaths at one site before writing it off.
        bool emitKeys = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed": seed = ulong.Parse(args[++i]); break;
                case "--cycles": cycles = int.Parse(args[++i]); break;
                case "--max-keys": maxKeys = int.Parse(args[++i]); break;
                case "--per-world": perWorldBudget = int.Parse(args[++i]); break;
                case "--site-keys": siteKeyBudget = int.Parse(args[++i]); break;
                case "--site-deaths": siteDeathBudget = int.Parse(args[++i]); break;
                case "--emit-keys": emitKeys = true; break;
                default:
                    Console.Error.WriteLine($"aegis journey: unexpected argument '{args[i]}'");
                    return 1;
            }
        }

        var game = new Game(seed);
        int targetCycle = game.Cycle + cycles;
        var keys = new StringBuilder();
        var crossings = new List<Crossing>();

        // Per-world bookkeeping: which sites we have given up on, and how much each has
        // cost so far. All of it resets at a crossing, because the next world's sites are
        // freshly generated (and may reuse an id).
        var skip = new HashSet<string>();
        var siteKeys = new Dictionary<string, int>();
        var siteDeaths = new Dictionary<string, int>();

        int totalKeys = 0;
        int keysThisWorld = 0;
        int prevDeaths = 0;
        int deathsThisWorld = 0;
        string stop;

        while (true)
        {
            if (!game.Running) { stop = "the bearer fell for good (the run ended)"; break; }
            if (game.Cycle >= targetCycle) { stop = $"reached the target of {cycles} crossing(s)"; break; }
            if (totalKeys >= maxKeys) { stop = $"hit the {maxKeys}-key safety cap"; break; }
            if (keysThisWorld >= perWorldBudget)
            {
                stop = $"stuck in cycle {game.Cycle} (tier {game.World.Tier}) after {keysThisWorld} keys, {Where(game)}";
                break;
            }

            int cycleBefore = game.Cycle;
            string worldBefore = game.World.Name;
            var beforeReads = Bestiary(game, cycleBefore);
            var sitesBefore = SiteStates(game, skip);

            // The site the bot is fighting in right now, if any (the camp is never given
            // up: a crossing needs it, and it is always winnable). Budget it here, before
            // asking the pilot, so a fresh skip is honored on the very same tick.
            string? activeSite = game.Mode == MapMode.Site && game.CurrentSite is { } cs
                && !cs.Cleared && !skip.Contains(cs.Id) && cs.Kind != SiteKind.GoblinCamp
                    ? cs.Id : null;
            if (activeSite is not null)
            {
                int k = siteKeys.GetValueOrDefault(activeSite) + 1;
                siteKeys[activeSite] = k;
                if (k > siteKeyBudget) skip.Add(activeSite);
            }

            char? key = JourneyPilot.NextKey(game, skip);
            if (key is null)
            {
                // Cannot win or even reach a foe here: write the site off and move on.
                // (skip.Add returns false if it was already written off, meaning we cannot
                // even reach the ladder to leave: that, and any dead end in the camp or on
                // the overworld, is genuinely terminal.)
                if (game.Mode == MapMode.Site && game.CurrentSite is { } deadSite
                    && deadSite.Kind != SiteKind.GoblinCamp && skip.Add(deadSite.Id))
                    continue;
                stop = $"no move available in cycle {game.Cycle} (tier {game.World.Tier}), {Where(game)}";
                break;
            }

            game.ApplyKey(key.Value);
            keys.Append(key.Value);
            totalKeys++;
            keysThisWorld++;

            if (game.Player.Deaths > prevDeaths)
            {
                int d = game.Player.Deaths - prevDeaths;
                deathsThisWorld += d;
                prevDeaths = game.Player.Deaths;
                if (activeSite is not null)
                {
                    int sd = siteDeaths.GetValueOrDefault(activeSite) + d;
                    siteDeaths[activeSite] = sd;
                    if (sd > siteDeathBudget) skip.Add(activeSite);
                }
            }

            if (game.Cycle > cycleBefore)
            {
                crossings.Add(new Crossing(
                    cycleBefore, worldBefore, game.Cycle, game.World.Name,
                    game.Turn, deathsThisWorld, sitesBefore, beforeReads, Bestiary(game, game.Cycle)));
                keysThisWorld = 0;
                deathsThisWorld = 0;
                skip.Clear();
                siteKeys.Clear();
                siteDeaths.Clear();
            }
        }

        Report(seed, cycles, crossings, stop, game, totalKeys, keys, emitKeys);
        return 0;
    }

    /// <summary>The bearer's read of every known kind at a given tier: the bank, and what it reads to here.</summary>
    private static List<Read> Bestiary(Game game, int tier) =>
        game.Player.Reads.Keys
            .OrderBy(k => (int)k)
            .Select(k => new Read(k, game.Player.Reads[k], game.Player.ReadOf(k, tier)))
            .ToList();

    /// <summary>Every tenanted site in the current world, and how the bot left it.</summary>
    private static List<SiteOutcome> SiteStates(Game game, IReadOnlySet<string> skip) =>
        game.World.Sites
            .Where(s => s.Spawns.Count > 0)
            .OrderBy(s => (int)s.Kind)
            .Select(s => new SiteOutcome(ShortSite(s.Kind), s.Cleared, skip.Contains(s.Id)))
            .ToList();

    private static string ShortSite(SiteKind kind) =>
        kind == SiteKind.GoblinCamp ? "camp" : kind.ToString().ToLowerInvariant();

    private static string Where(Game game) =>
        game.Mode == MapMode.Site
            ? $"underground with {game.LiveMonstersHere.Count()} foe(s) standing"
            : $"on the overworld at ({game.Player.Pos.X},{game.Player.Pos.Y})";

    private static void Report(
        ulong seed, int cycles, List<Crossing> crossings, string stop,
        Game game, int totalKeys, StringBuilder keys, bool emitKeys)
    {
        var w = Console.Out;
        w.WriteLine($"AEGIS JOURNEY   seed {seed}   target {cycles} crossing(s)");
        w.WriteLine(new string('=', 62));

        if (crossings.Count == 0)
            w.WriteLine("  no crossing was made.");

        foreach (var c in crossings)
        {
            w.WriteLine();
            w.WriteLine($"crossing {crossings.IndexOf(c) + 1}: cycle {c.FromCycle} \"{c.FromWorld}\" (tier {c.FromCycle}) "
                        + $"-> cycle {c.ToCycle} \"{c.ToWorld}\" (tier {c.ToCycle})   [turn {c.Turn}]");

            string cleared = string.Join(", ", c.Sites.Where(s => s.Cleared).Select(s => s.Name));
            string standing = string.Join(", ", c.Sites.Where(s => !s.Cleared).Select(s => s.Name));
            w.WriteLine($"  sites cleared: {(cleared.Length == 0 ? "none" : cleared)}"
                        + (standing.Length == 0 ? "" : $"; left standing: {standing}"));
            w.WriteLine($"  {c.DeathsInWorld} death(s) in that world.");

            if (c.Before.Count == 0)
            {
                w.WriteLine("  bestiary: empty (nothing was read in that world).");
                continue;
            }
            w.WriteLine("  bestiary across the arch (bank carries whole; read may soften):");
            var afterByKind = c.After.ToDictionary(r => r.Kind);
            foreach (var b in c.Before)
            {
                var a = afterByKind.TryGetValue(b.Kind, out var aa) ? aa : b;
                string note = a.Tier < b.Tier ? "softened"
                            : a.Tier > b.Tier ? "sharpened"
                            : "held";
                string bankNote = a.Bank == b.Bank ? $"bank {b.Bank} (unchanged)" : $"bank {b.Bank}->{a.Bank}";
                w.WriteLine($"    {b.Kind.ToString().ToLowerInvariant(),-8} {bankNote,-20} {b.Tier,-4} -> {a.Tier,-4}  ({note})");
            }
        }

        w.WriteLine();
        w.WriteLine(new string('-', 62));
        w.WriteLine($"OUTCOME: reached cycle {game.Cycle} (tier {game.World.Tier}), {crossings.Count} crossing(s) made.");
        w.WriteLine($"         {stop}.");
        w.WriteLine($"         {totalKeys} keys pressed, {game.Turn} turns, {game.Player.Deaths} death(s) total.");
        w.WriteLine("         a seeded journey replays identically: the pilot reads only game state.");
        if (emitKeys)
        {
            w.WriteLine();
            w.WriteLine($"keys ({keys.Length}): {keys}");
        }
    }
}
