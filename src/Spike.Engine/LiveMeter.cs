using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using AionDPS.Combat.Sources;
using Spike.Core;

namespace Spike.Engine;

public sealed class LiveMeter : IDisposable
{
    private readonly Aion2PacketCombatSource source;
    private readonly List<DamageEvent> events = new();
    private readonly BossEventBuffer pendingBossEvents = new();
    private readonly HashSet<int> participantIds = new();
    private readonly Aion2Protocol protocol;
    private DateTime first, last, lastActivity;
    private int activityPlayer = -1;
    private readonly Dictionary<int, DateTime> assistedPlayers = new();
    private BossAttempt? boss;
    private bool dormant;
    private bool defeated;
    private string encounterZone = "";
    private Guid id;
    private DateTime observedUntil;
    private int contextVersion;
    private Encounter? cachedSnapshot;
    public SourceState State { get; private set; }
    public bool Paused { get; private set; }
    public string? Error { get; private set; }
    public long Packets => source.Packets;
    public long DecodedEvents => source.DecodedEvents;
    public int Errors => source.CallbackErrors + source.DroppedEvents;
    public bool CanFinish => events.Count > 0 && !defeated;
    public bool HasCombat => CanFinish && !dormant;
    public event Action<Encounter>? Completed;
    public DateTime LastEvent => last;

    public LiveMeter(string? playerName = null, string? adapter = null)
    {
        protocol = Aion2Protocol.Load();
        source = new(protocol, adapterId: adapter ?? "all", ownCharacterName: playerName);
        source.StatusChanged += status => { State = status.State; if (status.State == SourceState.Error) Error = status.Message; };
    }

    internal LiveMeter(Aion2PacketCombatSource source, Aion2Protocol protocol)
    {
        this.source = source;
        this.protocol = protocol;
    }

    public void Start() => source.Start();

    public void Poll() => Process(source.Poll(Paused).Damage, DateTime.Now);

