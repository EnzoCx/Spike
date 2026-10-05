using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using Spike.Core;
using Spike.Engine;

internal static class RaidChecks
{
    public static void Run(Aion2Protocol protocol)
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
        var start = new DateTime(2026, 1, 1, 12, 0, 0);
        var entities = new Aion2EntityDirectory(); entities.Register(1086, "Synthetic attacker"); entities.Register(2, "Synthetic support");
        var tracker = new BetaBuffTracker();
        var hit = new DamageEvent(start.AddSeconds(1), 1086, 100, 1105, false, SkillId: 15100000);
        foreach (var opcode in new[] { 0x2A38, 0x2B38 })
        {
            tracker.Reset(); tracker.Observe(Frame(opcode), start, entities);
            Check(tracker.Estimate(hit, entities).Raid == new RaidCredit(2, 18190000, 105), "Candidate aura opcode produces explicitly provisional contribution");
            Check(tracker.Estimate(hit with { Timestamp = start.AddSeconds(10) }, entities).Raid is null, "Aura expires at its advertised boundary");
            Check(tracker.Estimate(hit with { IsHeal = true }, entities).Raid is null, "Healing never receives rDPS credit");
            Check(tracker.Estimate(hit with { OriginalSource = 999 }, entities).Raid is null, "Summons do not inherit owner auras by assumption");
        }
        var frame = Frame();
        for (var length = 0; length < frame.Length; length++)
        {
            tracker.Reset(); tracker.Observe(frame[..length], start, entities);
            Check(tracker.Estimate(hit, entities).Raid is null, "Truncated aura cannot establish evidence");
        }
        tracker.Observe(frame, start, entities); tracker.Observe(Frame(duration: 0), start, entities);
        Check(tracker.Estimate(hit, entities).Raid is null, "Zero duration invalidates prior evidence");
        tracker.Observe(frame, start, entities); tracker.Observe(Frame(provider: 3), start, entities);
        Check(tracker.Estimate(hit, entities).Raid is null, "Competing providers are ambiguous even when one identity is unknown");
        tracker.Reset(); tracker.Observe(Frame(provider: 3), start, entities);
        Check(tracker.Estimate(hit, entities).Raid is null, "Unknown support cannot receive guessed contribution");
        tracker.Reset(); tracker.Observe(Frame(provider: 1086, skill: 17410000), start, entities);
        Check(tracker.Estimate(hit, entities).Raid?.Bonus == 0, "Self aura is observed without transferring damage");
        tracker.Reset(); tracker.Observe(Frame(skill: 17070000), start, entities);
        Check(tracker.Estimate(hit, entities).Raid is null, "Unsupported debuffs are excluded");
        tracker.Observe(frame, start, entities); entities.NoteSpawned(1086, start.AddMilliseconds(500));
        Check(tracker.Estimate(hit, entities).Raid is null, "Reused target ID invalidates old aura evidence");
        entities.ResetContext(start); entities.Register(1086, "Synthetic attacker"); entities.Register(2, "Synthetic support");
        Check(tracker.Estimate(hit, entities).Raid is null, "Context reset discards aura evidence");
        var decoder = new Aion2FrameDecoder(protocol, entities);
        decoder.Decode(frame, start);
        var damage = decoder.Decode(Convert.FromHexString("04388484030600be08fe26a8004f02000002433baf4101000000ba4ffd020100"), start.AddSeconds(1)).Single();
        Check(damage.Amount == 381 && damage.Raid is { Provider: 2, Bonus: 36 }, "Actual damage decoder preserves raw damage and attaches estimated aura credit");
        decoder.ResetBetaBuffs();
        Check(decoder.Decode(Convert.FromHexString("04388484030600be08fe26a8004f02000002433baf4101000000ba4ffd020100"), start.AddSeconds(2)).Single().Raid is null,
            "Resetting evidence prevents stale credit after capture gaps");
        using var source = new Aion2PacketCombatSource(protocol);
        using var meter = new LiveMeter(source, protocol);
        var directory = (Aion2EntityDirectory)source.Entities;
        directory.Register(1086, "Synthetic attacker"); directory.Register(2, "Synthetic support");
        meter.Process([damage], damage.Timestamp);
        var fight = meter.Snapshot()!; EncounterFile.Validate(fight);
        Check(fight.RdpsModel == RaidDamage.Model && fight.Events.Single().Raid == damage.Raid && fight.Participants.Any(p => p.Id == 2 && p.IsPlayer),
            "Live aggregation retains identified support with no personal damage and freezes credit in the archive");
    }

    private static byte[] Frame(int opcode = 0x2A38, uint duration = 10000, int provider = 2, uint skill = 18190000)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write((byte)(opcode >> 8)); writer.Write((byte)opcode); Id(1086);
        writer.Write((byte)0); writer.Write((byte)1); Id(1);
        writer.Write(skill * 10 + 1); writer.Write(duration); writer.Write(0u); writer.Write(0u); Id(provider);
        return stream.ToArray();
        void Id(int id) { uint value = (uint)id; while (value >= 128) { writer.Write((byte)(value | 128)); value >>= 7; } writer.Write((byte)value); }
    }
}
