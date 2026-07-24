using Aegis.Core;
using Aegis.Host;

namespace Aegis.GodotClient;

internal sealed class SpikeRuntime : IDisposable
{
    private readonly SaveFile? _save;

    public Game Game { get; }

    public SpikeRuntime(SpikeOptions options)
    {
        if (options.SaveSlot is null)
        {
            Game = new Game(options.Seed, firstWake: true);
            return;
        }

        string path = SaveFile.SlotPath(options.SaveDirectory, options.SaveSlot);
        _save = SaveFile.Open(path, options.Seed);
        Game = _save.Game;
    }

    public void Dispose() => _save?.Dispose();
}
