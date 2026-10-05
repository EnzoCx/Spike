namespace Spike.Engine;

public static class EngineAssets
{
    private static readonly Lazy<string> Location = new(Extract);
    public static string Root => Location.Value;

    private static string Extract()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spike", "engine", "2f237fed-v1");
        var assembly = typeof(EngineAssets).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith("assets/", StringComparison.Ordinal)))
        {
            var destination = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var input = assembly.GetManifestResourceStream(name)!;
            // Always restore shipped data: avoid depending on mutable old cached versions.
            var bytes = new byte[input.Length];
            input.ReadExactly(bytes);
            if (!File.Exists(destination) || !File.ReadAllBytes(destination).AsSpan().SequenceEqual(bytes))
            {
                var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, destination, true);
            }
        }
        return root;
    }
}
