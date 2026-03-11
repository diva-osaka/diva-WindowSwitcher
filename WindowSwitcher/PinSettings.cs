using System.IO;
using System.Text.Json;

namespace WindowSwitcher;

public class PinSettings
{
    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory, "settings.json");

    public List<string> PinnedNames { get; set; } = [];
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }

    public static PinSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new PinSettings();

        var json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<PinSettings>(json) ?? new PinSettings();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
