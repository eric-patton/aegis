using Aegis.Host;

namespace Aegis.GodotClient;

internal sealed class SpikeOptions
{
    public ulong Seed { get; private set; } = (ulong)Environment.TickCount64;
    public bool Pilot { get; private set; }
    public string Session { get; private set; } = "godot-spike";
    public string? SaveSlot { get; private set; }
    public string SaveDirectory { get; private set; } = SaveFile.DefaultDirectory;
    public string Theme { get; private set; } = "dark";
    public bool ThemeSpecified { get; private set; }

    public static SpikeOptions Parse(string[] args)
    {
        var result = new SpikeOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed":
                    result.Seed = ulong.Parse(Next(args, ref i, "--seed"));
                    break;
                case "--pilot":
                    result.Pilot = true;
                    break;
                case "--session":
                    result.Session = Next(args, ref i, "--session");
                    break;
                case "--save":
                    result.SaveSlot = Next(args, ref i, "--save");
                    break;
                case "--no-save":
                    result.SaveSlot = null;
                    break;
                case "--save-dir":
                    result.SaveDirectory = Next(args, ref i, "--save-dir");
                    break;
                case "--theme":
                    result.Theme = Next(args, ref i, "--theme").ToLowerInvariant();
                    result.ThemeSpecified = true;
                    if (result.Theme is not ("dark" or "light"))
                        throw new ArgumentException("--theme must be dark or light.");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        PilotWire.ValidateSessionName(result.Session);
        if (result.SaveSlot is not null)
            _ = SaveFile.SlotPath(result.SaveDirectory, result.SaveSlot);
        return result;
    }

    private static string Next(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"{option} requires a value.");
        return args[++index];
    }
}
