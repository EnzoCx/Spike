using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using Spike.Core;
using Spike.Engine;

internal static class LiveMeterChecks
{
    public static void Run(Aion2Protocol protocol)
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0);
        using var source = new Aion2PacketCombatSource(protocol);
        using var meter = new LiveMeter(source, protocol);
        var directory = (Aion2EntityDirectory)source.Entities;
        var completed = new List<Encounter>();
        meter.Completed += completed.Add;
        directory.SetLocalCharacter(new(1, "Synthetic player", 1, 1, [], start));
        directory.NoteClass(1, "Assassin");
        directory.NoteClass(400, "Sorcerer");
        directory.NoteSpawned(400);

        DamageEvent Hit(int second, int actor = 1, int target = 100, bool heal = false,
            bool tick = false, long amount = 100) => new(start.AddSeconds(second), actor, target, amount, heal, IsTick: tick);
        void Feed(params DamageEvent[] hits) => meter.Process(hits, hits.Max(hit => hit.Timestamp));
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Console.WriteLine("PASS " + message);
        }

        Feed(Hit(0), Hit(1, 2), Hit(2, 100, 1), Hit(3, 1, 2, heal: true), Hit(4, 1, 1, heal: true));
        meter.Process([], start.AddSeconds(20));
        Check(!meter.HasCombat && !meter.CanFinish && meter.Snapshot() is null && completed.Count == 0,
            "Personal and nearby farming, incoming trash damage and healing never start a fight");
        meter.Finish();
        Check(completed.Count == 0, "Manual finish never saves a trash encounter");

        directory.RegisterNpc(100, 2300171);
        Feed(Hit(30), Hit(32), Hit(33, 400), Hit(34, target: 999), Hit(35, 2, 999), Hit(36, 999, 1));
        var id = meter.Snapshot()!.Id;
        Check(meter.Snapshot()!.Events.Length == 3 && meter.Snapshot()!.Participants.Single(actor => actor.Id == 400).IsUnidentifiedSource,
            "Only boss damage is retained, including unidentified sources, without personal or nearby trash");
        Feed(Hit(45, target: 999));
        Check(!meter.HasCombat && completed.Single().Id == id && completed[0].DurationMs == 3000
            && EncounterMath.Window(completed[0], 100).DurationMs == 3000,
            "Farming does not prolong a boss fight or dilute its damage window");
        Feed(Hit(46, target: 999));
        Check(!meter.HasCombat && completed.Count == 1, "Personal farming never wakes an idle boss attempt");
        Feed(Hit(47, target: 2, heal: true));
        Check(!meter.HasCombat && completed.Count == 1, "Healing alone never wakes an idle boss attempt");
        Feed(Hit(50, actor: 2));
        Check(meter.HasCombat && meter.Snapshot()!.Id == id && meter.Snapshot()!.Events.Length == 4,
            "Boss damage resumes the same attempt after farming");
        Feed(Hit(51, target: 2, heal: true), Hit(52, actor: 2, target: 1, heal: true));
        Check(meter.Snapshot()!.Events.Count(hit => hit.Heal) == 2, "Healing remains available inside a boss fight");
        Feed(Hit(60, target: 2, heal: true), Hit(62, target: 2, heal: true));
        Check(!meter.HasCombat, "Continuous personal healing cannot prolong a boss after its last damage");
        meter.Finish();

        Feed(Hit(64, 100, 1), Hit(70, 100, 1));
        meter.Process([], start.AddSeconds(81));
        Check(meter.HasCombat, "Incoming boss damage engages and maintains combat");
        Feed(Hit(82, 2, 1, heal: true));
        Check(!meter.HasCombat, "Incoming healing cannot maintain or resume an idle boss fight");
        meter.Finish();

        Feed(Hit(90, 1, 2, heal: true), Hit(91, 2));
        Check(meter.HasCombat, "Healing an engaged player allows a healer to record the boss");
        meter.Finish();
        Feed(Hit(100, 1, 2, heal: true, tick: true), Hit(101, 2));
        Check(!meter.HasCombat, "Residual healing ticks cannot engage a new boss");

        directory.SetSummonOwner(400, 1);
        Feed(Hit(110, 400));
        Check(meter.HasCombat && meter.Snapshot()!.Events.Single().OriginalSource == 400,
            "Confirmed summons engage bosses with their original source preserved");
        Feed(Hit(115, amount: 0), Hit(120, amount: -1), Hit(121, amount: 1_000_000_000_001));
        meter.Process([], start.AddSeconds(122));
        Check(!meter.HasCombat, "Zero or invalid amounts never refresh boss activity");
        meter.Finish();
        Feed(Hit(130), Hit(140), Hit(135));
        meter.Process([], start.AddSeconds(151));
        Check(meter.HasCombat && meter.Snapshot()!.Events.Length == 3,
            "Out-of-order damage does not move the boss activity clock backwards");
        meter.Finish();
        Feed(Hit(152, 2));
        Check(!meter.HasCombat, "Other players cannot restart a manually finished boss");

        using var unknownSource = new Aion2PacketCombatSource(protocol);
        using var unknown = new LiveMeter(unknownSource, protocol);
        var unknownDirectory = (Aion2EntityDirectory)unknownSource.Entities;
        unknown.Process([Hit(180, 2), Hit(190, 3)], start.AddSeconds(195));
        Check(!unknown.HasCombat, "Observation without a local identity still excludes unconfirmed monsters");
        unknownDirectory.RegisterNpc(100, 2300171);
        unknown.Process([], start.AddSeconds(196));
        Check(unknown.HasCombat && unknown.Snapshot()!.Events.Length == 2,
            "Observation without a local identity works once the boss is confirmed");

        using var expiredSource = new Aion2PacketCombatSource(protocol);
        using var expired = new LiveMeter(expiredSource, protocol);
        expired.Process([Hit(200)], start.AddSeconds(200));
        ((Aion2EntityDirectory)expiredSource.Entities).RegisterNpc(100, 2300171);
        expired.Process([], start.AddSeconds(231));
        Check(!expired.HasCombat && !expired.CanFinish, "Unconfirmed hits expire after thirty seconds");

        using var reusedSource = new Aion2PacketCombatSource(protocol);
        using var reused = new LiveMeter(reusedSource, protocol);
        var reusedDirectory = (Aion2EntityDirectory)reusedSource.Entities;
        reused.Process([Hit(240)], start.AddSeconds(240));
        reusedDirectory.NoteSpawned(100, start.AddSeconds(241));
        reusedDirectory.RegisterNpc(100, 2300171);
        reused.Process([], start.AddSeconds(242));
        Check(!reused.HasCombat, "Reused monster IDs never turn earlier trash damage into boss damage");
        reused.Process([Hit(243)], start.AddSeconds(243));
        Check(reused.Snapshot()!.Events.Length == 1, "A confirmed new boss retains only its own damage");

        var buffer = new BossEventBuffer();
        var bufferDirectory = new Aion2EntityDirectory();
        Check(buffer.Drain(Enumerable.Range(0, 5000).Select(_ => Hit(250)), bufferDirectory, start.AddSeconds(250)).Length == 0,
            "Heavy farming never produces ready combat events");
        bufferDirectory.RegisterNpc(100, 2300171);
        Check(buffer.Drain([], bufferDirectory, start.AddSeconds(251)).Length == 4096,
            "Delayed boss identification has a bounded memory buffer");
    }
}
