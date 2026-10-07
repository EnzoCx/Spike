using AionDPS.Aion2;
using AionDPS.Combat;

namespace Spike.Engine;

// Boss identity can arrive after damage. Unconfirmed hits stay briefly in memory,
// never in a displayed or saved encounter, and cannot grow with an open-world farm.
internal sealed class BossEventBuffer
{
    private readonly List<DamageEvent> pending = new();

    public DamageEvent[] Drain(IEnumerable<DamageEvent> received, Aion2EntityDirectory directory, DateTime now)
    {
        bool KnownBoss(int actor, DateTime at) => directory.EvidenceApplies(actor, at) && directory.BossNpcIdOf(actor) is not null;
        bool BossDamage(DamageEvent hit) => KnownBoss(hit.SourceObjectId, hit.Timestamp) || KnownBoss(hit.TargetObjectId, hit.Timestamp);

        pending.RemoveAll(hit => (now - hit.Timestamp).TotalSeconds > 30);
        var ready = pending.Where(BossDamage).ToList();
        pending.RemoveAll(BossDamage);
        foreach (var hit in received)
        {
            if (hit.Amount <= 0 || hit.Amount > 1_000_000_000_000) continue;
            if (hit.IsHeal || BossDamage(hit)) ready.Add(hit);
            else if ((now - hit.Timestamp).TotalSeconds <= 30) pending.Add(hit);
        }
        if (pending.Count > 4096) pending.RemoveRange(0, pending.Count - 4096);
        return ready.OrderBy(hit => hit.Timestamp).ToArray();
    }

    public void Clear() => pending.Clear();
}
