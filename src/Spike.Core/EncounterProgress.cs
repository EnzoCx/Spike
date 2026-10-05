namespace Spike.Core;

public sealed record ProgressAttempt(Guid Id, DateTimeOffset At, string Context, string Title, string PlayerKey,
    string PlayerName, bool IsSelf, double Dps, double Hps, long Damage, long DurationMs, int? Deaths,
    string EndReason, bool Uncertain, IReadOnlyList<SpellRow> Skills);

/// <summary>Small history projections: no retained event arrays, no cross-fight entity-ID matching.</summary>
public static class EncounterProgress
{
    public static IReadOnlyList<ProgressAttempt> Summarize(Encounter fight)
    {
        var boss = EncounterMath.PrimaryBoss(fight);
        var title = boss?.Name ?? fight.Zone;
        if (string.IsNullOrWhiteSpace(title) || title.StartsWith('#')) return [];
        // Keep demos, imports, protocol versions and zones apart. Old archives use the observed name.
        var targetKey = boss is null ? "zone" : boss.NpcId is { } npc ? $"npc:{npc}" : $"name:{boss.Name}";
        var context = System.Text.Json.JsonSerializer.Serialize(new[] { fight.Region, fight.Patch, fight.Origin, fight.Zone, targetKey });
        var rates = EncounterMath.Players(fight, false, boss?.Id).ToDictionary(p => p.Id);
        var healing = EncounterMath.Players(fight, true).ToDictionary(p => p.Id);
        var duration = EncounterMath.Window(fight, boss?.Id).DurationMs;
        var unattributed = fight.Participants.Any(a => a.IsUnidentifiedSource);
        string PlayerKey(Participant p) => System.Text.Json.JsonSerializer.Serialize(new[] { p.Name, p.ClassName, p.ServerId?.ToString() ?? "" });
        var players = fight.Participants.Where(p => p.IsPlayer && !p.IsUnidentifiedSource && IsNamed(p)).ToArray();
        var identities = players.GroupBy(PlayerKey).ToDictionary(g => g.Key, g => g.Count());
        return players.Where(p => identities[PlayerKey(p)] == 1)
            .Select(p => new ProgressAttempt(fight.Id, fight.StartedAt, context, title,
                PlayerKey(p),
                p.Name, p.IsSelf, rates.GetValueOrDefault(p.Id)?.PerSecond ?? 0,
                healing.GetValueOrDefault(p.Id)?.PerSecond ?? 0, rates.GetValueOrDefault(p.Id)?.Total ?? 0,
                duration, p.ObservedDeaths, fight.EndReason,
                p.IdentityEvidence != "direct" || p.ServerId is null || boss is { NpcId: null }
                    || unattributed,
                EncounterMath.Spells(fight, p.Id, false, boss?.Id))).ToArray();
    }

    public static IReadOnlyList<ProgressAttempt> Series(IEnumerable<ProgressAttempt> attempts, string context, string playerKey) =>
        attempts.Where(a => a.Context == context && a.PlayerKey == playerKey)
            .OrderBy(a => a.At).ThenBy(a => a.Id).ToArray();

    public static double? Change(double current, double previous) => previous > 0 ? (current / previous - 1) * 100 : null;

    private static bool IsNamed(Participant actor) => !string.IsNullOrWhiteSpace(actor.Name)
        && actor.Name != $"Player #{actor.Id}" && actor.Name != $"#{actor.Id}"
        && !(actor.Name.StartsWith("Player ", StringComparison.Ordinal) && int.TryParse(actor.Name.AsSpan(7), out _));
}
