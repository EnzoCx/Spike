using System.Text.Json;

namespace DPSMeter.Core;

public sealed record Actor(string Id, string Name, string ClassName);
public sealed record Hit(string Id, long OffsetMs, string ActorId, string Skill, long Damage, bool Critical);
public sealed record CombatLog(int SchemaVersion, string Id, string Region, string Patch, string Boss,
    string Difficulty, long DurationMs, bool IsDemo, Actor[] Actors, Hit[] Hits);
public sealed record DamageRow(string Name, string Detail, long Damage, double Dps, double Share,
    int Hits, double CriticalRate);
public sealed record CombatSummary(string Boss, double DurationSeconds, long TotalDamage,
    IReadOnlyList<DamageRow> Players);

public static class Combat
{
    public const int MaxFileBytes = 10 * 1024 * 1024;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static CombatLog Read(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxFileBytes)
            throw new InvalidDataException("Le fichier dépasse 10 Mo.");
        var log = JsonSerializer.Deserialize<CombatLog>(json, Json)
            ?? throw new InvalidDataException("Le fichier ne contient pas de combat.");
        Validate(log);
        return log;
    }

    public static void Validate(CombatLog log)
    {
        if (log.SchemaVersion != 1) throw new InvalidDataException("Version de fichier non prise en charge.");
        RequireText(log.Id, "Identifiant");
        RequireText(log.Boss, "Boss");
        RequireText(log.Patch, "Version du jeu");
        RequireText(log.Difficulty, "Difficulté");
        if (log.Region != "Global") throw new InvalidDataException("Cette version prend en charge la région Global.");
        if (log.DurationMs is <= 0 or > 86_400_000) throw new InvalidDataException("Durée invalide (maximum 24 heures).");
        if (log.Actors is null || log.Actors.Length is < 1 or > 100)
            throw new InvalidDataException("Le combat doit contenir entre 1 et 100 participants.");
        var actorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var actor in log.Actors)
        {
            if (actor is null) throw new InvalidDataException("Participant invalide.");
            RequireText(actor.Id, "Identifiant du participant");
            RequireText(actor.Name, "Nom");
            RequireText(actor.ClassName, "Classe");
            if (!actorIds.Add(actor.Id)) throw new InvalidDataException("Identifiant de participant en double.");
        }
        if (log.Hits is null || log.Hits.Length > 100_000) throw new InvalidDataException("Nombre d'événements invalide.");
        var hitIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var hit in log.Hits)
        {
            if (hit is null) throw new InvalidDataException("Événement invalide.");
            RequireText(hit.Id, "Identifiant d'événement");
            RequireText(hit.Skill, "Compétence");
            if (!hitIds.Add(hit.Id)) throw new InvalidDataException("Événement en double : import refusé pour éviter de compter deux fois les dégâts.");
            if (hit.ActorId is null || !actorIds.Contains(hit.ActorId)) throw new InvalidDataException("Participant inconnu dans un événement.");
            if (hit.OffsetMs < 0 || hit.OffsetMs > log.DurationMs) throw new InvalidDataException("Événement hors de la durée du combat.");
            if (hit.Damage is < 0 or > 1_000_000_000_000) throw new InvalidDataException("Valeur de dégâts invalide.");
        }
    }

    public static CombatSummary Summarize(CombatLog log)
    {
        Validate(log);
        var total = log.Hits.Sum(hit => hit.Damage);
        var byActor = log.Hits.ToLookup(hit => hit.ActorId);
        var rows = log.Actors.Select(actor => Row(actor.Name, actor.ClassName, byActor[actor.Id], log.DurationMs, total))
            .OrderByDescending(row => row.Damage).ThenBy(row => row.Name, StringComparer.Ordinal).ToArray();
        return new(log.Boss, log.DurationMs / 1000d, total, rows);
    }

    public static IReadOnlyList<DamageRow> Skills(CombatLog log, string actorId)
    {
        Validate(log);
        var hits = log.Hits.Where(hit => hit.ActorId == actorId).ToArray();
        var total = hits.Sum(hit => hit.Damage);
        return hits.GroupBy(hit => hit.Skill).Select(group => Row(group.Key, "", group, log.DurationMs, total))
            .OrderByDescending(row => row.Damage).ToArray();
    }

    // Export aliases are local to this combat; they are not persistent player identifiers.
    public static CombatLog Anonymize(CombatLog log)
    {
        Validate(log);
        var ids = log.Actors.Select((actor, index) => (actor.Id, Alias: $"player-{index + 1}"))
            .ToDictionary(item => item.Id, item => item.Alias);
        return log with
        {
            Id = Guid.NewGuid().ToString("N"),
            Actors = log.Actors.Select((actor, index) => new Actor(ids[actor.Id], $"Joueur {index + 1}", actor.ClassName)).ToArray(),
            Hits = log.Hits.Select((hit, index) => hit with { Id = $"event-{index + 1}", ActorId = ids[hit.ActorId] }).ToArray()
        };
    }

    private static DamageRow Row(string name, string detail, IEnumerable<Hit> source, long duration, long total)
    {
        var hits = source.ToArray();
        var damage = hits.Sum(hit => hit.Damage);
        return new(name, detail, damage, damage / (duration / 1000d), total == 0 ? 0 : damage * 100d / total,
            hits.Length, hits.Length == 0 ? 0 : hits.Count(hit => hit.Critical) * 100d / hits.Length);
    }

    private static void RequireText(string? text, string field)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 120 || text.Any(char.IsControl))
            throw new InvalidDataException($"{field} : texte manquant ou invalide (120 caractères maximum).");
    }
}
