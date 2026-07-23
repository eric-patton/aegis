using Aegis.Core;

namespace Aegis.Host;

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

    public static SaveFile Open(string path, ulong seedIfNew)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        if (File.Exists(path))
        {
            var (seed, generatorVersion, keys) = SaveCodec.Parse(ReadContent(path));
            var game = SaveCodec.Replay(seed, generatorVersion, keys);
            var appender = new StreamWriter(path, append: true, PilotWire.Utf8NoBom) { AutoFlush = true };
            return new SaveFile(game, path, loaded: true, appender, keys.Length % KeysPerLine);
        }

        var newGame = new Game(seedIfNew, firstWake: true);
        var newAppender = new StreamWriter(path, append: true, PilotWire.Utf8NoBom) { AutoFlush = true };
        newAppender.WriteLine(SaveCodec.EncodeHeader(seedIfNew, newGame.GeneratorVersion));
        return new SaveFile(newGame, path, loaded: false, newAppender, 0);
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

    public static string ReadContent(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, PilotWire.Utf8NoBom);
        return reader.ReadToEnd();
    }

    public static string DefaultDirectory =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aegis",
            "saves");

    public static string SlotPath(string directory, string slot)
    {
        if (slot.Length is < 1 or > 48)
            throw new ArgumentException("Save slot names must be 1-48 characters.");

        foreach (char c in slot)
        {
            if (!char.IsLetterOrDigit(c) && c is not ('-' or '_'))
                throw new ArgumentException($"Save slot names use letters, digits, - and _ only (got '{slot}').");
        }

        return System.IO.Path.Combine(directory, slot + ".aegis");
    }

    public void Dispose()
    {
        Game.KeyApplied -= Append;
        _appender.Dispose();
    }
}
