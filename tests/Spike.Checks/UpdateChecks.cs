using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Spike.Desktop.Updates;

internal static class UpdateChecks
{
    internal static async Task Run(Action<bool, string> check)
    {
        await CheckPolling(check);
        var current = new Version(0, 4, 6, 0);
        var bytes = Encoding.UTF8.GetBytes("synthetic executable fixture");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        string Release(string tag = "v0.4.7", bool prerelease = false, bool draft = false,
            string? digest = null, string? url = null, long? size = null) => JsonSerializer.Serialize(new
            {
                tag_name = tag,
                prerelease,
                draft,
                assets = new[] { new { name = "Spike.exe", state = "uploaded", size = size ?? bytes.Length,
                    digest = digest ?? "sha256:" + hash,
                    browser_download_url = url ?? $"https://github.com/EnzoCx/Spike/releases/download/{tag}/Spike.exe" } }
            });
        check(UpdatePackage.FromRelease(Release(), current)?.Version == new Version(0, 4, 7, 0), "Accept newer stable GitHub release");
        foreach (var tag in new[] { "v0.4.6", "v0.4.5", "v0.4.7-beta", "invalid" })
            check(UpdatePackage.FromRelease(Release(tag), current) is null, "Ignore old, equal or unsupported version " + tag);
        check(UpdatePackage.FromRelease(Release(prerelease: true), current) is null &&
            UpdatePackage.FromRelease(Release(draft: true), current) is null, "Ignore drafts and prereleases");
        check(UpdatePackage.FromRelease(Release("v0.10.0"), current) is not null, "Compare versions numerically");
        check(UpdatePackage.FromRelease(Release(url: "https://example.org/Spike.exe"), current) is null,
            "Reject downloads outside the official repository");
        check(UpdatePackage.FromRelease(Release(url: "https://github.com/Phobie53/DPSMeter/releases/download/v0.4.7/DPSMeter.exe"), current) is null,
            "Reject the old repository after migration");
        check(UpdatePackage.FromRelease(Release(digest: "sha256:broken"), current) is null &&
            UpdatePackage.FromRelease(Release(size: UpdatePackage.MaximumSize + 1), current) is null,
            "Require SHA256 and bounded asset size");
        check(UpdatePackage.FromRelease("{}", current) is null, "Missing release fields are ignored");

        var folder = Path.Combine(Path.GetTempPath(), "Spike-update-check-" + Guid.NewGuid().ToString("N"));
        var store = new UpdateStore(folder);
        try
        {
            using (store.AcquireLock())
            {
                try { using var duplicate = store.AcquireLock(); throw new Exception("Concurrent lock accepted"); }
                catch (IOException) { check(true, "Serialize competing updaters"); }
                using var client = new HttpClient(new FakeDownload(bytes));
                await store.StageAsync(client, Release(), current, CancellationToken.None);
                check(store.Pending(current) is not null, "Complete verified download becomes pending");
                check(store.Pending(new Version(0, 4, 7, 0)) is null, "Do not reapply an installed update");
                var target = Path.Combine(folder, "installed.exe");
                File.WriteAllText(target, "original");
                store.Install(target, store.Pending(current)!);
                check(File.ReadAllBytes(target).SequenceEqual(bytes) && File.ReadAllText(target + ".previous") == "original",
                    "Atomic replacement retains original binary for recovery");
                using (var lockedTarget = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    try { store.Install(target, store.Pending(current)!); throw new Exception("Locked target replaced"); }
                    catch (IOException) { check(File.ReadAllBytes(target).SequenceEqual(bytes), "Locked executable remains intact"); }
                }
                File.WriteAllText(store.Payload, "corrupt");
                check(store.Pending(current) is null, "Recheck cached checksum before installation");
                foreach (var badBytes in new[] { bytes[..^1], bytes.Concat(new byte[] { 0 }).ToArray(), new byte[bytes.Length] })
                {
                    using var badClient = new HttpClient(new FakeDownload(badBytes));
                    try { await store.StageAsync(badClient, Release(), current, CancellationToken.None); throw new Exception("Bad download accepted"); }
                    catch (InvalidDataException) { check(store.Pending(current) is null, "Reject truncated, oversized or altered downloads"); }
                }
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                try { await store.StageAsync(client, Release(), current, cancelled.Token); throw new Exception("Cancelled download accepted"); }
                catch (OperationCanceledException) { check(store.Pending(current) is null, "Cancelled download never becomes installable"); }
                using var unavailable = new HttpClient(new FakeDownload(bytes, HttpStatusCode.ServiceUnavailable));
                try { await store.StageAsync(unavailable, Release(), current, CancellationToken.None); throw new Exception("HTTP failure accepted"); }
                catch (HttpRequestException) { check(true, "HTTP failures cannot install a payload"); }
                check(File.ReadAllBytes(target).SequenceEqual(bytes), "Download failures leave installed application intact");
                File.WriteAllText(Path.Combine(folder, "pending.json"), "broken manifest");
                check(store.Pending(current) is null, "Ignore malformed cached metadata");
                await store.StageAsync(client, Release(), current, CancellationToken.None);
                check(store.Pending(current) is not null, "A damaged cache recovers on the next download");
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    private sealed class FakeDownload(byte[] bytes, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(bytes) });
    }

    private static async Task CheckPolling(Action<bool, string> check)
    {
        check(UpdatePolling.Interval == TimeSpan.FromMinutes(15), "Check updates every fifteen minutes");
        using var stop = new CancellationTokenSource();
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var active = 0;
        var overlap = false;
        var loop = UpdatePolling.RunAsync(async () =>
        {
            if (Interlocked.Increment(ref active) != 1) overlap = true;
            var call = Interlocked.Increment(ref calls);
            if (call == 1) { first.SetResult(); await release.Task; }
            if (call == 2) second.SetResult();
            Interlocked.Decrement(ref active);
        }, stop.Token, TimeSpan.FromMilliseconds(10));
        try
        {
            await first.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(50);
            check(calls == 1 && !overlap, "A slow initial check never overlaps periodic checks");
            release.SetResult();
            await second.Task.WaitAsync(TimeSpan.FromSeconds(5));
            stop.Cancel();
            await loop.WaitAsync(TimeSpan.FromSeconds(5));
            var stoppedCalls = calls;
            await Task.Delay(30);
            check(stoppedCalls >= 2 && calls == stoppedCalls && !overlap,
                "Checks recur without reopening and stop on shutdown");
        }
        finally { stop.Cancel(); release.TrySetResult(); }

        using var alreadyStopped = new CancellationTokenSource();
        alreadyStopped.Cancel();
        await UpdatePolling.RunAsync(() => throw new Exception("Check after shutdown"), alreadyStopped.Token);
        check(true, "A stopped updater never starts a request");
    }
}
