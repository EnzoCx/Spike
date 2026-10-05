using System.Text.Json;

namespace Spike.Core;

public sealed record HistoryEntry(Guid Id, DateTimeOffset StartedAt, string Title, long DurationMs, long Damage, string Origin,
    int Players = 0, bool Boss = false, string SearchPlayers = "");

public sealed class EncounterStore(string root)
{
    public string Root { get; } = root;
    public int SkippedFiles { get; private set; }

    public void Save(Encounter encounter)
    {
        EncounterFile.Validate(encounter);
        Directory.CreateDirectory(Root);
        var path = FilePath(encounter.Id);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(encounter, EncounterFile.Json));
        File.Move(temporary, path, true);
    }

    public Encounter Load(Guid id)
    {
        var path = FilePath(id);
        if (new FileInfo(path).Length > EncounterFile.MaxBytes) throw new InvalidDataException("File too large.");
        return EncounterFile.Read(File.ReadAllText(path));
    }

    public IReadOnlyList<HistoryEntry> List()
    {
        SkippedFiles = 0;
        if (!Directory.Exists(Root)) return [];
        var entries = new List<HistoryEntry>();
        foreach (var path in Directory.EnumerateFiles(Root, "*.json"))
        {
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id)) continue;
            try
            {
                var encounter = Load(id);
                var boss = EncounterMath.PrimaryBoss(encounter);
                var players = EncounterMath.Players(encounter, false, boss?.Id);
                entries.Add(new(id, encounter.StartedAt, boss?.Name ?? encounter.Title, EncounterMath.Window(encounter, boss?.Id).DurationMs,
                    players.Sum(row => row.Total), encounter.Origin, players.Count(row => encounter.Participants.First(p => p.Id == row.Id).IsPlayer), boss is not null, string.Join(" ", players.Select(p => p.Name))));
            }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException or UnauthorizedAccessException) { SkippedFiles++; }
        }
        return entries.OrderByDescending(entry => entry.StartedAt).ToArray();
    }

    private string FilePath(Guid id) => Path.Combine(Root, id.ToString("N") + ".json");
}
