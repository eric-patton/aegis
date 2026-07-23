using System.Text.Json.Serialization;
using Aegis.Core;

namespace Aegis.Cli;

public sealed class SimResult
{
    public ulong Seed { get; set; }
    public int KeysApplied { get; set; }
    public string[] Messages { get; set; } = [];
    public Snapshot? Final { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(SimResult))]
[JsonSerializable(typeof(JourneyReport))]
[JsonSerializable(typeof(WorldgenReport))]
internal partial class CliJsonPretty : JsonSerializerContext;
