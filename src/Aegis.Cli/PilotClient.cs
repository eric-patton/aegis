using System.IO.Pipes;
using System.Text;
using System.Text.Json;

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
                    else return Fail($"unexpected argument '{args[i]}'");
                    break;
            }
        }

        if (sub is null)
            return Fail("usage: aegis pilot <screen|keys <keys>|state|quit|ping> [--session name]");

        var request = sub switch
        {
            "screen" => new PilotRequest { Cmd = "screen" },
            "state" => new PilotRequest { Cmd = "state" },
            "quit" => new PilotRequest { Cmd = "quit" },
            "ping" => new PilotRequest { Cmd = "ping" },
            "keys" when keys is not null => new PilotRequest { Cmd = "keys", Keys = keys },
            "keys" => null,
            _ => null,
        };
        if (request is null)
            return Fail(sub == "keys" ? "usage: aegis pilot keys \"<keys>\"" : $"unknown pilot command '{sub}'");

        PilotResponse? response;
        try
        {
            response = Exchange(session, request);
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
            Console.WriteLine(JsonSerializer.Serialize(response.State, PilotJsonPretty.Default.Snapshot));

        if (request.Cmd is "keys" && response.State is not null)
        {
            var s = response.State;
            Console.WriteLine($"-- T{s.Turn} {s.Mode} @({s.X},{s.Y}) hp {s.Hp}/{s.MaxHp} st {s.Stamina}/{s.MaxStamina} " +
                              $"coin {s.Coin} ess {s.Essence}{(s.WoundedTurns > 0 ? $" WOUNDED({s.WoundedTurns})" : "")}" +
                              $"{(s.RemnantExists ? $" remnant@{s.RemnantMap}({s.RemnantX},{s.RemnantY})" : "")}");
        }

        if (request.Cmd is "ping" or "quit")
            Console.WriteLine("ok");

        return 0;
    }

    private static PilotResponse? Exchange(string session, PilotRequest request)
    {
        Trace("connecting");
        using var pipe = new NamedPipeClientStream(".", PilotWire.PipeName(session), PipeDirection.InOut);
        pipe.Connect(timeout: 2000);
        Trace("connected");

        // Deliberately not disposing the wrappers: AutoFlush already pushed every byte,
        // and a dispose-time flush would throw if the server (e.g. on "quit") closed first.
        // Disposing the pipe itself closes the handle.
        var reader = new StreamReader(pipe, PilotWire.Utf8NoBom, leaveOpen: true);
        var writer = new StreamWriter(pipe, PilotWire.Utf8NoBom, leaveOpen: true) { AutoFlush = true };
        Trace("streams ready");

        writer.WriteLine(PilotWire.Serialize(request));
        Trace("request written");
        string? line = reader.ReadLine();
        Trace($"response: {line ?? "<null>"}");
        return line is null ? null : PilotWire.ParseResponse(line);
    }

    private static void Trace(string message)
    {
        if (Environment.GetEnvironmentVariable("AEGIS_PILOT_TRACE") == "1")
            Console.Error.WriteLine($"[pilot-client] {message}");
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"aegis pilot: {message}");
        return 1;
    }
}
