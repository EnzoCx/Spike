using System.IO;
using System.Text.Json;

namespace DPSMeter.Desktop;

public sealed record Preferences(string Language = "fr", string Theme = "dark", bool AlwaysOnTop = false,
    string PlayerName = "", bool AutoStart = true, double OverlayOpacity = 0.94,
    double OverlayLeft = 40, double OverlayTop = 80, double OverlayWidth = 460, double OverlayHeight = 504,
    bool ShowOverlayOnStartup = true, bool OverlayAutoFit = true, bool OverlayCompact = false, bool OverlaySnapToEdges = true,
    bool OverlayFadeWhenIdle = true, bool OverlayDiscreet = true)
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DPSMeter", "settings.json");

    public static Preferences Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var value = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath));
            return value is not null && Text.Languages.Contains(value.Language) && Themes.Ids.Contains(value.Theme) ? value : new();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, FilePath, overwrite: true);
    }
}
