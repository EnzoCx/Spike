using System.Windows.Media;

namespace Spike.Desktop;

public sealed record Palette(string Background, string Surface, string Foreground, string Muted, string Accent, string Border,
    string Sidebar, string Hover, string Brand);

public static class Themes
{
    public static readonly string[] Ids = ["dark", "light", "contrast"];
    public static Palette Get(string id) => id switch
    {
        "light" => new("#FCFCFB", "#F0F0EE", "#252526", "#646467", "#303033", "#DEDEDC", "#F3F3F1", "#E7E7E5", "#85501F"),
        "contrast" => new("#111113", "#242427", "#FAFAF8", "#DEDEDF", "#FAFAF8", "#909095", "#151517", "#353539", "#FFD398"),
        _ => new("#202022", "#2B2B2E", "#ECECEE", "#ADADB2", "#E4E4E7", "#39393D", "#18181A", "#353539", "#DDA66A")
    };
    public static SolidColorBrush Brush(string value) => new((Color)ColorConverter.ConvertFromString(value));
}
