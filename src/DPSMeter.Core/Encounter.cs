namespace DPSMeter.Core;

public sealed record Participant(int Id, string Name, string ClassName, bool IsPlayer, bool IsBoss = false,
    long? CombatPower = null, bool IsSelf = false, long? CurrentHp = null, long? HighestHp = null,
    bool IsUnidentifiedSource = false);
public sealed record CombatEvent(long AtMs, int Source, int Target, int SkillId, string Skill,
    long Amount, bool Heal, bool Critical, bool Tick);
public sealed record Encounter(int Version, Guid Id, DateTimeOffset StartedAt, string Region, string Patch,
    string Origin, string Zone, string EndReason, long DurationMs, Participant[] Participants, CombatEvent[] Events)
{
    public string Title => Participants.FirstOrDefault(actor => actor.IsBoss)?.Name
        ?? (string.IsNullOrWhiteSpace(Zone) ? "—" : Zone);
    public double Seconds => Math.Max(1, DurationMs / 1000d);
}
public sealed record MeterRow(int Id, string Name, string ClassName, long Total, double PerSecond,
    double Share, int Hits, double CriticalRate, double MaximumShare);
public sealed record SpellRow(int Id, string Name, long Total, double PerSecond, double Share,
    int Hits, int Ticks, double CriticalRate);