    internal void Process(IReadOnlyList<DamageEvent> damage, DateTime now)
    {
        var wasDefeated = defeated;
        var previousId = id;
        var previousCount = events.Count;
        var directory = (Aion2EntityDirectory)source.Entities;
        directory.AdvanceTime(now);
        if (contextVersion != directory.ContextVersion) Finish("context");
        contextVersion = directory.ContextVersion;
        if (encounterZone != source.CurrentZone) Finish("zone");
        encounterZone = source.CurrentZone;
        var localPlayer = directory.LocalPlayerId;
        if (activityPlayer >= 0 && localPlayer != activityPlayer) Finish("identity");
        if (boss is not null && directory.BossNpcIdOf(boss.EntityId) != boss.NpcId) Finish("boss-changed");
        if (defeated && boss is not null && !directory.EvidenceApplies(boss.EntityId, boss.StartedAt)) Finish("boss-respawned");
        activityPlayer = localPlayer;
        boss?.Observe(directory, now);
        foreach (var player in assistedPlayers.Where(entry => (now - entry.Value).TotalSeconds >= 30).Select(entry => entry.Key).ToArray())
            assistedPlayers.Remove(player);
        var attributed = damage.Select(hit => CaptureAttribution(hit, directory));
        foreach (var hit in pendingBossEvents.Drain(attributed, directory, now))
        {
            observedUntil = hit.Timestamp;
            if (hit.Amount < 0 || hit.Amount > 1_000_000_000_000) continue;
            if (events.Count > 0 && hit.Timestamp < first) continue;
            if (boss?.EndReasonAt(hit.Timestamp) is { } ended)
            {
                if (ended == "boss-defeated") defeated = true;
                else Finish(ended);
            }
            if (hit.IsHeal && !hit.IsTick && hit.SourceObjectId == activityPlayer && hit.TargetObjectId != activityPlayer)
                assistedPlayers[hit.TargetObjectId] = hit.Timestamp;
            var engaged = EngagedBoss(hit, directory);
            if (boss is not null && engaged is not null
                && (boss.EntityId != engaged.EntityId || boss.NpcId != engaged.NpcId)) Finish("boss-changed");
            // Only this boss's damage belongs to the attempt. Healing cannot start one.
            if (hit.IsHeal ? boss is null : boss?.Includes(hit) != true && engaged is null) continue;
            var hitIds = new[] { hit.SourceObjectId, hit.TargetObjectId, hit.OriginalSource ?? hit.SourceObjectId };
            if (events.Count >= 250_000 || participantIds.Count + hitIds.Distinct().Count(actor => !participantIds.Contains(actor)) > 4096
                || (events.Count > 0 && (hit.Timestamp - first).TotalHours >= 23)) Finish("limit");
            var sameBoss = boss?.Includes(hit) == true;
            if (defeated && (!sameBoss || boss!.HasRespawnedAt(hit.Timestamp)))
            {
                if (engaged is null) continue;
                Finish("boss-defeated");
                sameBoss = false;
            }
            if (HasCombat && !sameBoss && (hit.Timestamp - lastActivity).TotalSeconds >= 12) EndIdle();
            if (dormant && !defeated)
            {
                if (sameBoss) dormant = false;
                else continue;
            }
            if (events.Count == 0)
            {
                if (engaged is null) continue;
                first = last = lastActivity = hit.Timestamp;
                id = Guid.NewGuid();
                encounterZone = source.CurrentZone;
            }
            if (boss is null && engaged is not null)
            {
                boss = engaged;
                boss.Observe(directory, now);
            }
            last = hit.Timestamp > last ? hit.Timestamp : last;
            if (boss?.Includes(hit) == true && hit.Timestamp > lastActivity) lastActivity = hit.Timestamp;
            events.Add(hit);
            participantIds.UnionWith(hitIds);
        }
        observedUntil = now;
        if (boss?.EndReasonAt(now, afterBatch: true) is { } reason)
        {
            if (reason == "boss-defeated") defeated = true;
            else Finish(reason);
        }
        // HP zero can arrive before the last damage frames. Keep those hits in the
        // completed archive, without reopening the overlay or creating a tiny new fight.
        var changed = !wasDefeated || previousId != id || previousCount != events.Count;
        if (defeated && changed)
            Completed?.Invoke(BuildSnapshot()! with { EndReason = "boss-defeated" });
        if (HasCombat && (now - lastActivity).TotalSeconds >= 12) EndIdle();
        if (events.Count > 0 && (!defeated || changed)) cachedSnapshot = BuildSnapshot();
    }

    private static DamageEvent CaptureAttribution(DamageEvent hit, Aion2EntityDirectory directory)
    {
        if (hit.AttributionCaptured) return hit;
        var owner = directory.EvidenceApplies(hit.SourceObjectId, hit.Timestamp) ? directory.SummonOwnerOf(hit.SourceObjectId) : null;
        return hit with
        {
            SourceObjectId = owner ?? hit.SourceObjectId,
            OriginalSource = owner is null ? null : hit.SourceObjectId,
            Attribution = owner is null ? null : directory.OwnerEvidence(hit.SourceObjectId),
            AttributionCaptured = true
        };
    }

    private BossAttempt? EngagedBoss(DamageEvent hit, Aion2EntityDirectory directory)
    {
        if (hit.IsHeal || hit.Amount <= 0) return null;
        var actor = hit.SourceObjectId;
        var target = directory.SummonOwnerOf(hit.TargetObjectId) ?? hit.TargetObjectId;
        bool Involved(int player) => activityPlayer < 0 || player == activityPlayer
            || (assistedPlayers.TryGetValue(player, out var healedAt) && hit.Timestamp >= healedAt
                && (hit.Timestamp - healedAt).TotalSeconds < 12);
        if (Involved(actor) && directory.BossNpcIdOf(target) is { } targetNpc)
            return new(target, targetNpc, hit.Timestamp);
        if (Involved(target) && directory.BossNpcIdOf(actor) is { } sourceNpc)
            return new(actor, sourceNpc, hit.Timestamp);
        return null;
    }

