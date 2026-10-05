namespace Spike.Core;

public static class EncounterMath
{
    public static Participant? PrimaryBoss(Encounter encounter) => encounter.Participants.Where(p => p.IsBoss)
        .OrderByDescending(p => encounter.Events.Where(e => e.Target == p.Id && !e.Heal).Sum(e => e.Amount)).FirstOrDefault();

    // Target DPS measures the target's own damage window; healing/other targets do not extend it.
    public static (long StartMs, long DurationMs) Window(Encounter encounter, int? target = null)
    {
        if (target is null) return (0, encounter.DurationMs);
        var players = encounter.Participants.Where(p => p.IsPlayer || p.IsUnidentifiedSource).Select(p => p.Id).ToHashSet();
        var hits = encounter.Events.Where(e => !e.Heal && e.Target == target && players.Contains(e.Source)).ToArray();
        return hits.Length == 0 ? (0, 0) : (hits.Min(e => e.AtMs), hits.Max(e => e.AtMs) - hits.Min(e => e.AtMs));
    }
    public static double Seconds(Encounter encounter, int? target = null) => Math.Max(1, Window(encounter, target).DurationMs / 1000d);

    public static IReadOnlyList<MeterRow> Players(Encounter encounter, bool heals, int? target = null)
    {
        var people = encounter.Participants.Where(actor => actor.IsPlayer || actor.IsUnidentifiedSource).ToDictionary(actor => actor.Id);
        var groups = encounter.Events.Where(hit => hit.Heal == heals && (target is null || hit.Target == target) && people.ContainsKey(hit.Source))
            .GroupBy(hit => hit.Source).Select(group => new { Actor = people[group.Key], Events = group.ToArray(), Total = group.Sum(hit => hit.Amount) })
            .OrderBy(group => group.Actor.IsUnidentifiedSource).ThenByDescending(group => group.Total).ToArray();
        var total = groups.Sum(group => group.Total);
        var maximum = groups.Length == 0 ? 0 : groups.Max(group => group.Total);
        var seconds = Seconds(encounter, target);
        return groups.Select(group => new MeterRow(group.Actor.Id, group.Actor.Name, group.Actor.ClassName,
            group.Total, group.Total / seconds, Percent(group.Total, total), group.Events.Count(hit => !hit.Tick),
            Percent(group.Events.Count(hit => hit.Critical && !hit.Tick), group.Events.Count(hit => !hit.Tick)), Percent(group.Total, maximum))).ToArray();
    }

    public static IReadOnlyList<SpellRow> Spells(Encounter encounter, int actor, bool heals, int? target = null)
    {
        var events = encounter.Events.Where(hit => hit.Source == actor && hit.Heal == heals && (target is null || hit.Target == target)).ToArray();
        var total = events.Sum(hit => hit.Amount);
        var seconds = Seconds(encounter, target);
        // AION 2 class skills encode specialisation/level in their last four digits.
        // Keep unknown IDs distinct and retain names to avoid merging unrelated imported skills.
        return events.GroupBy(hit => (SkillId: hit.SkillId is >= 11_000_000 and < 20_000_000 ? hit.SkillId / 10000 * 10000 : hit.SkillId, hit.Skill)).Select(group => new SpellRow(group.Key.SkillId, group.Key.Skill,
            group.Sum(hit => hit.Amount), group.Sum(hit => hit.Amount) / seconds,
            Percent(group.Sum(hit => hit.Amount), total), group.Count(hit => !hit.Tick), group.Count(hit => hit.Tick),
            Percent(group.Count(hit => hit.Critical && !hit.Tick), group.Count(hit => !hit.Tick))))
            .OrderByDescending(row => row.Total).ToArray();
    }

    public static double[] Timeline(Encounter encounter, bool heals, int? target = null, int buckets = 60, int? actor = null)
    {
        if (buckets is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(buckets), "Use between 1 and 4096 buckets.");
        var result = new double[buckets];
        var window = Window(encounter, target);
        var widthMs = Math.Max(1000, window.DurationMs) / (double)buckets;
        var people = encounter.Participants.Where(actor => actor.IsPlayer || actor.IsUnidentifiedSource).Select(actor => actor.Id).ToHashSet();
        foreach (var hit in encounter.Events.Where(hit => hit.Heal == heals && people.Contains(hit.Source) && (target is null || hit.Target == target) && (actor is null || hit.Source == actor)))
            result[Math.Clamp((int)((hit.AtMs - window.StartMs) / widthMs), 0, buckets - 1)] += hit.Amount / (widthMs / 1000);
        return result;
    }

    private static double Percent(long numerator, long denominator) => denominator == 0 ? 0 : numerator * 100d / denominator;
}
