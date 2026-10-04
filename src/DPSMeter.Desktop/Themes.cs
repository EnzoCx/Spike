using System.Windows.Media;

namespace DPSMeter.Desktop;

public sealed record Palette(string Background, string Surface, string Foreground, string Muted, string Accent, string Border);

public static class Themes
{
    public static readonly string[] Ids = ["dark", "light", "contrast"];
    public static Palette Get(string id) => id switch
    {
        "light" => new("#F3F0E8", "#E5E1D7", "#242720", "#5F6257", "#85501F", "#C7C9BE"),
        "contrast" => new("#10120F", "#1C201A", "#FCFAF2", "#E2E5D7", "#FFD398", "#929B87"),
        _ => new("#191A18", "#252722", "#F3F0E8", "#B1B1A5", "#DDA66A", "#3D4038")
    };
    public static SolidColorBrush Brush(string value) => new((Color)ColorConverter.ConvertFromString(value));
}
