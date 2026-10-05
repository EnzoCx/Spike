using System.IO;
using System.Text.Json;
using Spike.Core;

var count = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
    Console.WriteLine($"PASS {label}");
    count++;
}
void Reject(CombatLog invalid, string label)
{
    try { Combat.Validate(invalid); }
    catch (InvalidDataException) { Check(true, label); return; }
    throw new Exception($"Accepted invalid log: {label}");
}
var log = new CombatLog(1, "test", "Global", "test", "Boss", "Normal", 10_000, false,
    [new("a", "PrivateName", "Mage"), new("b", "Idle", "Cleric")],
    [new("1", 0, "a", "Fire", 1000, true), new("2", 9000, "a", "Ice", 500, false)]);
var summary = Combat.Summarize(log);
Check(summary.TotalDamage == 1500 && summary.Players[0].Dps == 150, "DPS uses explicit encounter duration, including idle time");
Check(summary.Players[0].CriticalRate == 50 && summary.Players[0].Share == 100, "Critical rate and damage share");
Check(summary.Players[1].Dps == 0, "Idle participants are retained");
Check(Combat.Skills(log, "a")[0].Dps == 100, "Skill DPS uses encounter duration");
Reject(log with { DurationMs = 0 }, "Reject zero duration");
Reject(log with { Hits = [log.Hits[0], log.Hits[0]] }, "Reject duplicate events");
Reject(log with { Hits = [log.Hits[0] with { ActorId = "missing" }] }, "Reject unknown actors");
Reject(log with { Hits = [log.Hits[0] with { Damage = -1 }] }, "Reject negative damage");
Reject(log with { Hits = [log.Hits[0] with { OffsetMs = 10_001 }] }, "Reject out-of-range timestamps");
Reject(log with { Actors = [log.Actors[0], log.Actors[0]] }, "Reject duplicate actors");
Reject(log with { SchemaVersion = 2 }, "Reject unsupported schema");
Check(Combat.Summarize(log with { Hits = [] }).Players.All(row => row.Share == 0), "Empty encounter has finite zero shares");
Check(Combat.Summarize(log with { Hits = log.Hits.Reverse().ToArray() }).TotalDamage == 1500, "Order does not affect totals");
var anonymous = Combat.Anonymize(log);
Check(!JsonSerializer.Serialize(anonymous, Combat.Json).Contains("PrivateName"), "Export removes player names");
Check(anonymous.Actors[0].Id != "a" && anonymous.Hits[0].ActorId == anonymous.Actors[0].Id, "Export replaces identifiers consistently");
Check(Combat.Summarize(Combat.Read(JsonSerializer.Serialize(anonymous, Combat.Json))).TotalDamage == 1500, "Export/import preserves totals");
Check(Combat.Summarize(Demo.Create()).Players.Count == 4 && Demo.Create().IsDemo, "Synthetic fixture is labelled and valid");
var fight = new Encounter(2, Guid.NewGuid(), DateTimeOffset.UtcNow, "Global", "test", "test", "", "idle", 10_000,
    [new(1, "One", "Ranger", true), new(2, "Two", "Cleric", true), new(100, "Boss", "", false, true)],
    [new(0, 1, 100, 10, "Arrow", 1000, false, true, false), new(5000, 1, 100, 10, "Arrow", 300, false, false, true),
     new(7000, 2, 1, 20, "Heal", 500, true, false, false), new(9000, 100, 1, 30, "Boss attack", 900, false, false, false)]);
var players = EncounterMath.Players(fight, false);
Check(players.Count == 1 && players[0].Total == 1300 && players[0].PerSecond == 130, "Live math excludes NPC attacks from outgoing player DPS");
Check(players[0].Hits == 1 && players[0].CriticalRate == 100, "Ticks contribute damage but not hit/crit denominator");
Check(EncounterMath.Players(fight, true)[0].PerSecond == 50, "Healing is separated from damage");
Check(EncounterMath.Players(fight, false, 1).Count == 0, "Target filter isolates target-specific events");
Check(EncounterMath.Spells(fight, 1, false)[0].Ticks == 1, "Spell breakdown preserves tick counts");
Check(Math.Abs(EncounterMath.Timeline(fight, false).Sum() * fight.Seconds / 60 - 1300) < 0.001, "Timeline integral matches total damage");
var bossWindow = EncounterMath.Window(fight, 100);
Check(bossWindow == (0L, 5000L) && EncounterMath.Players(fight, false, 100).Single().PerSecond == 260,
    "Boss clock excludes later healing and incoming NPC attacks");
