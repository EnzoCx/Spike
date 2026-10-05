using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Spike.Desktop.Updates;

internal enum UpdateDownloadResult { Current, Ready, Failed, Unavailable }

internal static class AutomaticUpdater
{
    private static readonly SemaphoreSlim DownloadGate = new(1);
    private static Version Current => typeof(AutomaticUpdater).Assembly.GetName().Version!;

    internal static UpdateStore Store(string executable)
    {
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant())));
        return new UpdateStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Spike", "updates", id));
    }

    internal static string? PublishedExecutable()
    {
        var path = Environment.ProcessPath;
        // Never update development builds, dotnet hosts or offscreen verification processes.
        return path is not null && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            !File.Exists(Path.Combine(AppContext.BaseDirectory, "Spike.dll")) &&
            !Path.GetFileNameWithoutExtension(path).Equals("dotnet", StringComparison.OrdinalIgnoreCase) ? path : null;
    }

    internal static bool TryApplyPending(string executable)
    {
        try
        {
            var store = Store(executable);
            using var updateLock = store.AcquireLock();
            var pending = store.Pending(Current);
            if (pending is null || !HasVersion(store.Payload, pending.Version)) return false;
            // Use the current trusted binary as the helper, never an unchecked downloaded program.
            var helper = Path.Combine(store.Folder, "installer.exe");
            File.Copy(executable, helper, true);
            var start = new ProcessStartInfo(helper) { UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("--apply-update");
            start.ArgumentList.Add(Environment.ProcessId.ToString());
            start.ArgumentList.Add(executable);
            using var process = Process.Start(start);
            return process is not null;
        }
        catch (Exception error) when (IsExpected(error)) { return false; }
    }

    internal static async Task<UpdateDownloadResult> DownloadAsync(string executable)
    {
        await DownloadGate.WaitAsync();
        var cachedUpdate = false;
        try
        {
            var store = Store(executable);
            using var updateLock = store.AcquireLock();
            var pending = store.Pending(Current);
            cachedUpdate = pending is not null && HasVersion(store.Payload, pending.Version);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30), MaxResponseContentBufferSize = 1024 * 1024 };
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"Spike/{Current}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            var json = await client.GetStringAsync($"https://api.github.com/repos/{UpdatePackage.Repository}/releases/latest", timeout.Token);
            var package = UpdatePackage.FromRelease(json, new Version(0, 0, 0, 0));
            if (package is null) return cachedUpdate ? UpdateDownloadResult.Ready : UpdateDownloadResult.Failed;
            if (package.Version <= Current) return cachedUpdate ? UpdateDownloadResult.Ready : UpdateDownloadResult.Current;
            await store.StageAsync(client, json, Current, timeout.Token);
            return store.Pending(Current) is not null ? UpdateDownloadResult.Ready : UpdateDownloadResult.Failed;
        }
        // Updates are best effort: offline, rate limiting, read-only folders and corrupt data never block the meter.
        catch (Exception error) when (IsExpected(error)) { return cachedUpdate ? UpdateDownloadResult.Ready : UpdateDownloadResult.Failed; }
        finally { DownloadGate.Release(); }
    }

    internal static async Task<int> ApplyAsync(string parentId, string target)
    {
        try
        {
            target = Path.GetFullPath(target);
            var store = Store(target);
            if (!string.Equals(Environment.ProcessPath, Path.Combine(store.Folder, "installer.exe"), StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(parentId, out var pid)) return 1;
            try
            {
                using var parent = Process.GetProcessById(pid);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await parent.WaitForExitAsync(timeout.Token);
            }
            catch (ArgumentException) { } // Parent already exited.
            catch (OperationCanceledException) { return 1; } // Never duplicate or terminate a still-running parent.

            using (var updateLock = store.AcquireLock())
            {
                UpdatePackage.TryVersion(FileVersionInfo.GetVersionInfo(target).FileVersion, out var installed);
                var pending = store.Pending(installed > Current ? installed : Current);
                if (pending is not null && HasVersion(store.Payload, pending.Version))
                {
                    for (var attempt = 0; attempt < 20; attempt++)
                    {
                        try { store.Install(target, pending); break; }
                        catch (IOException) when (attempt < 19) { await Task.Delay(250); }
                    }
                }
            }
        }
        catch (Exception error) when (IsExpected(error)) { }

        try
        {
            // Also restart the original on installation failure. Skip applying again to prevent a restart loop.
            var start = new ProcessStartInfo(target) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(target)! };
            start.ArgumentList.Add("--update-restarted");
            using var process = Process.Start(start);
            return process is null ? 1 : 0;
        }
        catch (Exception error) when (IsExpected(error)) { return 1; }
    }

    private static bool HasVersion(string path, Version version) =>
        UpdatePackage.TryVersion(FileVersionInfo.GetVersionInfo(path).FileVersion, out var actual) && actual == version;

    private static bool IsExpected(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or
        ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException;
}
