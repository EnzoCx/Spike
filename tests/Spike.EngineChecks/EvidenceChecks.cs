using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using Spike.Core;
using Spike.Engine;

internal static class EvidenceChecks
{
    public static void Run(Aion2Protocol protocol)
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0);
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
        var hp = new Aion2HitPoints();
        Check(hp.ObservedDeaths(1, start, start.AddSeconds(10)) is null, "Missing HP is unknown, not zero deaths");
        foreach (var (second, value) in new[] { (-1, 100), (1, 0), (2, 0), (3, 100), (4, 0), (5, 0), (8, 50), (9, 0) })
            hp.Note(1, start.AddSeconds(second), value);
        Check(hp.ObservedDeaths(1, start, start.AddSeconds(5)) == 2 && hp.ObservedDeaths(1, start.AddSeconds(2), start.AddSeconds(5)) == 1,
            "Deaths count positive-to-zero transitions within the attempt, deduplicating repeated zeros and respecting resurrection");
        hp.Note(2, start, 0);
        Check(hp.ObservedDeaths(2, start, start.AddSeconds(5)) == 0, "An initial zero does not invent a death during capture");

        using var source = new Aion2PacketCombatSource(protocol);
        using var meter = new LiveMeter(source, protocol);
        var directory = (Aion2EntityDirectory)source.Entities;
        directory.RegisterNpc(100, 2300171);
        directory.SetLocalCharacter(new(1, "Synthetic A", 1, 1, [], start, ServerId: 1));
        directory.Register(2, "Synthetic B");
        directory.NoteClass(1, "Sorcerer"); directory.NoteClass(2, "Sorcerer");
        directory.NoteParty(["Synthetic A", "Synthetic B"], start);
        directory.NoteSpawned(400, start); directory.SetSummonOwner(400, 1);
        directory.NoteSpawned(401, start); directory.SetSummonOwner(401, 2);
        DamageEvent Hit(int second, int actor = 1) => new(start.AddSeconds(second), actor, 100, 100, false, SkillId: 15100000);
        meter.Process([Hit(0), Hit(1, 400), Hit(2, 401)], start.AddSeconds(2));
        directory.NoteSpawned(400, start.AddSeconds(3)); directory.SetSummonOwner(400, 2);
        meter.Process([Hit(4, 400), Hit(5)], start.AddSeconds(5));
        var fight = meter.Snapshot()!;
        Check(fight.Events[1].Source == 1 && fight.Events[1].OriginalSource == 400 && fight.Events[3].Source == 2,
            "Two same-class owners and reused summon IDs never reassign earlier damage");
        Check(fight.Events.Sum(e => e.Amount) == 500 && fight.Events.Where(e => e.OriginalSource is not null).All(e => e.Attribution == "owner-id"),
            "Original summon IDs and explicit attribution evidence are retained without duplicating damage");
        EncounterFile.Validate(fight);
        directory.NoteSpawned(400, start.AddSeconds(6));
        Check(directory.SummonOwnerOf(400) is null, "A spawn without owner clears the previous owner");
        directory.SetSummonOwnerName(400, "Synthetic B");
        Check(directory.SummonOwnerOf(400) == 2 && directory.OwnerEvidence(400) == "owner-name", "Unique owner-name evidence resolves explicitly");
        directory.Register(3, "Synthetic B");
        Check(directory.SummonOwnerOf(400) is null, "Ambiguous owner names never choose a player");
        directory.AdvanceTime(start.AddSeconds(91));
        Check(directory.PartyNames.Count == 0, "Party evidence expires even without a later roster frame");
        directory.NoteClass(900, "Sorcerer");
        directory.NotePartyClass("Unseen teammate", "Sorcerer"); directory.NoteParty(["Unseen teammate"], start.AddSeconds(92));
        Check(!directory.HasName(900), "A roster class never supplies an unproven combat identity");
        var unnamed = new Aion2EntityDirectory(); unnamed.NoteClass(900, "Sorcerer");
        Check(unnamed.LocalPlayerId == -1 && unnamed.NameFor(900) == "Player #900", "Casting the most spells never identifies the local player");

        directory.HitPoints.Note(1, start.AddSeconds(4), 100);
        directory.HitPoints.Note(1, start.AddSeconds(6), 0);
        directory.HitPoints.Note(1, start.AddSeconds(7), 0);
        meter.Process([], start.AddSeconds(7));
        fight = meter.Snapshot()!;
        Check(fight.Participants.Single(p => p.Id == 1).ObservedDeaths == 1 && fight.DurationMs == 5000,
            "A death after the last hit is counted without extending the DPS duration");
        var finished = new List<Encounter>(); meter.Completed += finished.Add;
        directory.ResetContext(start.AddSeconds(8));
        meter.Process([], start.AddSeconds(8));
        Check(finished.Single().Participants.Single(p => p.Id == 1).Name == "Synthetic A" && !meter.CanFinish
            && directory.SummonOwnerOf(401) is null && directory.LocalPlayerId == -1,
            "Context reset seals the previous report and clears stale identity, owner and HP evidence");
        directory.Register(1, "New player"); directory.SetSummonOwner(400, 1);
        var generation = directory.ContextVersion;
        directory.Register(1, "Different player");
        Check(directory.ContextVersion > generation && directory.SummonOwnerOf(400) is null,
            "A conflicting explicit identity invalidates stale owner mappings");

        // Use the actual decoder with a synthetic HP frame (one i64 current-HP entry).
        var decodedDirectory = new Aion2EntityDirectory();
        var decoder = new Aion2FrameDecoder(protocol, decodedDirectory);
        var opcode = Enumerable.Range(0, 65536).Single(code => protocol.FamilyOf(code) == OpcodeFamily.HpUpdate);
        byte[] HpFrame(long amount) => new byte[] { (byte)(opcode >> 8), (byte)opcode, 1, 2, 1, 0 }.Concat(BitConverter.GetBytes(amount)).ToArray();
        decoder.Decode(HpFrame(500), start);
        decoder.Decode(HpFrame(0), start.AddSeconds(1));
        decoder.Decode(HpFrame(0), start.AddSeconds(2));
        Check(decodedDirectory.HitPoints.ObservedDeaths(1, start, start.AddSeconds(2)) == 1,
            "Synthetic wire HP frames produce a deduplicated observed death through the actual decoder");

        using var boundedSource = new Aion2PacketCombatSource(protocol);
        using var bounded = new LiveMeter(boundedSource, protocol);
        ((Aion2EntityDirectory)boundedSource.Entities).SetLocalCharacter(new(1, "Synthetic bounded", 1, 1, [], start));
        ((Aion2EntityDirectory)boundedSource.Entities).RegisterNpc(100, 2300171);
        var segments = new List<Encounter>(); bounded.Completed += segments.Add;
        bounded.Process(Enumerable.Range(0, 4100).Select(i => Hit(0) with
        { OriginalSource = 1000 + i, Attribution = "owner-id", AttributionCaptured = true }).ToArray(), start);
        segments.Add(bounded.Snapshot()!);
        foreach (var segment in segments) EncounterFile.Validate(segment);
        Check(segments.Count == 2 && segments.Sum(s => s.Events.Length) == 4100,
            "Retained original sources respect the entity limit without losing damage across segments");
    }
}