var mixed = fight with
{
    Events = fight.Events.Concat(new CombatEvent[] {
    new(8000, 1, 2, 10, "Arrow", 700, false, false, false) }).ToArray()
};
Check(EncounterMath.Players(mixed, false, 100).Single().Total == 1300 && EncounterMath.Players(mixed, false).Single().Total == 2000,
    "Boss scope excludes damage to other targets while all-target totals preserve it");
var delayed = fight with { DurationMs = 15000, Events = fight.Events.Select(e => e with { AtMs = e.AtMs + 5000 }).ToArray() };
Check(EncounterMath.Window(delayed, 100) == (5000L, 5000L) && Math.Abs(EncounterMath.Timeline(delayed, false, 100, actor: 1).Sum() * 5 / 60 - 1300) < .001,
    "Target timeline starts at first target hit and preserves selected-player damage");
Check(EncounterMath.Spells(fight, 1, false, 100).Single().PerSecond == 260 && EncounterMath.Timeline(fight, false, 100, actor: 2).Sum() == 0,
    "Skill DPS and player timeline share the selected scope");
var variants = fight with
{
    Events = [
    new(0, 1, 100, 13350341, "Heart Gore", 469128, false, true, false),
    new(5000, 1, 100, 13350340, "Heart Gore", 161538, false, false, false),
    new(6000, 1, 100, 13350000, "Different imported skill", 50, false, false, false),
    new(7000, 1, 100, 10, "Unknown", 10, false, false, false),
    new(8000, 1, 100, 11, "Unknown", 20, false, false, false)]
};
var merged = EncounterMath.Spells(variants, 1, false);
Check(merged.Count == 4 && merged[0].Total == 630666 && merged[0].Hits == 2 && merged[0].CriticalRate == 50,
    "Class skill variants merge totals and hit rates; distinct names and unknown IDs stay separate");
var metadata = fight with
{
    Participants = [fight.Participants[0] with { CombatPower = 83500, IsSelf = true }, fight.Participants[1],
    fight.Participants[2] with { CurrentHp = 0, HighestHp = 11067000 }]
};
Check(EncounterFile.Read(JsonSerializer.Serialize(metadata, EncounterFile.Json)).Participants.SequenceEqual(metadata.Participants),
    "Optional power, self marker and observed HP round-trip");
Check(!EncounterFile.PrivateExport(metadata).Participants.Any(p => p.IsSelf), "Shared export removes local-player marker");
var legacyJson = JsonSerializer.Serialize(fight, EncounterFile.Json).Replace(",\"combatPower\":null,\"isSelf\":false,\"currentHp\":null,\"highestHp\":null", "");
Check(EncounterFile.Read(legacyJson).Participants.All(p => p.CombatPower is null && p.CurrentHp is null), "Existing v2 files without added metadata remain readable");
var exported = EncounterFile.PrivateExport(fight);
Check(!exported.Participants.Any(actor => actor.Name == "One") && exported.Origin == "shared-unverified", "Public export pseudonymizes and never asserts authenticity");
Check(EncounterFile.Read(JsonSerializer.Serialize(exported, EncounterFile.Json)).Events.Length == 4, "Version 2 JSON round trip");
// Synthetic fixture reproducing anonymous effect-only actors, not a captured fight.
var sources = fight with
{
    Origin = "live",
    Participants = [
    new(1, "Named mage", "Sorcerer", true), new(2, "Player #2", "Chanter", true),
    new(3, "Player #3", "Sorcerer", true), new(4, "Player #4", "Ranger", true), fight.Participants.Single(p => p.Id == 100)],
    Events = [new(0, 1, 100, 15050140, "Blaze", 1000, false, false, false),
        new(1000, 2, 100, 18050230, "Bursting Blow", 2000, false, false, false),
        new(2000, 3, 100, 15390012, "Fire Wall", 300, false, true, false),
        new(3000, 3, 100, 15200022, "Cold Storm", 100, false, false, true),
        new(4000, 4, 100, 14170131, "Explosion", 50, false, false, false)]
};
var classified = EncounterSources.Classify(sources);
Check(classified.Participants.Count(p => p.IsPlayer) == 2 && classified.Participants.Count(p => p.IsUnidentifiedSource) == 2,
    "Anonymous effect-only actors are unconfirmed sources; anonymous Chanter is retained");
Check(classified.Events.SequenceEqual(sources.Events) && sources.Participants.All(p => !p.IsUnidentifiedSource),
    "Classification never drops damage, changes ownership or mutates original records");
