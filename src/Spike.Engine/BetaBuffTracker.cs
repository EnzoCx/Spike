using System.Buffers.Binary;
using AionDPS.Aion2;
using AionDPS.Combat;

namespace Spike.Engine;

/// <summary>Experimental wire hypothesis documented in RDPS-RESEARCH.md. No raw frames retained.
/// Only two auras, level-one nominal 10.5% treated as a final multiplier. Never calibrated.</summary>
internal sealed class BetaBuffTracker
{
    private sealed record Aura(int Target, int Provider, int Skill, DateTime At, DateTime Until);
    private readonly Dictionary<(int Target, int Provider, int Skill), Aura> active = new();
    private int context;

    public void Reset() => active.Clear();

    public void Observe(ReadOnlySpan<byte> frame, DateTime at, Aion2EntityDirectory entities)
    {
        if (context != entities.ContextVersion) { Reset(); context = entities.ContextVersion; }
        foreach (var key in active.Where(pair => pair.Value.Until <= at).Select(pair => pair.Key).ToArray()) active.Remove(key);
        if (frame.Length < 2 || frame.Length > 16384) return;
        var opcode = BinaryPrimitives.ReadUInt16BigEndian(frame);
        if (opcode is not (0x2A38 or 0x2B38)) return;
        var p = 2;
        if (!ReadId(frame, ref p, out var target) || p + 2 > frame.Length) { Reset(); return; }
        p += 2; // Two flags, semantics not yet validated.
        if (!ReadId(frame, ref p, out _) || p + 16 > frame.Length) { Invalidate(target); return; }
        var effect = BinaryPrimitives.ReadUInt32LittleEndian(frame[p..]);
        var duration = BinaryPrimitives.ReadUInt32LittleEndian(frame[(p + 4)..]);
        p += 16; // Effect, duration, unknown word and server clock.
        if (!ReadId(frame, ref p, out var provider) || p != frame.Length) { Invalidate(target); return; }
        var skill = (int)(effect / 10 / 10000 * 10000);
        if (skill is not (17410000 or 18190000)) return;
        // Zero/invalid duration means loss of evidence, not a claimed removal opcode.
        if (duration is 0 or > 3_600_000 || target == 0 || provider == 0) { Invalidate(target); return; }
        if (active.Count >= 4096) Reset();
        active[(target, provider, skill)] = new(target, provider, skill, at, at.AddMilliseconds(duration));
    }

    private void Invalidate(int target)
    {
        foreach (var key in active.Keys.Where(key => key.Target == target).ToArray()) active.Remove(key);
    }

    public DamageEvent Estimate(DamageEvent hit, Aion2EntityDirectory entities)
    {
        if (hit.IsHeal || hit.Amount <= 0 || context != entities.ContextVersion) return hit;
        var source = hit.OriginalSource ?? hit.SourceObjectId;
        var effects = active.Values.Where(a => a.Target == source && a.At <= hit.Timestamp && a.Until > hit.Timestamp
            && entities.EvidenceApplies(a.Target, a.At) && entities.EvidenceApplies(a.Provider, a.At)).ToArray();
        // Conflicting auras/unknown ranks cannot be resolved by assuming a class or a roster.
        if (effects.Length != 1) return hit;
        var aura = effects[0];
        if (!entities.HasDirectIdentity(aura.Provider) || !entities.HasDirectIdentity(hit.SourceObjectId)) return hit;
        var bonus = aura.Provider == hit.SourceObjectId ? 0 : (long)decimal.Round(hit.Amount * 105m / 1105m, 0, MidpointRounding.ToEven);
        return hit with { Raid = new(aura.Provider, aura.Skill, bonus) };
    }

    private static bool ReadId(ReadOnlySpan<byte> data, ref int p, out int id)
    {
        uint value = 0;
        id = 0;
        for (var i = 0; i < 5 && p < data.Length; i++)
        {
            var next = data[p++];
            if (i == 4 && next > 15) return false;
            value |= (uint)(next & 127) << (7 * i);
            if (next < 128) { if (i > 0 && next == 0) return false; id = unchecked((int)value); return true; }
        }
        return false;
    }
}