    private void EndIdle()
    {
        if (boss is null)
        {
            if (observedUntil > lastActivity.AddSeconds(12)) observedUntil = lastActivity.AddSeconds(12);
            Finish("idle"); return;
        }
        var snapshot = Snapshot();
        dormant = true;
        // Upsert the same archive if this boss resumes after a phase. Ambient traffic stays idle.
        if (snapshot is not null) Completed?.Invoke(snapshot with { EndReason = "idle" });
    }

    public void TogglePause()
    {
        Poll();
        if (!Paused) Finish("paused");
        Paused = !Paused;
        source.Poll(true);
    }

    public Encounter? Snapshot() => HasCombat ? BuildSnapshot() : null;

    private Encounter? BuildSnapshot()
    {
        if (events.Count == 0) return null;
        var directory = (Aion2EntityDirectory)source.Entities;
        if (contextVersion != directory.ContextVersion) return cachedSnapshot;
        var inspected = directory.InspectedPlayers().ToDictionary(p => p.Name, StringComparer.Ordinal);
        var mapped = events.ToArray();
        var sources = mapped.Select(h => h.SourceObjectId).ToHashSet();
        var actorTimes = mapped.SelectMany(h => new[] { (Id: h.SourceObjectId, h.Timestamp), (Id: h.TargetObjectId, h.Timestamp),
            (Id: h.OriginalSource ?? h.SourceObjectId, h.Timestamp) })
            .GroupBy(a => a.Id).ToDictionary(g => g.Key, g => g.Min(a => a.Timestamp));
        var actors = actorTimes.Select(entry =>
        {
            var actor = entry.Key;
            var valid = directory.EvidenceApplies(actor, entry.Value);
            var direct = valid && directory.HasDirectIdentity(actor);
            var name = (actor == boss?.EntityId ? Aion2BossCatalog.Find(boss.NpcId)?.Name : valid ? directory.NameFor(actor) : null) ?? $"#{actor}";
            var hp = directory.HitPoints.Latest(actor);
            var npc = actor == boss?.EntityId ? boss.NpcId : valid ? directory.BossNpcIdOf(actor) : null;
            var unresolved = !direct && npc is null && sources.Contains(actor)
                && (!valid || directory.IsKnownPlayer(actor));
            return new Participant(actor, name, valid ? directory.ClassOf(actor) ?? "" : "", direct, npc is not null,
                inspected.TryGetValue(name, out var profile) && profile.CombatPower > 0 ? profile.CombatPower : null,
                direct && actor == directory.LocalPlayerId, valid && hp is { } sample && sample.At >= first ? sample.Hp : null,
                valid ? directory.HitPoints.HighestSeen(actor) : null, unresolved, npc,
                direct && actor == directory.LocalPlayerId && directory.LocalCharacter?.ServerId is > 0 and var server ? server : null,
                direct ? directory.HitPoints.ObservedDeaths(actor, first,
                    boss?.EndedAt is { } end && end < observedUntil ? end : observedUntil) : null, direct ? "direct" : "unknown");
        }).ToArray();
        // Do not dilute the finished boss's DPS while waiting for the inactivity timeout.
        var duration = Math.Max(0, (long)(last - first).TotalMilliseconds);
        var hits = mapped.Select(hit => new CombatEvent(Math.Max(0, (long)(hit.Timestamp - first).TotalMilliseconds), hit.SourceObjectId,
            hit.TargetObjectId, hit.SkillId, hit.Skill ?? "—", hit.Amount, hit.IsHeal, hit.IsCritical, hit.IsTick, hit.OriginalSource, hit.Attribution)).ToArray();
        return EncounterSources.Classify(new Encounter(2, id, new DateTimeOffset(first), "Global", protocol.GameVersion, "live", encounterZone, "active", duration, actors, hits));
    }

    public void Finish(string reason = "manual")
    {
        if (defeated) reason = "boss-defeated";
        var snapshot = BuildSnapshot();
        events.Clear();
        pendingBossEvents.Clear();
        participantIds.Clear();
        assistedPlayers.Clear();
        boss = null;
        dormant = false;
        defeated = false;
        cachedSnapshot = null;
        if (snapshot is not null) Completed?.Invoke(snapshot with { EndReason = reason });
    }

    public void Dispose() { source.Stop(); Poll(); Finish("closed"); source.Dispose(); }
}
