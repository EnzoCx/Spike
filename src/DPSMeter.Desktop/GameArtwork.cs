using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DPSMeter.Desktop;

/// <summary>Embedded AION 2 Global artwork. No network access at runtime.
/// Skill IDs and abnormal/buff IDs are separate namespaces, never guessed from names.
/// Bitmap sources are cached and frozen; controls remain distinct for each visual tree.</summary>
internal static class GameArtwork
{
    private sealed record Atlas(int Columns, int Rows, Dictionary<int, int> Skills, Dictionary<int, int> Buffs);
    private static readonly Lazy<Atlas> Catalog = new(() => Read<Atlas>("catalog.json"));
    private static readonly Lazy<Dictionary<string, string>> Colors = new(() => Read<Dictionary<string, string>>("classes.json"));
    private static readonly Lazy<BitmapSource> Sheet = new(() => LoadImage("atlas.png"));
    private static readonly Dictionary<int, BitmapSource> Tiles = new();
    private static readonly Dictionary<string, BitmapSource> Classes = new();
    private static readonly Dictionary<string, Brush> Brushes = new();

    private static Stream Resource(string path) => Application.GetResourceStream(new Uri($"pack://application:,,,/GameArt/{path}"))!.Stream;
    private static T Read<T>(string path)
    {
        using var stream = Resource(path);
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
    private static BitmapSource LoadImage(string path)
    {
        using var stream = Resource(path);
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); return bitmap;
    }
    public static Brush ClassColor(string name)
    {
        var key = Colors.Value.ContainsKey(name) ? name : "";
        if (!Brushes.TryGetValue(key, out var brush))
        {
            brush = Themes.Brush(key.Length == 0 ? "#ADAEB9" : Colors.Value[key]); brush.Freeze(); Brushes[key] = brush;
        }
        return brush;
    }
    public static BitmapSource? ClassSource(string name)
    {
        if (!Colors.Value.ContainsKey(name)) return null;
        if (!Classes.TryGetValue(name, out var source)) Classes[name] = source = LoadImage($"classes/{name}.png");
        return source;
    }
    public static BitmapSource? SkillSource(int id)
    {
        var skills = Catalog.Value.Skills;
        if (skills.TryGetValue(id, out var exact)) return Tile(exact);
        return id is >= 11_000_000 and < 20_000_000 && skills.TryGetValue(id / 10000 * 10000, out var family) ? Tile(family) : null;
    }
    public static BitmapSource? BuffSource(int id) => Catalog.Value.Buffs.TryGetValue(id, out var index) ? Tile(index) : null;
    private static BitmapSource Tile(int index)
    {
        if (Tiles.TryGetValue(index, out var cached)) return cached;
        var atlas = Catalog.Value; var sheet = Sheet.Value;
        var size = sheet.PixelWidth / atlas.Columns;
        if (index < 0 || index >= atlas.Columns * atlas.Rows || sheet.PixelHeight != size * atlas.Rows)
            throw new InvalidDataException("Invalid embedded AION 2 atlas.");
        var tile = new CroppedBitmap(sheet, new Int32Rect(index % atlas.Columns * size, index / atlas.Columns * size, size, size));
        tile.Freeze(); Tiles[index] = tile; return tile;
    }
    public static FrameworkElement ClassIcon(string name, double size) => Icon(ClassSource(name), size, name);
    public static FrameworkElement SkillIcon(int id, string name, double size) => Icon(SkillSource(id), size, name);
    private static FrameworkElement Icon(BitmapSource? source, double size, string label)
    {
        FrameworkElement element = source is null
            ? new Border
            {
                Width = size,
                Height = size,
                BorderThickness = new Thickness(1),
                BorderBrush = ClassColor(""),
                CornerRadius = new CornerRadius(3),
                Child = new TextBlock { Text = "?", FontSize = size * .5, Foreground = ClassColor(""), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            }
            : new Image { Source = source, Width = size, Height = size, Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.HighQuality);
        System.Windows.Automation.AutomationProperties.SetName(element, label);
        return element;
    }
    internal static void Verify()
    {
        if (Colors.Value.Count != 9 || Colors.Value.Keys.Any(name => ClassSource(name) is null)) throw new InvalidDataException("Class artwork missing.");
        foreach (var index in Catalog.Value.Skills.Values.Concat(Catalog.Value.Buffs.Values).Distinct()) _ = Tile(index);
        if (!ReferenceEquals(SkillSource(13350340), SkillSource(13350341)) || SkillSource(13350340) is null)
            throw new InvalidDataException("Heart Gore variants lost their icon.");
        if (SkillSource(int.MaxValue) is not null || BuffSource(13350000) is not null || BuffSource(100) is null || ClassSource("Unknown") is not null)
            throw new InvalidDataException("Icon namespaces or missing-asset fallback failed.");
    }
}
