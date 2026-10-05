namespace Spike.Core;

public static class ApplicationData
{
    private static readonly Lazy<string> Location = new(() => Migrate(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
    public static string Root => Location.Value;

    // Copy once, preserve originals, and retry on the next launch if storage is unavailable.
    public static string Migrate(string localData)
    {
        var root = Path.Combine(localData, "Spike");
        var legacy = Path.Combine(localData, "DPSMeter");
        var marker = Path.Combine(root, "migration-complete");
        if (!Directory.Exists(legacy) || File.Exists(marker)) return root;
        try
        {
            Directory.CreateDirectory(root);
            CopyIfMissing(Path.Combine(legacy, "settings.json"), Path.Combine(root, "settings.json"));
            var fights = Path.Combine(legacy, "fights");
            if (Directory.Exists(fights))
            {
                Directory.CreateDirectory(Path.Combine(root, "fights"));
                foreach (var file in Directory.EnumerateFiles(fights, "*.json"))
                    CopyIfMissing(file, Path.Combine(root, "fights", Path.GetFileName(file)));
            }
            File.WriteAllText(marker, "1");
            return root;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return legacy; }
    }

    private static void CopyIfMissing(string source, string target)
    {
        if (!File.Exists(source) || File.Exists(target)) return;
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.Copy(source, temporary); File.Move(temporary, target, false); }
        finally { File.Delete(temporary); }
    }
}
