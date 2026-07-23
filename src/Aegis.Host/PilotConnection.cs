using System.IO.Pipes;

namespace Aegis.Host;

public static class PilotConnection
{
    public static PilotResponse? Exchange(
        string session,
        PilotRequest request,
        int connectTimeoutMilliseconds = 2_000)
    {
        using var pipe = new NamedPipeClientStream(
            ".",
            PilotWire.PipeName(session),
            PipeDirection.InOut,
            PipeOptions.CurrentUserOnly);
        pipe.Connect(connectTimeoutMilliseconds);

        var reader = new StreamReader(pipe, PilotWire.Utf8NoBom, leaveOpen: true);
        var writer = new StreamWriter(pipe, PilotWire.Utf8NoBom, leaveOpen: true)
        {
            AutoFlush = true,
        };

        writer.WriteLine(PilotWire.Serialize(request));
        string? line = reader.ReadLine();
        return line is null ? null : PilotWire.ParseResponse(line);
    }
}
