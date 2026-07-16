using Aegis.Cli;

// aegis                          play in the terminal
// aegis --pilot [--session s]    play, with the control channel open alongside
// aegis --headless --pilot       no console at all; driven entirely via the pilot
// aegis pilot <cmd>              client: screen | keys "<keys>" | state | quit | ping
// aegis sim --seed N --keys ".." headless scripted run, JSON result on stdout

if (args.Length > 0 && args[0] == "pilot")
    return PilotClient.Run(args[1..]);

if (args.Length > 0 && args[0] == "sim")
    return SimRunner.Run(args[1..]);

ulong seed = (ulong)Environment.TickCount64;
bool pilot = false;
bool headless = false;
string session = "default";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed": seed = ulong.Parse(args[++i]); break;
        case "--pilot": pilot = true; break;
        case "--headless": headless = true; break;
        case "--session": session = args[++i]; break;
        case "--help" or "-h":
            Console.WriteLine("""
                aegis [--seed N] [--pilot] [--headless] [--session name]
                aegis pilot <screen|keys "<keys>"|state|quit|ping> [--session name]
                aegis sim --seed N --keys "<keys>" [--quiet]

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

using var renderer = headless ? null : new ConsoleRenderer();
var host = new GameHost(seed, renderer);
using var cts = new CancellationTokenSource();

PilotServer? server = null;
if (pilot)
{
    server = new PilotServer(session, host.Writer);
    server.Start();
}

if (!headless)
    ConsoleInput.Start(host.Writer, cts.Token);

await host.RunAsync();

cts.Cancel();
server?.Stop();
return 0;
