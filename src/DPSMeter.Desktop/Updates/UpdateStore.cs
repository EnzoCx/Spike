using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace DPSMeter.Desktop.Updates;

// Each installed path has its own cache. A file lock serializes download and installation.
internal sealed class UpdateStore(string folder)
{
    internal string Folder => folder;
    internal string Payload => Path.Combine(folder, "pending.exe");
    private string Manifest => Path.Combine(folder, "pending.json");

    internal FileStream AcquireLock()
    {
        Directory.CreateDirectory(folder);
        return new FileStream(Path.Combine(folder, "update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    internal UpdatePackage? Pending(Version current)
    {
        try
        {
            if (!File.Exists(Manifest) || !File.Exists(Payload) || new FileInfo(Manifest).Length > 1024 * 1024) return null;
            var package = UpdatePackage.FromRelease(File.ReadAllText(Manifest), current);
            return package is not null && package.Matches(Payload) ? package : null;
        }
        // A damaged cache must not prevent a fresh download on every subsequent launch.
        catch (Exception error) when (error is IOException or JsonException or InvalidOperationException) { return null; }
    }

    internal async Task StageAsync(HttpClient client, string releaseJson, Version current, CancellationToken cancellation)
    {
        var package = UpdatePackage.FromRelease(releaseJson, current);
        if (package is null) return;
        var pending = Pending(current);
        if (pending is not null && pending.Version >= package.Version) return;
        var temporary = Path.Combine(folder, "download.tmp");
        try
        {
            using var response = await client.GetAsync(package.Url, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(cancellation))
            await using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellation)) != 0)
                {
                    received += read;
                    if (received > package.Size) throw new InvalidDataException("Update exceeds declared size.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellation);
                }
                await output.FlushAsync(cancellation);
            }
            if (!package.Matches(temporary)) throw new InvalidDataException("Invalid update checksum or size.");
            // Manifest is committed last: interrupted downloads can never become installable.
            File.Delete(Manifest);
            File.Move(temporary, Payload, true);
            var manifestTemporary = Manifest + ".tmp";
            await File.WriteAllTextAsync(manifestTemporary, releaseJson, cancellation);
            File.Move(manifestTemporary, Manifest, true);
        }
        finally { File.Delete(temporary); }
    }

    internal void Install(string target, UpdatePackage package)
    {
        var temporary = target + ".update-" + Guid.NewGuid().ToString("N");
        try
        {
            File.Copy(Payload, temporary);
            if (!package.Matches(temporary)) throw new InvalidDataException("Invalid staged update.");
            // Same-volume atomic replacement; the original is kept for manual recovery.
            File.Replace(temporary, target, target + ".previous");
        }
        finally { File.Delete(temporary); }
    }
}
