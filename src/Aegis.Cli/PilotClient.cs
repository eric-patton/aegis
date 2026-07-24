using System.Text.Json;
using Aegis.Host;

namespace Aegis.Cli;

/// <summary>
/// Client side of the pilot channel: `aegis pilot <cmd>` connects to a running
/// instance, sends one request, prints the response, exits. Built so a shell (or an
/// agent) can drive and observe a live game without touching the window.
/// </summary>
public static class PilotClient
{
    public static int Run(string[] args)
    {
        string session = "default";
        string? sub = null;
        string? keys = null;
        string? action = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--session":
                    session = args[++i];
                    break;
                default:
                    if (sub is null) sub = args[i];
                    else if (sub == "keys" && keys is null) keys = args[i];
                    else if (sub == "ui" && action is null) action = args[i];
                    else return Fail($"unexpected argument '{args[i]}'");
                    break;
            }
        }

        if (sub is null)
            return Fail("usage: aegis-tools pilot <screen|keys <keys>|state|frame|ui <action>|quit|ping> [--session name]");

        var request = sub switch
        {
            "screen" => new PilotRequest { Cmd = "screen" },
            "state" => new PilotRequest { Cmd = "state" },
            "frame" => new PilotRequest { Cmd = "frame" },
            "ui" when action is not null => new PilotRequest { Cmd = "ui", Action = action },
            "quit" => new PilotRequest { Cmd = "quit" },
            "ping" => new PilotRequest { Cmd = "ping" },
            "keys" when keys is not null => new PilotRequest { Cmd = "keys", Keys = keys },
            "keys" => null,
            _ => null,
        };
        if (request is null)
            return Fail(sub switch
            {
                "keys" => "usage: aegis pilot keys \"<keys>\"",
                "ui" => "usage: aegis pilot ui <dismiss-help|guide|compass|log|close|next|previous|activate>",
                _ => $"unknown pilot command '{sub}'",
            });

        PilotResponse? response;
        try
        {
            response = PilotConnection.Exchange(session, request);
        }
        catch (TimeoutException)
        {
            return Fail($"no game found on session '{session}' (is one running with --pilot?)");
        }

        if (response is null) return Fail("no response");
        if (!response.Ok) return Fail(response.Error ?? "unknown error");

        if (response.Screen is not null)
            foreach (var line in response.Screen)
                Console.WriteLine(line);

        if (response.State is not null && request.Cmd is "state")
            Console.WriteLine(JsonSerializer.Serialize(response.State, CliJsonPretty.Default.Snapshot));

        if (response.Frame is not null && request.Cmd is "frame")
            Console.WriteLine(PilotWire.SerializeFrame(response.Frame));

        if (request.Cmd is "keys" && response.State is not null)
        {
            var s = response.State;
            Console.WriteLine($"-- T{s.Turn} {s.Mode} @({s.X},{s.Y}) hp {s.Hp}/{s.MaxHp} st {s.Stamina}/{s.MaxStamina} " +
                              $"coin {s.Coin} ess {s.Essence}{(s.Cycle > 1 || s.Legend > 0 ? $" cyc {s.Cycle} leg {s.Legend}" : "")}" +
                              $"{(s.WoundedTurns > 0 ? $" WOUNDED({s.WoundedTurns})" : "")}" +
                              $"{(s.RemnantExists ? $" remnant@{s.RemnantMap}({s.RemnantX},{s.RemnantY})" : "")}");
        }

        if (request.Cmd is "ping" or "quit" or "ui")
            Console.WriteLine("ok");

        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"aegis-tools pilot: {message}");
        return 1;
    }
}
