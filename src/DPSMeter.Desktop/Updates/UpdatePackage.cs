using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DPSMeter.Desktop.Updates;

internal sealed record UpdatePackage(Version Version, Uri Url, long Size, string Sha256)
{
    internal const string Repository = "EnzoCx/Spike";
    internal const long MaximumSize = 300 * 1024 * 1024;

    internal static UpdatePackage? FromRelease(string json, Version current)
    {
        var release = JsonSerializer.Deserialize<Release>(json);
        if (release is null || release.Draft || release.Prerelease ||
            !TryVersion(release.Tag, out var version) || version <= current) return null;
        var asset = release.Assets?.SingleOrDefault(a => a?.Name == "DPSMeter.exe");
        var expectedUrl = $"https://github.com/{Repository}/releases/download/{release.Tag}/DPSMeter.exe";
        if (asset is null || asset.State != "uploaded" || asset.Url != expectedUrl ||
            asset.Size <= 0 || asset.Size > MaximumSize || asset.Digest is null ||
            !asset.Digest.StartsWith("sha256:", StringComparison.Ordinal)) return null;
        var hash = asset.Digest[7..];
        if (hash.Length != 64 || !hash.All(Uri.IsHexDigit)) return null;
        return new(version, new Uri(expectedUrl), asset.Size, hash);
    }

    internal static bool TryVersion(string? value, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        var text = value?.TrimStart('v');
        if (text is null || !Version.TryParse(text, out var parsed) || parsed.Build < 0) return false;
        version = new Version(parsed.Major, parsed.Minor, parsed.Build, Math.Max(0, parsed.Revision));
        return true;
    }

    internal bool Matches(string path)
    {
        using var stream = File.OpenRead(path);
        return stream.Length == Size &&
            Convert.ToHexString(SHA256.HashData(stream)).Equals(Sha256, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record Release(
        [property: JsonPropertyName("tag_name")] string? Tag,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] Asset?[]? Assets);

    private sealed record Asset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("browser_download_url")] string Url,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("digest")] string? Digest);
}