Check(EncounterMath.Players(classified, false, 100).Sum(p => p.Total) == 3450
    && EncounterMath.Window(classified, 100) == EncounterMath.Window(sources, 100), "Unattributed damage and boss duration remain in group totals");
Check(Math.Abs(EncounterMath.Timeline(classified, false, 100).Sum() * EncounterMath.Seconds(classified, 100) / 60 - 3450) < .001,
    "Timeline includes unattributed sources exactly once");
Check(EncounterMath.Spells(classified, 3, false).Sum(s => s.Total) == 400, "Unidentified sources retain detailed skills");
Check(EncounterSources.Classify(sources with { Participants = sources.Participants.Select(p => p.Id == 3 ? p with { Name = "Real mage" } : p).ToArray() }).Participants.Single(p => p.Id == 3).IsPlayer,
    "Named player using only effect skills is never reclassified");
Check(EncounterSources.Classify(sources with { Participants = sources.Participants.Select(p => p.Id == 3 ? p with { IsSelf = true } : p).ToArray() }).Participants.Single(p => p.Id == 3).IsPlayer,
    "Local player is never reclassified");
Check(EncounterSources.Classify(sources with { Events = sources.Events.Append(new CombatEvent(5000, 3, 100, 15050140, "Blaze", 10, false, false, false)).ToArray() }).Participants.Single(p => p.Id == 3).IsPlayer,
    "Normal player skills avoid provisional source classification");
Check(EncounterFile.Read(JsonSerializer.Serialize(sources, EncounterFile.Json)).Participants.SequenceEqual(classified.Participants),
    "Old local reports classify sources on read without rewriting files");
var exportedSources = EncounterFile.PrivateExport(classified);
Check(EncounterFile.Read(JsonSerializer.Serialize(exportedSources, EncounterFile.Json)).Participants.SequenceEqual(exportedSources.Participants),
    "Source metadata survives anonymized export and import");
Check(EncounterSources.Classify(sources with { Origin = "import-unverified" }).Participants.All(p => !p.IsUnidentifiedSource),
    "Imports are not classified from placeholder names");
foreach (var invalidBuckets in new[] { 0, -1, 4097 })
{
    try { EncounterMath.Timeline(fight, false, buckets: invalidBuckets); throw new Exception("Invalid timeline size accepted."); }
    catch (ArgumentOutOfRangeException) { Check(true, $"Timeline rejects invalid bucket count {invalidBuckets}"); }
}
var testFolder = Path.Combine(Path.GetTempPath(), "Spike-check-" + Guid.NewGuid().ToString("N"));
var store = new EncounterStore(testFolder);
store.Save(fight); store.Save(fight with { EndReason = "closed" });
Check(store.List().Count == 1 && store.Load(fight.Id).EndReason == "closed", "Checkpoint updates are atomic and do not duplicate fights");
var reopened = new EncounterStore(testFolder);
var recovered = reopened.Load(reopened.List().Single().Id);
Check(recovered.Events.SequenceEqual(fight.Events) && recovered.Participants.SequenceEqual(fight.Participants), "Fresh store restores full event and player details after restart");
Check(EncounterMath.Spells(recovered, 1, false).SequenceEqual(EncounterMath.Spells(fight, 1, false)), "Recovered skill totals, ticks and critical rates match original combat");
var nextFight = fight with { Id = Guid.NewGuid(), StartedAt = fight.StartedAt.AddMinutes(1), Events = [fight.Events[0]] };
reopened.Save(nextFight);
Check(reopened.List()[0].Id == nextFight.Id && reopened.Load(fight.Id).Events.Length == 4, "New combat does not overwrite previous history; latest combat sorts first");
File.Delete(Path.Combine(testFolder, nextFight.Id.ToString("N") + ".json"));
File.WriteAllText(Path.Combine(testFolder, Guid.NewGuid().ToString("N") + ".json"), "broken");
Check(store.List().Count == 1 && store.SkippedFiles == 1, "Corrupted history does not hide valid fights");
for (var i = 0; i < 500; i++) store.Save(fight with { Id = Guid.NewGuid(), StartedAt = fight.StartedAt.AddMinutes(i + 1) });
Check(new EncounterStore(testFolder).List().Count == 501, "History keeps older fights accessible beyond 500 entries");
foreach (var file in Directory.EnumerateFiles(testFolder)) File.Delete(file);
Directory.Delete(testFolder);
await UpdateChecks.Run(Check);
ApplicationDataChecks.Run(Check);
ProgressChecks.Run(Check);
LegacyArchiveChecks.Run(Check);
Console.WriteLine($"{count} checks passed.");
