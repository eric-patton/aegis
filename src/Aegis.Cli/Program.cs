using Aegis.Cli;

// aegis                          play in the terminal
// aegis --pilot [--session s]    play, with the control channel open alongside
// aegis --headless --pilot       no console at all; driven entirely via the pilot
// aegis pilot <cmd>              client: screen | keys "<keys>" | state | quit | ping
// aegis sim --seed N --keys ".." headless scripted run, JSON result on stdout
// aegis journey --seed N --cycles K   autopilot: climb the ladder, report the crossings
// aegis worldgen --seeds N --tiers A-B   batch-generate worlds, chart the expressive range (D-137)

if (args.Length > 0 && args[0] == "pilot")
    return PilotClient.Run(args[1..]);

if (args.Length > 0 && args[0] == "sim")
    return SimRunner.Run(args[1..]);

if (args.Length > 0 && args[0] == "journey")
    return JourneyRunner.Run(args[1..]);

if (args.Length > 0 && args[0] == "worldgen")
    return WorldgenRunner.Run(args[1..]);

if (args.Length > 0 && args[0] == "saves")
    return ListSaves(args[1..]);

ulong seed = (ulong)Environment.TickCount64;
bool seedGiven = false;
bool pilot = false;
bool headless = false;
string session = "default";
string? saveSlot = null;
string saveDir = SaveFile.DefaultDirectory;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed": seed = ulong.Parse(args[++i]); seedGiven = true; break;
        case "--pilot": pilot = true; break;
        case "--headless": headless = true; break;
        case "--session": session = args[++i]; break;
        case "--save": saveSlot = args[++i]; break;
        case "--save-dir": saveDir = args[++i]; break;
        case "--help" or "-h":
            Console.WriteLine("""
                aegis [--seed N] [--save slot] [--pilot] [--headless] [--session name]
                aegis saves [--save-dir dir]
                aegis pilot <screen|keys "<keys>"|state|quit|ping> [--session name]
                aegis sim --seed N (--keys "<keys>" | --keys-file path) [--quiet] [--generator N]
                aegis journey --seed N --cycles K [--emit-keys] [--rogue] [--caster] [--companion] [--generator N]
                aegis journey --release [--seed N] [--json]
                aegis worldgen [--seeds N] [--start S] [--tiers A-B] [--json] [--dump] [--generator N]

                --save      play in a named slot: loads it if it exists, creates it if not;
                            every action is journaled immediately (quit any time, nothing lost)
                --pilot     open the named-pipe control channel (aegis.pilot.<session>)
                --headless  run without console rendering or input (requires --pilot)
                """);
            return 0;
        default:
            Console.Error.WriteLine($"aegis: unknown argument '{args[i]}' (try --help)");
            return 1;
    }
}

if (headless && !pilot)
{
    Console.Error.WriteLine("aegis: --headless requires --pilot (there would be no way to play)");
    return 1;
}

SaveFile? save = null;
Aegis.Core.Game game;
if (saveSlot is not null)
{
    string path;
    try
    {
        path = SaveFile.SlotPath(saveDir, saveSlot);
        save = SaveFile.Open(path, seed);
    }
    catch (Exception ex) when (ex is FormatException or ArgumentException)
    {
        Console.Error.WriteLine($"aegis: {ex.Message}");
        return 1;
    }
    catch (IOException)
    {
        Console.Error.WriteLine($"aegis: slot '{saveSlot}' is in use by a running game");
        return 1;
    }
    game = save.Game;
    if (save.Loaded && seedGiven && game.World.Seed != seed)
        Console.Error.WriteLine($"aegis: slot '{saveSlot}' already exists (seed {game.World.Seed}); ignoring --seed {seed}");
}
else
{
    game = new Aegis.Core.Game(seed);
}

using var renderer = headless ? null : new ConsoleRenderer();
var host = new GameHost(game, renderer);
using var cts = new CancellationTokenSource();

PilotServer? server = null;
if (pilot)
{
    server = new PilotServer(session, host.Writer);
    server.Start();
}

if (!headless)
{
    ConsoleInput.Start(host.Writer, cts.Token);
    ResizeWatcher.Start(host.Writer, cts.Token);
}

await host.RunAsync();

if (server is not null)
    await Task.Delay(200); // let an in-flight pilot response (e.g. the "quit" ack) flush before exit
cts.Cancel();
server?.Stop();
save?.Dispose();
return 0;

static int ListSaves(string[] rest)
{
    string dir = SaveFile.DefaultDirectory;
    for (int i = 0; i < rest.Length; i++)
        if (rest[i] == "--save-dir") dir = rest[++i];

    if (!Directory.Exists(dir))
    {
        Console.WriteLine($"No saves yet ({dir}).");
        return 0;
    }

    var files = Directory.GetFiles(dir, "*.aegis");
    if (files.Length == 0)
    {
        Console.WriteLine($"No saves yet ({dir}).");
        return 0;
    }

    foreach (var file in files.OrderByDescending(File.GetLastWriteTime))
    {
        string slot = Path.GetFileNameWithoutExtension(file);
        string when = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm");
        string detail;
        try
        {
            var (seed, generatorVersion, keys) = Aegis.Core.SaveCodec.Parse(SaveFile.ReadContent(file));
            detail = $"seed {seed}, generator {generatorVersion}, {keys.Length} actions";
        }
        catch (FormatException ex)
        {
            detail = $"unreadable: {ex.Message}";
        }
        catch (IOException)
        {
            detail = "(in use by a running game)";
        }
        Console.WriteLine($"{slot,-20} {when}  {detail}");
    }
    return 0;
}
