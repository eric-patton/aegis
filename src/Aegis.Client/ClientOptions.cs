using Aegis.Host;

namespace Aegis.Client;

public sealed class ClientOptions
{
    public ulong Seed { get; private set; } = (ulong)Environment.TickCount64;
    public bool SeedGiven { get; private set; }
    public bool Pilot { get; private set; }
    public bool Headless { get; private set; }
    public string Session { get; private set; } = "default";
    public string? SaveSlot { get; private set; }
    public string SaveDirectory { get; private set; } = SaveFile.DefaultDirectory;
    public int? FontScaleOverride { get; private set; }

    public static ClientOptions Parse(string[] args)
    {
        var result = new ClientOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed":
                    result.Seed = ulong.Parse(Next(args, ref i, "--seed"));
                    result.SeedGiven = true;
                    break;
                case "--pilot":
                    result.Pilot = true;
                    break;
                case "--headless":
                    result.Headless = true;
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
                case "--font-scale":
                    result.FontScaleOverride = int.Parse(Next(args, ref i, "--font-scale"));
                    if (result.FontScaleOverride is not (1 or 2))
                        throw new ArgumentException("--font-scale must be 1 or 2.");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'.");
            }
        }

        if (result.Headless && !result.Pilot)
            throw new ArgumentException("--headless requires --pilot.");

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
