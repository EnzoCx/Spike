using AionDPS.Aion2;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat;
using Spike.Core;
using Spike.Engine;

internal static class BossAttemptChecks
{
    public static void Run(Aion2Protocol protocol)
    {
        using (var fight = new Fixture(protocol))
        {
            fight.Feed(fight.Hit(0), fight.Hit(10, actor: 2), fight.Hit(20, actor: 400), fight.Hit(30, actor: 2));
            var id = fight.Current.Id;
            Check(fight.Completed.Count == 0 && fight.Current.Events.Length == 4,
                "Same boss remains one attempt while allies and unidentified sources fight");
            fight.Poll(42);
            Check(!fight.Meter.HasCombat && fight.Meter.CanFinish && fight.Meter.Snapshot() is null,
                "Quiet boss goes idle during a phase and still allows manual finish");
            fight.Feed(fight.Hit(44, actor: 3, target: 200), fight.Hit(46, actor: 3, target: 999));
            Check(!fight.Meter.HasCombat, "Nearby boss and trash farming do not resume the idle attempt");
            fight.Feed(fight.Hit(60, actor: 2), fight.Hit(65));
            Check(fight.Current.Id == id && fight.Current.Events.Length == 6
                && fight.Current.Events.All(hit => hit.Target == 100), "Phase resumes the same archive without idle ambient traffic");
            Check(EncounterMath.Window(fight.Current, 100).DurationMs == 65000
                && fight.Current.Events.Sum(hit => hit.Amount) == 600, "Boss window spans phases and retains all damage once");
            fight.Poll(77);
            Check(fight.Completed.Count == 2 && fight.Completed.Select(encounter => encounter.Id).Distinct().Count() == 1,
                "Repeated idle snapshots update a single parse");
            var root = Path.Combine(Path.GetTempPath(), "Spike-boss-check-" + Guid.NewGuid().ToString("N"));
            var store = new EncounterStore(root);
            try
            {
                foreach (var encounter in fight.Completed) store.Save(encounter);
                Check(store.List().Count == 1 && store.Load(id).Events.Length == 6, "Phase resumption upserts one version 2 history file");
            }
            finally
            {
                File.Delete(Path.Combine(root, id.ToString("N") + ".json"));
                Directory.Delete(root);
            }
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Hp(0, 1000); fight.Hp(5, 700); fight.Hp(8, 1000); fight.Hp(9, 900);
            fight.Feed(fight.Hit(1), fight.Hit(5, actor: 2), fight.Hit(8), fight.Hit(9, actor: 2));
            Check(fight.Completed.Count == 1 && fight.Completed[0].EndReason == "boss-reset"
                && fight.Completed[0].Events.Length == 2 && fight.Current.Events.Length == 2
                && fight.Current.Id != fight.Completed[0].Id && fight.Current.StartedAt == fight.At(8),
                "Reset splits rapid attempts at its timestamp inside one drained batch");
            Check(EncounterMath.Window(fight.Completed[0], 100).DurationMs == 4000
                && EncounterMath.Window(fight.Current, 100).DurationMs == 1000, "Retry damage windows exclude the wipe and preceding attempt");
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Hp(0, 1000); fight.Hp(2, 500);
            fight.Feed(fight.Hit(1), fight.Hit(2));
            var first = fight.Current.Id;
            fight.Poll(14);
            fight.Hp(20, 1000);
            fight.Poll(20);
            Check(fight.Completed[^1].EndReason == "boss-reset", "Reset without damage seals an idle attempt");
            fight.Feed(fight.Hit(21, actor: 2));
            Check(!fight.Meter.HasCombat, "Other players cannot start a fresh retry without personal engagement");
            fight.Feed(fight.Hit(22));
            Check(fight.Current.Id != first && fight.Current.Events.Length == 1, "Same entity after a wipe starts a new parse");
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Hp(0, 1000); fight.Hp(3, 0);
            fight.Feed(fight.Hit(1), fight.Hit(3));
            var previous = fight.Completed.Single();
            Check(!fight.Meter.HasCombat && previous.EndReason == "boss-defeated" && previous.Events.Length == 2,
                "Boss death seals the parse after retaining its same-timestamp killing hit");
            fight.Hp(5, 1000);
            fight.Feed(fight.Hit(6));
            Check(fight.Current.Id != previous.Id, "Respawn of the same entity never resumes a defeated attempt");
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Feed(fight.Hit(1), fight.Hit(5, target: 200));
            Check(fight.Completed.Single().EndReason == "boss-changed" && fight.Current.Events.Single().Target == 200,
                "Another entity is a different boss even with the same NPC and name");
            var id = fight.Current.Id;
            fight.Directory.RegisterNpc(200, 2300104);
            fight.Feed(fight.Hit(7, actor: 2, target: 200));
            Check(!fight.Meter.HasCombat && fight.Completed[^1].Id == id,
                "A reused entity with a different boss NPC cannot resume through an ally");
            Check(fight.Completed[^1].Participants.Single(actor => actor.Id == 200).Name == Aion2BossCatalog.Find(2300171)!.Name,
                "Reusing an entity does not rename the previous attempt's boss");
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Hp(0, 1000); fight.Hp(2, 970); fight.Hp(3, 1000); fight.Hp(5, 700); fight.Hp(7, 800);
            fight.Feed(fight.Hit(1), fight.Hit(3, actor: 2), fight.Hit(7));
            Check(fight.Completed.Count == 0, "Minor and partial boss healing do not declare a wipe");
            var id = fight.Current.Id;
            fight.Poll(19);
            fight.Feed(fight.Hit(25, target: 999), fight.Hit(30));
            Check(fight.Current.Id == id && fight.Current.Events.Length == 5, "Personal add combat during a boss phase stays in the attempt");
            fight.Meter.Finish();
            fight.Feed(fight.Hit(31));
            Check(fight.Current.Id != id, "Manual finish prevents later phase resumption");
            id = fight.Current.Id;
            fight.Poll(43);
            fight.Meter.TogglePause();
            fight.Meter.TogglePause();
            fight.Feed(fight.Hit(50));
            Check(fight.Current.Id != id, "Pause discards a dormant boss continuation");
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Feed(fight.Hit(1, target: 2, heal: true), fight.Hit(2, actor: 2), fight.Hit(15, actor: 3));
            Check(fight.Completed.Count == 0 && fight.Meter.HasCombat,
                "A healer engages the boss through the recipient's observed combat");
            fight.Poll(27);
            var id = fight.Completed.Single().Id;
            fight.Feed(fight.Hit(30, target: 2, heal: true), fight.Hit(35, actor: 2));
            Check(fight.Current.Id == id, "Healing at a phase transition keeps the same boss parse");
        }

        using (var fight = new Fixture(protocol))
        {
            fight.Feed(fight.Hit(0, target: 999), fight.Hit(10, actor: 2, target: 100), fight.Hit(13, actor: 3, target: 100));
            Check(!fight.Meter.HasCombat, "An unrelated nearby boss cannot take ownership of a trash encounter");
            fight.Feed(fight.Hit(20));
            fight.Poll(32);
            fight.Directory.SetLocalCharacter(new(2, "Synthetic other player", 1, 1, [], fight.At(33)));
            fight.Feed(fight.Hit(33, actor: 3));
            Check(!fight.Meter.HasCombat, "Changing character clears a dormant boss");
        }

        using (var fight = new Fixture(protocol, registerBoss: false))
        {
            fight.Feed(fight.Hit(0), fight.Hit(8, actor: 2));
            var id = fight.Current.Id;
            fight.Directory.RegisterNpc(100, 2300171);
            fight.Poll(15);
            Check(fight.Meter.HasCombat && fight.Current.Id == id, "Late boss identification adopts previously observed boss activity");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly DateTime start = new(2026, 1, 1, 12, 0, 0);
        public LiveMeter Meter { get; }
        public Aion2EntityDirectory Directory { get; }
        public List<Encounter> Completed { get; } = [];
        public Encounter Current => Meter.Snapshot() ?? throw new Exception("Expected an active boss encounter.");

        public Fixture(Aion2Protocol protocol, bool registerBoss = true)
        {
            var source = new Aion2PacketCombatSource(protocol);
            Directory = (Aion2EntityDirectory)source.Entities;
            Directory.SetLocalCharacter(new(1, "Synthetic player", 1, 1, [], start));
            foreach (var actor in new[] { 1, 2, 3, 400 }) Directory.NoteClass(actor, "Assassin");
            Directory.NoteSpawned(400);
            if (registerBoss) Directory.RegisterNpc(100, 2300171);
            Directory.RegisterNpc(200, 2300171);
            Meter = new(source, protocol);
            Meter.Completed += encounter => { EncounterFile.Validate(encounter); Completed.Add(encounter); };
        }

        public DateTime At(int second) => start.AddSeconds(second);
        public DamageEvent Hit(int second, int actor = 1, int target = 100, bool heal = false) => new(At(second), actor, target, 100, heal);
        public void Hp(int second, long hp) => Directory.HitPoints.Note(100, At(second), hp);
        public void Feed(params DamageEvent[] hits) => Meter.Process(hits, hits.Max(hit => hit.Timestamp));
        public void Poll(int second) => Meter.Process([], At(second));
        public void Dispose() => Meter.Dispose();
    }
}
