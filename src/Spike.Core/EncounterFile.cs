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
        if (encounter.RdpsModel is not null && encounter.RdpsModel != RaidCredit.LegacyModel)
            throw new InvalidDataException("Unknown rDPS model.");
        var ids = new HashSet<int>();
        foreach (var actor in encounter.Participants)
        {
            if (actor is null || !ids.Add(actor.Id)) throw new InvalidDataException("Duplicate actor.");
            CheckText(actor.Name); CheckText(actor.ClassName);
            if (actor.CombatPower is < 0 or > 1_000_000_000 || actor.CurrentHp is < 0 or > 1_000_000_000_000 || actor.HighestHp is < 0 or > 1_000_000_000_000)
                throw new InvalidDataException("Invalid participant statistics.");
            if (actor.NpcId is <= 0 || actor.ServerId is <= 0 || actor.ObservedDeaths is < 0 or > 250_000
                || actor.IdentityEvidence is not (null or "direct" or "unknown"))
                throw new InvalidDataException("Invalid participant evidence.");
        }
        foreach (var hit in encounter.Events)
        {
            if (hit is null || !ids.Contains(hit.Source) || !ids.Contains(hit.Target) || hit.Amount is < 0 or > 1_000_000_000_000
                || hit.AtMs < 0 || hit.AtMs > encounter.DurationMs) throw new InvalidDataException("Invalid event.");
            CheckText(hit.Skill);
            if (hit.Raid is { } raid && (encounter.RdpsModel != RaidCredit.LegacyModel || hit.Heal
                || !ids.Contains(raid.Provider) || raid.SkillId is not (17410000 or 18190000)
                || raid.Bonus < 0 || raid.Bonus > hit.Amount || raid.Provider == hit.Source && raid.Bonus != 0))
                throw new InvalidDataException("Invalid rDPS estimate.");
            if (hit.OriginalSource is { } original && !ids.Contains(original)
                || hit.Attribution is not (null or "owner-id" or "owner-name"))
                throw new InvalidDataException("Invalid attribution evidence.");
        }
    }

    public static Encounter PrivateExport(Encounter encounter)
    {
        Validate(encounter);
        var ids = encounter.Participants.Select((actor, index) => (actor.Id, Replacement: index + 1)).ToDictionary(pair => pair.Id, pair => pair.Replacement);
        var originalSources = encounter.Events.Where(e => e.OriginalSource is not null).Select(e => e.OriginalSource!.Value).ToHashSet();
        return encounter with
        {
            Id = Guid.NewGuid(),
            Origin = encounter.Origin == "demo" ? "demo" : "shared-unverified",
            Participants = encounter.Participants.Select(actor => actor with { Id = ids[actor.Id], Name = actor.IsPlayer || actor.IsUnidentifiedSource || originalSources.Contains(actor.Id) ? $"Player {ids[actor.Id]}" : actor.Name, IsSelf = false }).ToArray(),
            Events = encounter.Events.Select(hit => hit with
            {
                Source = ids[hit.Source],
                Target = ids[hit.Target],
                OriginalSource = hit.OriginalSource is { } original ? ids[original] : null,
                Raid = hit.Raid is { } raid ? raid with { Provider = ids[raid.Provider] } : null
            }).ToArray()
        };
    }

    private static void CheckText(string? value)
    {
        if (value is null || value.Length > 256 || value.Any(char.IsControl)) throw new InvalidDataException("Invalid text.");
    }
}
