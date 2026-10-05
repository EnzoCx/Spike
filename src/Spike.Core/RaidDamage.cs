namespace Spike.Core;

// A frozen estimate for ONE observed aura on a hit. Original damage remains authoritative.
public sealed record RaidCredit(int Provider, int SkillId, long Bonus);
public sealed record RaidRow(MeterRow Meter, long Received, long Provided, bool HasEvidence);
public sealed record RaidResult(IReadOnlyList<RaidRow> Rows, long ObservedDamage, long TotalDamage)
{
    public bool Available => Rows.Any(row => row.HasEvidence);
    public double ObservedPercent => TotalDamage == 0 ? 0 : ObservedDamage * 100d / TotalDamage;
}

public static class RaidDamage
{
    public const string Model = "auras-level1-v1";

    public static RaidResult Calculate(Encounter fight, int? target = null)
    {
        var raw = EncounterMath.Players(fight, false, target).ToDictionary(row => row.Id);
        var people = fight.Participants.Where(p => p.IsPlayer || p.IsUnidentifiedSource).ToDictionary(p => p.Id);
        var received = new Dictionary<int, long>();
        var provided = new Dictionary<int, long>();
        var evidence = new HashSet<int>();
        long observed = 0;
        if (fight.RdpsModel == Model)
            foreach (var hit in fight.Events.Where(e => !e.Heal && (target is null || e.Target == target)))
            {
                if (hit.Raid is not { } credit || !people.ContainsKey(hit.Source) || !people.ContainsKey(credit.Provider)) continue;
                observed += hit.Amount;
                evidence.Add(hit.Source); evidence.Add(credit.Provider);
                received[hit.Source] = received.GetValueOrDefault(hit.Source) + credit.Bonus;
                provided[credit.Provider] = provided.GetValueOrDefault(credit.Provider) + credit.Bonus;
            }
        var total = raw.Values.Sum(r => r.Total);
        var seconds = EncounterMath.Seconds(fight, target);
        var rows = raw.Keys.Concat(provided.Keys).Distinct().Select(id =>
        {
            var person = people[id];
            var original = raw.GetValueOrDefault(id) ?? new(id, person.Name, person.ClassName, 0, 0, 0, 0, 0, 0);
            var amount = original.Total - received.GetValueOrDefault(id) + provided.GetValueOrDefault(id);
            return new RaidRow(original with
            {
                Total = amount,
                PerSecond = amount / seconds,
                Share = total == 0 ? 0 : amount * 100d / total
            }, received.GetValueOrDefault(id), provided.GetValueOrDefault(id), evidence.Contains(id));
        }).OrderBy(r => people[r.Meter.Id].IsUnidentifiedSource).ThenByDescending(r => r.Meter.Total).ToArray();
        var maximum = rows.Length == 0 ? 0 : rows.Max(r => r.Meter.Total);
        return new(rows.Select(r => r with { Meter = r.Meter with { MaximumShare = maximum == 0 ? 0 : r.Meter.Total * 100d / maximum } }).ToArray(), observed, total);
    }
}
