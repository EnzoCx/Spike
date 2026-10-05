using System.IO;
using System.Text.Json;
using Spike.Core;

internal static class ProgressChecks
{
    public static void Run(Action<bool, string> check)
    {
        var fight = new Encounter(2, Guid.NewGuid(), DateTimeOffset.Parse("2026-01-01T12:00:00Z"), "Global", "test", "live", "Cave", "boss-defeated", 20000,
            [new(1, "Synthetic player", "Sorcerer", true, IsSelf: true, ServerId: 1, ObservedDeaths: 2, IdentityEvidence: "direct"),
             new(100, "Synthetic boss", "", false, true, NpcId: 10), new(400, "#400", "", false)],
            [new(2000, 1, 100, 15100000, "Fire", 1000, false, false, false, 400, "owner-id"),
             new(12000, 1, 100, 15100000, "Fire", 2000, false, false, false), new(19000, 1, 1, 1, "Heal", 200, true, false, false)]);
        var first = EncounterProgress.Summarize(fight).Single();
        check(first.Dps == 300 && first.DurationMs == 10000 && first.Hps == 10 && first.Deaths == 2,
            "Progress shares the boss damage clock and keeps whole-fight healing and observed deaths");
        var next = fight with { Id = Guid.NewGuid(), StartedAt = fight.StartedAt.AddMinutes(10),
            Participants = fight.Participants.Select(p => p with { Id = p.Id + 1000 }).ToArray(),
            Events = fight.Events.Select(e => e with { Source = e.Source + 1000, Target = e.Target + 1000,
                OriginalSource = e.OriginalSource is { } raw ? raw + 1000 : null, Amount = e.Amount * 2 }).ToArray() };
        var second = EncounterProgress.Summarize(next).Single();
        check(EncounterProgress.Series([second, first], first.Context, first.PlayerKey).Select(a => a.Id).SequenceEqual(new[] { fight.Id, next.Id }),
            "Progress matches characters and bosses across changed runtime entity IDs and orders attempts chronologically");
        check(EncounterProgress.Change(second.Dps, first.Dps) == 100 && EncounterProgress.Change(10, 0) is null,
            "Progress delta is correct and has no invented percentage from a zero baseline");
        foreach (var other in new[] { fight with { Origin = "demo" }, fight with { Origin = "import-unverified" },
            fight with { Patch = "other" }, fight with { Zone = "Other cave" },
            fight with { Participants = fight.Participants.Select(p => p.IsBoss ? p with { NpcId = 11 } : p).ToArray() } })
            check(EncounterProgress.Summarize(other).Single().Context != first.Context, "Progress separates incompatible provenance, patches, zones and boss variants");
        var otherServer = fight with { Participants = fight.Participants.Select(p => p.IsSelf ? p with { ServerId = 2 } : p).ToArray() };
        check(EncounterProgress.Summarize(otherServer).Single().PlayerKey != first.PlayerKey, "Same character name on different known servers stays separate");
        var ambiguous = fight with { Participants = [.. fight.Participants, fight.Participants[0] with { Id = 2 }] };
        check(EncounterProgress.Summarize(ambiguous).Count == 0, "Ambiguous names within a fight never merge progression rows");
        var unknown = fight with { Participants = fight.Participants.Select(p => p.IsPlayer ? p with { IsPlayer = false, IsUnidentifiedSource = true } : p).ToArray() };
        check(EncounterProgress.Summarize(unknown).Count == 0, "Unidentified sources never become a character progression series");
        var zone = fight with { Participants = fight.Participants.Select(p => p with { IsBoss = false, NpcId = null }).ToArray() };
        check(EncounterProgress.Summarize(zone).Single().Title == "Cave" && EncounterProgress.Summarize(zone with { Zone = "" }).Count == 0,
            "Non-boss progression groups known zones only");
        var restored = EncounterFile.Read(JsonSerializer.Serialize(fight, EncounterFile.Json));
        check(restored.Participants.SequenceEqual(fight.Participants) && restored.Events.SequenceEqual(fight.Events), "Evidence and death counts survive v2 round trip");
        var exported = EncounterFile.PrivateExport(fight);
        check(EncounterProgress.Summarize(exported).Count == 0, "Anonymized Player numbers cannot identify the same person across imported fights");
        check(exported.Events[0].OriginalSource == 3 && exported.Participants.All(p => !p.IsSelf) && exported.Participants[0].Name != "Synthetic player",
            "Private export remaps original source evidence and removes personal markers");
        foreach (var invalid in new[] {
            fight with { Events = [fight.Events[0] with { OriginalSource = 999 }] },
            fight with { Events = [fight.Events[0] with { Attribution = "guessed" }] },
            fight with { Participants = fight.Participants.Select(p => p with { ObservedDeaths = -1 }).ToArray() } })
        {
            var rejected = false;
            try { EncounterFile.Validate(invalid); } catch (InvalidDataException) { rejected = true; }
            check(rejected, "Invalid evidence references and death counts are rejected");
        }
    }
}
