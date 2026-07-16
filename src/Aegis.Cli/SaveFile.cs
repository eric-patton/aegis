using System.Text;
using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// One save slot: an append-only journal file. Every key the engine applies is
/// appended and flushed immediately, so death consequences are durable the instant
/// they happen (D-012's autosave-on-death, by construction) and a crash loses at
/// most the final keystroke.
/// </summary>
public sealed class SaveFile : IDisposable
{
    private const int KeysPerLine = 64;

    private readonly StreamWriter _appender;
    private int _keysOnLine;

    public Game Game { get; }
    public string Path { get; }
    public bool Loaded { get; }

    private SaveFile(Game game, string path, bool loaded, StreamWriter appender, int keysOnLine)
    {
        Game = game;
        Path = path;
        Loaded = loaded;
        _appender = appender;
        _keysOnLine = keysOnLine;
        Game.KeyApplied += Append;
    }

    /// <summary>Loads the slot if it exists (replaying its journal), otherwise creates it with the given seed.</summary>
    public static SaveFile Open(string path, ulong seedIfNew)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        if (File.Exists(path))
        {
            var (seed, keys) = SaveCodec.Parse(ReadContent(path));
            var game = SaveCodec.Replay(seed, keys);
            var appender = new StreamWriter(path, append: true, PilotWire.Utf8NoBom) { AutoFlush = true };
            return new SaveFile(game, path, loaded: true, appender, keysOnLine: keys.Length % KeysPerLine);
        }
        else
        {
            var game = new Game(seedIfNew);
            var appender = new StreamWriter(path, append: true, PilotWire.Utf8NoBom) { AutoFlush = true };
            appender.WriteLine(SaveCodec.EncodeHeader(seedIfNew));
            return new SaveFile(game, path, loaded: false, appender, keysOnLine: 0);
        }
    }

    private void Append(char key)
    {
        _appender.Write(key);
        if (++_keysOnLine >= KeysPerLine)
        {
            _appender.WriteLine();
            _keysOnLine = 0;
        }
    }

    /// <summary>Reads journal content while tolerating a concurrent writer (a running game).</summary>
    public static string ReadContent(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, PilotWire.Utf8NoBom);
        return reader.ReadToEnd();
    }

    public static string DefaultDirectory =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aegis", "saves");

    public static string SlotPath(string directory, string slot)
    {
        foreach (char c in slot)
            if (!char.IsLetterOrDigit(c) && c is not ('-' or '_'))
                throw new ArgumentException($"Save slot names use letters, digits, - and _ only (got '{slot}').");
        return System.IO.Path.Combine(directory, slot + ".aegis");
    }

    public void Dispose()
    {
        Game.KeyApplied -= Append;
        _appender.Dispose();
    }
}
