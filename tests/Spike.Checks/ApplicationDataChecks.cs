using System.IO;
using Spike.Core;

internal static class ApplicationDataChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var folder = Path.Combine(Path.GetTempPath(), "Spike-migration-" + Guid.NewGuid().ToString("N"));
        var legacy = Path.Combine(folder, "DPSMeter");
        var root = Path.Combine(folder, "Spike");
        try
        {
            check(ApplicationData.Migrate(folder) == root && !Directory.Exists(root), "Fresh installations use Spike without creating legacy data");
            Directory.CreateDirectory(Path.Combine(legacy, "fights"));
            File.WriteAllText(Path.Combine(legacy, "settings.json"), "original preferences");
            File.WriteAllText(Path.Combine(legacy, "fights", "fixture.json"), "original fight");
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "settings.json"), "new preferences");
            check(ApplicationData.Migrate(folder) == root, "Migrate to Spike storage");
            check(File.ReadAllText(Path.Combine(root, "settings.json")) == "new preferences", "Migration preserves existing Spike preferences");
            check(File.ReadAllText(Path.Combine(root, "fights", "fixture.json")) == "original fight" &&
                File.Exists(Path.Combine(legacy, "fights", "fixture.json")), "Migration copies history and preserves originals");
            File.Delete(Path.Combine(root, "fights", "fixture.json"));
            ApplicationData.Migrate(folder);
            check(!File.Exists(Path.Combine(root, "fights", "fixture.json")), "Completed migration does not resurrect removed history");
            File.Delete(Path.Combine(root, "migration-complete"));
            using (var held = new FileStream(Path.Combine(legacy, "fights", "fixture.json"), FileMode.Open, FileAccess.Read, FileShare.None))
                check(ApplicationData.Migrate(folder) == legacy && !File.Exists(Path.Combine(root, "migration-complete")), "Interrupted migration falls back to legacy storage without marking completion");
            check(ApplicationData.Migrate(folder) == root && File.Exists(Path.Combine(root, "fights", "fixture.json")), "Retry completes migration after a storage failure");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
