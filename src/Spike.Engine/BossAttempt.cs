using AionDPS.Aion2;
using AionDPS.Combat;

namespace Spike.Engine;

internal sealed class BossAttempt(int entityId, int npcId, DateTime startedAt)
{
    public int EntityId { get; } = entityId;
    public int NpcId { get; } = npcId;
    public DateTime StartedAt { get; } = startedAt;
    private DateTime? endedAt;
    public DateTime? EndedAt => endedAt;
    private string? endReason;
    private DateTime? respawnedAt;

    public bool HasRespawnedAt(DateTime at) => respawnedAt is { } respawn && respawn <= at;

    public bool Includes(DamageEvent hit) => !hit.IsHeal && hit.Amount > 0
        && (hit.SourceObjectId == EntityId || hit.TargetObjectId == EntityId);

    // Read HP history once per poll, not for every damage event. The directory may already
    // contain a reset from later in the batch, so apply its timestamp while draining hits.
    public void Observe(Aion2EntityDirectory directory, DateTime now)
    {
        foreach (var at in directory.HitPoints.ResetsOf(EntityId))
            if (at > StartedAt && at <= now) EndAt(at, "boss-reset");
        foreach (var sample in directory.HitPoints.SamplesAround(EntityId, StartedAt, now))
        {
            if (sample.At >= StartedAt && sample.Hp == 0) EndAt(sample.At, "boss-defeated");
            if (endReason == "boss-defeated" && sample.At > endedAt && sample.Hp > 0)
                respawnedAt ??= sample.At;
        }
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
