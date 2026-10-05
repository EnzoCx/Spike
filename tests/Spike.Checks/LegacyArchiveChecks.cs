using System.IO;
using System.Text.Json;
using Spike.Core;

internal static class LegacyArchiveChecks
{
    public static void Run(Action<bool, string> check)
    {
        var fight = new Encounter(2, Guid.NewGuid(), DateTimeOffset.UtcNow, "Global", "synthetic", "demo", "", "test", 20000,
            [new(41, "Synthetic attacker", "Sorcerer", true), new(52, "Synthetic support", "Chanter", true), new(100, "Boss", "", false, true), new(200, "Add", "", false)],
            [new(1000, 41, 100, 1, "Hit", 1105, false, false, false, Raid: new(52, 18190000, 105)),
             new(3000, 41, 100, 1, "Hit", 1105, false, false, false),
             new(4000, 41, 200, 1, "Hit", 1105, false, false, false, Raid: new(52, 18190000, 105))])
        { RdpsModel = RaidCredit.LegacyModel };
        check(EncounterMath.Players(fight, false, 100).Single().Total == 2210, "Legacy credits never alter raw damage or create a support row");
        var exported = EncounterFile.PrivateExport(fight);
        var restored = EncounterFile.Read(JsonSerializer.Serialize(exported, EncounterFile.Json));
        check(restored.Events[0].Raid?.Provider == 2 && restored.RdpsModel == RaidCredit.LegacyModel && restored.Participants.All(p => !p.Name.StartsWith("Synthetic")),
            "rDPS survives v2 export/import with remapped pseudonymous provider IDs");
        foreach (var invalid in new[] {
            fight with { RdpsModel = "unrecognized" }, fight with { RdpsModel = null },
            fight with { Events = [fight.Events[0] with { Heal = true }] },
            fight with { Events = [fight.Events[0] with { Raid = new(999, 18190000, 105) }] },
            fight with { Events = [fight.Events[0] with { Raid = new(52, 1, 105) }] },
            fight with { Events = [fight.Events[0] with { Raid = new(52, 18190000, -1) }] },
            fight with { Events = [fight.Events[0] with { Raid = new(52, 18190000, 1106) }] },
            fight with { Events = [fight.Events[0] with { Raid = new(41, 18190000, 105) }] } })
        {
            var rejected = false;
            try { EncounterFile.Validate(invalid); } catch (InvalidDataException) { rejected = true; }
            check(rejected, "Malformed rDPS metadata is rejected");
        }
    }
}
