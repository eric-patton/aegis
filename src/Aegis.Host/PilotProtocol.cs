using System.Text.Json;
using System.Text.Json.Serialization;
using Aegis.Core;

namespace Aegis.Host;

public sealed class PilotRequest
{
    public string Cmd { get; set; } = "";
    public string? Keys { get; set; }
}

public sealed class PilotResponse
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string[]? Screen { get; set; }
    public Snapshot? State { get; set; }
    public FrameObservation? Frame { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(PilotRequest))]
[JsonSerializable(typeof(PilotResponse))]
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(FrameObservation))]
[JsonSerializable(typeof(FrameCellObservation))]
internal partial class PilotJson : JsonSerializerContext;

public static class PilotWire
{
    public const int SessionNameMaxLength = 48;
    public const int MaxKeyBatchLength = 65_536;

    public static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);

    public static string Serialize(PilotRequest request) =>
        JsonSerializer.Serialize(request, PilotJson.Default.PilotRequest);

    public static string Serialize(PilotResponse response) =>
        JsonSerializer.Serialize(response, PilotJson.Default.PilotResponse);

    public static PilotRequest? ParseRequest(string line) =>
        JsonSerializer.Deserialize(line, PilotJson.Default.PilotRequest);

    public static PilotResponse? ParseResponse(string line) =>
        JsonSerializer.Deserialize(line, PilotJson.Default.PilotResponse);

    public static string SerializeFrame(FrameObservation frame) =>
        JsonSerializer.Serialize(frame, PilotJson.Default.FrameObservation);

    public static string PipeName(string session)
    {
        ValidateSessionName(session);
        return $"aegis.pilot.{session}";
    }

    public static void ValidateSessionName(string session)
    {
        if (session.Length is < 1 or > SessionNameMaxLength)
            throw new ArgumentException($"Pilot session names must be 1-{SessionNameMaxLength} characters.");

        foreach (char c in session)
        {
            if (!char.IsLetterOrDigit(c) && c is not ('-' or '_'))
                throw new ArgumentException("Pilot session names use letters, digits, - and _ only.");
        }
    }
}
