using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using DPSMeter.Core;
using DPSMeter.Engine;

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
        }

        Feed(Hit(0, 2), Hit(1, 3), Hit(2, 2, 1, heal: true), Hit(3, 1, 1, heal: true));
        Check(!meter.HasCombat && completed.Count == 0, "Nearby farming or passive healing started a personal fight.");

        Feed(Hit(5), Hit(7), Hit(8, 400), Hit(9, 2, 200), Hit(18, 3, 300));
        Check(meter.HasCombat, "Fight ended before twelve seconds of personal inactivity.");
        Feed(Hit(19, 2, 200), Hit(20, 3, 300));
        Check(!meter.HasCombat && completed.Count == 1 && completed[0].EndReason == "idle",
            "Continuous nearby farming prevented the fight from ending or restarted it.");
        Check(completed[0].Events.Length == 5 && completed[0].Events.Any(hit => hit.Source == 400)
            && completed[0].Participants.Single(actor => actor.Id == 400).IsUnidentifiedSource,
            "Observed or unidentified damage inside the active segment was lost.");
        Check(EncounterMath.Window(completed[0], 100).DurationMs == 3000,
            "The inactivity timeout diluted the target damage window.");
        Feed(Hit(21));
        Check(meter.HasCombat && meter.Snapshot()!.Id != completed[0].Id, "Personal combat did not restart separately.");
        meter.Process([], start.AddSeconds(33));
        Check(!meter.HasCombat && completed[^1].DurationMs == 0, "Empty polls did not end combat at twelve seconds.");

        Feed(Hit(40, 100, 1), Hit(50, 100, 1));
        meter.Process([], start.AddSeconds(61));
        Check(meter.HasCombat, "Incoming damage did not keep combat active.");
        Feed(Hit(62, 2, 1, heal: true));
        Check(!meter.HasCombat, "Incoming healing kept an otherwise finished fight active.");

        Feed(Hit(70, 1, 2, heal: true), Hit(80, 1, 2, heal: true));
        Check(meter.HasCombat, "An active healer could not record combat.");
        Feed(Hit(92, 1, 2, heal: true, tick: true));
        Check(!meter.HasCombat, "Residual healing ticks restarted a finished fight.");

        directory.SetSummonOwner(400, 1);
        Feed(Hit(100, 400), Hit(110, 400));
        meter.Process([], start.AddSeconds(121));
        Check(meter.HasCombat, "Confirmed summon activity was not attributed to its owner.");
        Feed(Hit(122, 401));
        Check(!meter.HasCombat, "An unidentified source was guessed to belong to the local player.");

        Feed(Hit(130), Hit(135, amount: 0), Hit(140, amount: -1), Hit(141, amount: 1_000_000_000_001));
        meter.Process([], start.AddSeconds(142));
        Check(!meter.HasCombat, "Zero or invalid amounts refreshed the activity clock.");

        Feed(Hit(150), Hit(160), Hit(155));
        meter.Process([], start.AddSeconds(171));
        Check(meter.HasCombat, "Out-of-order events moved the activity clock backwards.");
        Feed(Hit(172));
        Check(meter.HasCombat && completed[^1].Events.Length == 3 && meter.Snapshot()!.Events.Length == 1,
            "A twelve-second personal gap within a batch did not split fights.");
        meter.Finish("manual");
        Feed(Hit(173, 2));
        Check(!meter.HasCombat && completed[^1].EndReason == "manual", "Ambient traffic restarted a manually ended fight.");

        using var unknownSource = new Aion2PacketCombatSource(protocol);
        using var unknown = new LiveMeter(unknownSource, protocol);
        unknown.Process([Hit(180, 2), Hit(190, 3)], start.AddSeconds(195));
        Check(unknown.HasCombat, "Missing identity disabled the observation fallback.");
        ((Aion2EntityDirectory)unknownSource.Entities).SetLocalCharacter(new(1, "Synthetic player", 1, 1, [], start));
        unknown.Process([Hit(196, 2)], start.AddSeconds(196));
        Check(!unknown.HasCombat, "Late identification kept unrelated observation active.");

        using var lateSource = new Aion2PacketCombatSource(protocol);
        using var late = new LiveMeter(lateSource, protocol);
        late.Process([Hit(200), Hit(210, 2)], start.AddSeconds(210));
        ((Aion2EntityDirectory)lateSource.Entities).SetLocalCharacter(new(1, "Synthetic player", 1, 1, [], start));
        late.Process([], start.AddSeconds(212));
        Check(!late.HasCombat, "Late identification did not rebuild the personal inactivity clock.");
        Console.WriteLine("PASS personal encounter lifecycle: ambient farming, restart, incoming damage, healer, summons, late identity, retained events and timing");
    }
}
