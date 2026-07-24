using System.Text.Json;
using System.Text.Json.Serialization;
using Aegis.Host;

namespace Aegis.Client;

public sealed class PresentationSettings
{
    public int FontScale { get; set; } = 1;
    public bool HelpSeen { get; set; }
    public bool GuideSeen { get; set; }

    [JsonIgnore]
    public string Path { get; private set; } = DefaultPath;

    public static string DefaultPath =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aegis",
            "presentation.json");

    public static PresentationSettings Load(string? path = null)
    {
        path ??= DefaultPath;
        PresentationSettings settings;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path, PilotWire.Utf8NoBom);
            settings = JsonSerializer.Deserialize(
                json,
                PresentationJson.Default.PresentationSettings) ?? new PresentationSettings();
        }
        else
        {
            settings = new PresentationSettings();
        }

        settings.Path = path;
        settings.FontScale = Math.Clamp(settings.FontScale, 1, 2);
        return settings;
    }

    public void Save()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        string json = JsonSerializer.Serialize(
            this,
            PresentationJson.Default.PresentationSettings);
        File.WriteAllText(Path, json, PilotWire.Utf8NoBom);
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(PresentationSettings))]
internal partial class PresentationJson : JsonSerializerContext;
