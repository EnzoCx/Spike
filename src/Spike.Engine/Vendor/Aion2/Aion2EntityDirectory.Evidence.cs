namespace AionDPS.Aion2;

public sealed partial class Aion2EntityDirectory
{
    private DateTime observedAt;
    private readonly Dictionary<int, DateTime> spawnedAt = new();
    public int ContextVersion { get; private set; }
    public DateTime ContextStartedAt { get; private set; }

    public void AdvanceTime(DateTime at)
    {
        lock (_gate) { if (at > observedAt) observedAt = at; }
    }

    public string? OwnerEvidence(int id)
    {
        lock (_gate)
            return SummonOwnerOf(id) is null ? null : _summonOwners.ContainsKey(id) ? "owner-id" : "owner-name";
    }

    public bool HasDirectIdentity(int id)
    {
        lock (_gate) return !_spawned.Contains(id) && (_names.ContainsKey(id) || _explicitLocalId == id);
    }

    public bool EvidenceApplies(int id, DateTime at)
    {
        lock (_gate) return at >= ContextStartedAt && (!spawnedAt.TryGetValue(id, out var spawn) || at >= spawn);
    }

    private void ForgetSpawn(int id, DateTime at)
    {
        _summonOwners.Remove(id); _summonOwnerNames.Remove(id); _bossNpcs.Remove(id);
        if (_spawned.Contains(id)) { _classVotes.Remove(id); HitPoints.Remove(id); }
        if (_names.Remove(id, out var name) && _ids.GetValueOrDefault(name) == id) _ids.Remove(name);
        _seen.Remove(id); _guilds.Remove(id); _detailedStats.Remove(id);
        spawnedAt[id] = at;
    }

    public void ResetContext(DateTime at)
    {
        lock (_gate)
        {
            ContextVersion++; ContextStartedAt = at; AdvanceTime(at);
            _names.Clear(); _ids.Clear(); _classVotes.Clear(); _guilds.Clear(); _seen.Clear();
            _bossNpcs.Clear(); _spawned.Clear(); spawnedAt.Clear();
            _summonOwners.Clear(); _summonOwnerNames.Clear(); _resolvedOwners.Clear();
            _partySeen.Clear(); _partyClasses.Clear();
            _roster.Clear(); _notPlayers.Clear(); _detailedStats.Clear();
            _explicitLocalId = -1; _character = null; HitPoints.Clear();
        }
    }
}
