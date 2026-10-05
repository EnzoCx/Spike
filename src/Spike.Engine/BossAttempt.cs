using AionDPS.Aion2;
using AionDPS.Combat;

namespace Spike.Engine;

internal sealed class BossAttempt(int entityId, int npcId, DateTime startedAt)
{
    public int EntityId { get; } = entityId;
    public int NpcId { get; } = npcId;
    private DateTime? endedAt;
    public DateTime? EndedAt => endedAt;
    private string? endReason;

    public bool Includes(DamageEvent hit) => !hit.IsHeal && hit.Amount > 0
        && (hit.SourceObjectId == EntityId || hit.TargetObjectId == EntityId);

    // Read HP history once per poll, not for every damage event. The directory may already
    // contain a reset from later in the batch, so apply its timestamp while draining hits.
    public void Observe(Aion2EntityDirectory directory, DateTime now)
    {
        foreach (var at in directory.HitPoints.ResetsOf(EntityId))
            if (at > startedAt && at <= now) EndAt(at, "boss-reset");
        foreach (var sample in directory.HitPoints.SamplesAround(EntityId, startedAt, now))
            if (sample.At >= startedAt && sample.Hp == 0) EndAt(sample.At, "boss-defeated");
    }

    public string? EndReasonAt(DateTime at, bool afterBatch = false) => endedAt is { } end
        && (end < at || (end == at && (afterBatch || endReason == "boss-reset"))) ? endReason : null;

    private void EndAt(DateTime at, string reason)
    {
        if (endedAt is not null && endedAt <= at) return;
        endedAt = at;
        endReason = reason;
    }
}
