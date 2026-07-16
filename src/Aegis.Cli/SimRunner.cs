using System.Text.Json;
using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// Headless scripted run: `aegis sim --seed N --keys "..."` builds a world, applies
/// the key script synchronously, and prints the full message log plus final snapshot
/// as JSON. The bedrock for balance sweeps, regression tests, and CI.
/// </summary>
public static class SimRunner
{
    public static int Run(string[] args)
    {
        ulong seed = 1;
        string keys = "";
        bool quiet = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed": seed = ulong.Parse(args[++i]); break;
                case "--keys": keys = args[++i]; break;
                case "--quiet": quiet = true; break;
                default:
                    Console.Error.WriteLine($"aegis sim: unexpected argument '{args[i]}'");
                    return 1;
            }
        }

        var game = new Game(seed);
        int applied = 0;
        foreach (char key in keys)
        {
            if (!game.Running) break;
            game.ApplyKey(key);
            applied++;
        }

        var result = new SimResult
        {
            Seed = seed,
            KeysApplied = applied,
            Messages = quiet ? [] : game.Log.Entries.Select(e => $"[T{e.Turn}] {e.Text}").ToArray(),
            Final = game.TakeSnapshot(),
        };

        Console.WriteLine(JsonSerializer.Serialize(result, PilotJsonPretty.Default.SimResult));
        return 0;
    }
}
