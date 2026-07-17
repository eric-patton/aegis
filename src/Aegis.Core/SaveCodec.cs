using System.Text;

namespace Aegis.Core;

/// <summary>
/// The save format (D-012, D-028): a header plus the input journal. Because the
/// engine is deterministic and advances only on keys, seed + every applied key IS the
/// complete game state; loading replays the journal. The codec is pure string work,
/// no file I/O, so it lives in Core and tests cover it directly. Checkpoint
/// compression can be layered on later without changing what a save means.
/// Version history: v1 launch format; v2 when D-033 changed tier 2+ worldgen
/// (journals that crossed a waygate would replay into a different world);
/// v3 when D-034 added the Unbinder to every world at every tier (a v2 journal
/// walking their tile would open a talk menu that did not exist when it was played);
/// v4 when D-035 added template selection (tier 2+ worlds draw to choose a story,
/// so a v3 journal that crossed would replay against different content).
/// </summary>
public static class SaveCodec
{
    public const int Version = 4;
    private const string Magic = "AEGIS-SAVE";

    public static string EncodeHeader(ulong seed) => $"{Magic} v{Version} seed:{seed}";

    /// <summary>Parses full save-file content into seed + key journal. Throws on malformed or wrong-version content.</summary>
    public static (ulong Seed, string Keys) Parse(string content)
    {
        var lines = content.Split('\n');
        string header = lines[0].TrimEnd('\r');

        var parts = header.Split(' ');
        if (parts.Length != 3 || parts[0] != Magic)
            throw new FormatException("Not an Aegis save file.");
        if (parts[1] != $"v{Version}")
            throw new FormatException($"Save is {parts[1]}; this build reads v{Version}. No migration exists yet.");
        if (!parts[2].StartsWith("seed:") || !ulong.TryParse(parts[2]["seed:".Length..], out ulong seed))
            throw new FormatException("Save header has no readable seed.");

        var keys = new StringBuilder();
        for (int i = 1; i < lines.Length; i++)
            keys.Append(lines[i].TrimEnd('\r'));

        return (seed, keys.ToString());
    }

    /// <summary>Rebuilds a game by replaying the journal. Deterministic: this IS loading.</summary>
    public static Game Replay(ulong seed, string keys)
    {
        var game = new Game(seed);
        foreach (char key in keys)
        {
            if (!game.Running) break;
            game.ApplyKey(key);
        }
        return game;
    }
}
