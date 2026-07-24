using System.Text.Json;
using Aegis.Host;
using Godot;

namespace Aegis.GodotClient;

internal sealed class GodotPresentationSettings
{
    private readonly string _path;

    public bool LightTheme { get; set; }
    public int ScaleIndex { get; set; }
    public bool IronRoseOpen { get; set; }
    public NormalizedFloatingPosition IronRosePosition { get; set; } =
        NormalizedFloatingPosition.Default;

    private GodotPresentationSettings(string path)
    {
        _path = path;
    }

    public static GodotPresentationSettings Load()
    {
        string path = ProjectSettings.GlobalizePath("user://presentation.json");
        try
        {
            if (File.Exists(path))
            {
                SettingsData? data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path));
                return new GodotPresentationSettings(path)
                {
                    LightTheme = data?.LightTheme ?? false,
                    ScaleIndex = Math.Clamp(data?.ScaleIndex ?? 0, 0, 4),
                    IronRoseOpen = data?.Version >= 1 && data.IronRoseOpen,
                    IronRosePosition = data?.Version >= 1
                        ? new NormalizedFloatingPosition(
                            Math.Clamp(data.IronRoseX, 0, 1),
                            Math.Clamp(data.IronRoseY, 0, 1))
                        : NormalizedFloatingPosition.Default,
                };
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            GD.PushWarning($"Presentation settings could not be loaded: {ex.Message}");
        }

        return new GodotPresentationSettings(path);
    }

    public void Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);
            if (directory is not null)
                Directory.CreateDirectory(directory);
            File.WriteAllText(
                _path,
                JsonSerializer.Serialize(
                    new SettingsData(
                        1,
                        LightTheme,
                        Math.Clamp(ScaleIndex, 0, 4),
                        IronRoseOpen,
                        Math.Clamp(IronRosePosition.X, 0, 1),
                        Math.Clamp(IronRosePosition.Y, 0, 1)),
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Presentation settings could not be saved: {ex.Message}");
        }
    }

    private sealed record SettingsData(
        int Version,
        bool LightTheme,
        int ScaleIndex,
        bool IronRoseOpen,
        float IronRoseX,
        float IronRoseY);
}
