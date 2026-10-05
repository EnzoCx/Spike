using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Spike.Desktop.Updates;

// Offline executable fixture: never creates UI, starts capture or contacts GitHub.
if (args.Length == 3 && args[0] == "--apply-update")
    return await AutomaticUpdater.ApplyAsync(args[1], args[2]);

if (args.Length == 3 && args[0] == "--stage")
{
    var store = AutomaticUpdater.Store(args[1]);
    using var updateLock = store.AcquireLock();
    File.Copy(args[2], store.Payload, true);
    var version = FileVersionInfo.GetVersionInfo(args[2]).FileVersion!;
    var tag = "v" + version;
    using var payload = File.OpenRead(store.Payload);
    var release = JsonSerializer.Serialize(new
    {
        tag_name = tag,
        draft = false,
        prerelease = false,
        assets = new[] { new { name = "Spike.exe", state = "uploaded", size = payload.Length,
            digest = "sha256:" + Convert.ToHexString(SHA256.HashData(payload)),
            browser_download_url = $"https://github.com/EnzoCx/Spike/releases/download/{tag}/Spike.exe" } }
    });
    File.WriteAllText(Path.Combine(store.Folder, "pending.json"), release);
    File.WriteAllText(args[1] + ".cache", store.Folder);
    return 0;
}

var executable = Environment.ProcessPath!;
if (args.Length == 0 && AutomaticUpdater.TryApplyPending(executable)) return 0;
File.WriteAllText(executable + ".started", typeof(AutomaticUpdater).Assembly.GetName().Version!.ToString());
return 0;
