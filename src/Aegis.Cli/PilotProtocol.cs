using System.Text.Json;
using System.Text.Json.Serialization;
using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The pilot wire protocol: one JSON object per line, request then response, over a
/// named pipe (aegis.pilot.&lt;session&gt;). Built for agent tooling: `screen` returns the
/// exact frame as text, `keys` injects input, `state` returns a structured snapshot.
/// </summary>
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
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(PilotRequest))]
[JsonSerializable(typeof(PilotResponse))]
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(SimResult))]
internal partial class PilotJson : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(SimResult))]
internal partial class PilotJsonPretty : JsonSerializerContext;

public sealed class SimResult
{
    public ulong Seed { get; set; }
    public int KeysApplied { get; set; }
    public string[] Messages { get; set; } = [];
    public Snapshot? Final { get; set; }
}

public static class PilotWire
{
    /// <summary>UTF-8 without BOM: a BOM emitted into a fresh, unread pipe can deadlock both ends.</summary>
    public static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);

    public static string Serialize(PilotRequest request) => JsonSerializer.Serialize(request, PilotJson.Default.PilotRequest);
    public static string Serialize(PilotResponse response) => JsonSerializer.Serialize(response, PilotJson.Default.PilotResponse);
    public static PilotRequest? ParseRequest(string line) => JsonSerializer.Deserialize(line, PilotJson.Default.PilotRequest);
    public static PilotResponse? ParseResponse(string line) => JsonSerializer.Deserialize(line, PilotJson.Default.PilotResponse);
    public static string PipeName(string session) => $"aegis.pilot.{session}";
}
