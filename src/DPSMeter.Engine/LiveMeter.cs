using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using AionDPS.Combat.Sources;
using DPSMeter.Core;

namespace DPSMeter.Engine;

public sealed class LiveMeter : IDisposable
{
    private readonly Aion2PacketCombatSource source;
    private readonly List<DamageEvent> events = new();
    private readonly Aion2Protocol protocol;
    private DateTime first, last, lastActivity;
    private int activityPlayer = -1;
    private Guid id;
    public SourceState State { get; private set; }
    public bool Paused { get; private set; }
    public string? Error { get; private set; }
    public long Packets => source.Packets;
    public long DecodedEvents => source.DecodedEvents;
    public int Errors => source.CallbackErrors + source.DroppedEvents;
    public bool HasCombat => events.Count > 0;
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
        var directory = (Aion2EntityDirectory)source.Entities;
        var localPlayer = directory.LocalPlayerId;
        if (localPlayer != activityPlayer)
        {
            activityPlayer = localPlayer;
            if (HasCombat)
            {
                // Identity can arrive after the first hits. Rebuild the clock from retained events.
                var activity = events.Where(hit => KeepsCombatActive(hit, directory)).ToArray();
                if (activity.Length == 0) Finish("idle");
                else lastActivity = activity.Max(hit => hit.Timestamp);
            }
        }
        foreach (var hit in damage)
        {
            if (hit.Amount < 0 || hit.Amount > 1_000_000_000_000) continue;
            if (HasCombat && (hit.Timestamp - lastActivity).TotalSeconds >= 12) Finish("idle");
            if (events.Count >= 250_000 || (events.Count > 0 && (hit.Timestamp - first).TotalHours >= 23)) Finish("limit");
            var active = KeepsCombatActive(hit, directory);
            if (!HasCombat)
            {
                if (!active) continue;
                first = last = lastActivity = hit.Timestamp;
                id = Guid.NewGuid();
            }
            if (hit.Timestamp < first) continue;
            last = hit.Timestamp > last ? hit.Timestamp : last;
            if (active && hit.Timestamp > lastActivity) lastActivity = hit.Timestamp;
            events.Add(hit);
        }
        if (HasCombat && (now - lastActivity).TotalSeconds >= 12) Finish("idle");
    }

    private bool KeepsCombatActive(DamageEvent hit, Aion2EntityDirectory directory)
    {
        // Without a local identity retain the observation mode; never guess a party or pet owner.
        if (activityPlayer < 0) return true;
        if (hit.Amount <= 0) return false;
        var actor = directory.SummonOwnerOf(hit.SourceObjectId) ?? hit.SourceObjectId;
        if (hit.IsHeal)
            return actor == activityPlayer && hit.TargetObjectId != activityPlayer && !hit.IsTick;
        var target = directory.SummonOwnerOf(hit.TargetObjectId) ?? hit.TargetObjectId;
        return actor == activityPlayer || target == activityPlayer;
    }

    public void TogglePause()
    {
        Poll();
        if (!Paused) Finish("paused");
        Paused = !Paused;
        source.Poll(true);
    }

    public Encounter? Snapshot()
    {
        if (events.Count == 0) return null;
        var directory = (Aion2EntityDirectory)source.Entities;
        var inspected = directory.InspectedPlayers().ToDictionary(p => p.Name, StringComparer.Ordinal);
        var mapped = events.Select(hit => hit with { SourceObjectId = directory.SummonOwnerOf(hit.SourceObjectId) ?? hit.SourceObjectId }).ToArray();
        var actors = mapped.SelectMany(hit => new[] { hit.SourceObjectId, hit.TargetObjectId }).Distinct().Select(actor =>
        {
            var name = directory.NameFor(actor) ?? $"#{actor}";
            var hp = directory.HitPoints.Latest(actor);
            var unresolved = directory.IsSpawned(actor) && directory.IsKnownPlayer(actor) && directory.BossNpcIdOf(actor) is null;
            return new Participant(actor, name, directory.ClassOf(actor) ?? "", directory.IsKnownPlayer(actor) && !unresolved, directory.BossNpcIdOf(actor) is not null,
                inspected.TryGetValue(name, out var profile) && profile.CombatPower > 0 ? profile.CombatPower : null,
                actor == directory.LocalPlayerId, hp is { } sample && sample.At >= first ? sample.Hp : null, directory.HitPoints.HighestSeen(actor), unresolved);
        }).ToArray();
        // Do not dilute the finished boss's DPS while waiting for the inactivity timeout.
        var duration = Math.Max(0, (long)(last - first).TotalMilliseconds);
        var hits = mapped.Select(hit => new CombatEvent(Math.Max(0, (long)(hit.Timestamp - first).TotalMilliseconds), hit.SourceObjectId,
            hit.TargetObjectId, hit.SkillId, hit.Skill ?? "—", hit.Amount, hit.IsHeal, hit.IsCritical, hit.IsTick)).ToArray();
        return EncounterSources.Classify(new(2, id, new DateTimeOffset(first), "Global", protocol.GameVersion, "live", source.CurrentZone, "active", duration, actors, hits));
    }

    public void Finish(string reason = "manual")
    {
        var snapshot = Snapshot();
        if (snapshot is null) return;
        events.Clear();
        Completed?.Invoke(snapshot with { EndReason = reason });
    }

    public void Dispose() { source.Stop(); Poll(); Finish("closed"); source.Dispose(); }
}
