using System.Text.Json;

namespace Spike.Core;

public static class EncounterFile
{
    public const int MaxBytes = 64 * 1024 * 1024;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Encounter Read(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("File too large.");
        var encounter = JsonSerializer.Deserialize<Encounter>(json, Json) ?? throw new InvalidDataException("Empty file.");
        Validate(encounter);
        return EncounterSources.Classify(encounter);
    }

    public static void Validate(Encounter encounter)
    {
        if (encounter.Version != 2 || encounter.Id == Guid.Empty || encounter.Region != "Global" || encounter.DurationMs is < 0 or > 86_400_000
            || encounter.Participants is null || encounter.Participants.Length is < 1 or > 4096 || encounter.Events is null || encounter.Events.Length > 250_000)
            throw new InvalidDataException("Invalid encounter.");
        CheckText(encounter.Patch); CheckText(encounter.Origin); CheckText(encounter.Zone); CheckText(encounter.EndReason);
        var ids = new HashSet<int>();
        foreach (var actor in encounter.Participants)
        {
            if (actor is null || !ids.Add(actor.Id)) throw new InvalidDataException("Duplicate actor.");
            CheckText(actor.Name); CheckText(actor.ClassName);
            if (actor.CombatPower is < 0 or > 1_000_000_000 || actor.CurrentHp is < 0 or > 1_000_000_000_000 || actor.HighestHp is < 0 or > 1_000_000_000_000)
                throw new InvalidDataException("Invalid participant statistics.");
        }
        foreach (var hit in encounter.Events)
        {
            if (hit is null || !ids.Contains(hit.Source) || !ids.Contains(hit.Target) || hit.Amount is < 0 or > 1_000_000_000_000
                || hit.AtMs < 0 || hit.AtMs > encounter.DurationMs) throw new InvalidDataException("Invalid event.");
            CheckText(hit.Skill);
        }
    }

    public static Encounter PrivateExport(Encounter encounter)
    {
        Validate(encounter);
        var ids = encounter.Participants.Select((actor, index) => (actor.Id, Replacement: index + 1)).ToDictionary(pair => pair.Id, pair => pair.Replacement);
        return encounter with
        {
            Id = Guid.NewGuid(),
            Origin = encounter.Origin == "demo" ? "demo" : "shared-unverified",
            Participants = encounter.Participants.Select(actor => actor with { Id = ids[actor.Id], Name = actor.IsPlayer || actor.IsUnidentifiedSource ? $"Player {ids[actor.Id]}" : actor.Name, IsSelf = false }).ToArray(),
            Events = encounter.Events.Select(hit => hit with { Source = ids[hit.Source], Target = ids[hit.Target] }).ToArray()
        };
    }

    private static void CheckText(string? value)
    {
        if (value is null || value.Length > 256 || value.Any(char.IsControl)) throw new InvalidDataException("Invalid text.");
    }
}
