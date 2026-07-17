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
/// so a v3 journal that crossed would replay against different content);
/// v5 when D-036 added trade (villager talk menus gained purchase entries, so a
/// v4 journal digit that merely closed a menu could now buy something);
/// v6 when D-037 added the hollow to tier 2+ worlds and new villager topics
/// (talk-menu digits shifted, and old journals never walked a world that held
/// the stone ring);
/// v7 when D-038 added the severed hermit to tier 3+ worlds (a v6 journal that
/// walked their tile kept walking; now it would open a talk menu instead);
/// v8 when D-039 added the last stair to tier 5+ worlds and the keeping menu
/// (a v7 journal deep enough would walk tiles that now hold a stair, and a
/// digit at the Hearth now answers the arc's central question);
/// v9 when D-040 added the quarry to tier 3+ worlds and weighted story
/// selection against repeating the previous world's template (a v8 journal
/// that crossed past tier 1 could replay into a world telling a different
/// story, and deep worlds now hold a site it never walked around);
/// v10 when D-041 added gear: a smith stands at every stead at every tier
/// (a v9 journal walking their tile would open a menu that did not exist),
/// deep chests hand out iron, and 'i' plus new menu digits carry meaning.
/// v11 when D-042 added use-grown skills: swings and turned blows now change
/// later damage, and 'c' (the sheet) gained meaning as a journaled key.
/// v12 when D-044 added the fallen hall to tier 4+ worlds (a v11 journal deep
/// enough would walk tiles that now hold the hall's gate, against a pack that
/// did not exist when it was played).
/// v13 when D-045 added the laying-down menu (a post-resolution bump on a
/// severed one now opens a choice where a v12 journal recorded an attack) and
/// the compounding song fact at crossings.
/// v14 when D-046 added knacks: the sheet's digits now answer threshold
/// questions (a v13 key that merely closed the sheet could now choose one
/// forever), and chosen knacks change later damage, wind, and wear.
/// v15 when D-047 added the terms of the crossing: '>' at an open waygate now
/// opens the oath menu where a v14 '>' crossed at once, digits there swear
/// terms on the next world, and oath-bound worlds generate more tenants.
/// </summary>
public static class SaveCodec
{
    public const int Version = 15;
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
