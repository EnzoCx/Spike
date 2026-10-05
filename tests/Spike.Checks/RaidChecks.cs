using System.IO;
using System.Text.Json;
using Spike.Core;

internal static class RaidChecks
{
    public static void Run(Action<bool, string> check)
    {
        var fight = new Encounter(2, Guid.NewGuid(), DateTimeOffset.UtcNow, "Global", "synthetic", "demo", "", "test", 20000,
            [new(41, "Synthetic attacker", "Sorcerer", true), new(52, "Synthetic support", "Chanter", true), new(100, "Boss", "", false, true), new(200, "Add", "", false)],
            [new(1000, 41, 100, 1, "Hit", 1105, false, false, false, Raid: new(52, 18190000, 105)),
             new(3000, 41, 100, 1, "Hit", 1105, false, false, false),
             new(4000, 41, 200, 1, "Hit", 1105, false, false, false, Raid: new(52, 18190000, 105))])
        { RdpsModel = RaidDamage.Model };
        var result = RaidDamage.Calculate(fight, 100);
        check(result.Rows.Sum(r => r.Meter.Total) == 2210 && result.TotalDamage == 2210, "rDPS conserves original damage");
        check(result.Rows.Single(r => r.Meter.Id == 41).Meter.PerSecond == 1052.5 && result.Rows.Single(r => r.Meter.Id == 52).Meter.PerSecond == 52.5,
            "rDPS transfers bonus to zero-damage support on the boss window, excluding inactivity and adds");
        check(result.ObservedPercent == 50 && result.Rows.All(r => r.HasEvidence), "Partial evidence coverage is reported independently of the estimate");
        check(RaidDamage.Calculate(fight).Rows.Single(r => r.Meter.Id == 52).Provided == 210, "All-target rDPS includes each observed bonus once");
        check(!RaidDamage.Calculate(fight with { RdpsModel = null }).Available && !RaidDamage.Calculate(fight with { Events = [] }).Available,
            "Legacy and empty fights do not substitute raw DPS for unavailable rDPS");
        var self = fight with { Events = [fight.Events[0] with { Raid = new(41, 17410000, 0) }] };
        check(RaidDamage.Calculate(self).Rows.Single().Meter.Total == 1105, "Self aura does not transfer damage");
        var exported = EncounterFile.PrivateExport(fight);
        var restored = EncounterFile.Read(JsonSerializer.Serialize(exported, EncounterFile.Json));
        check(restored.Events[0].Raid?.Provider == 2 && restored.RdpsModel == RaidDamage.Model && restored.Participants.All(p => !p.Name.StartsWith("Synthetic")),
            "rDPS survives v2 export/import with remapped pseudonymous provider IDs");
        check(RaidDamage.Calculate(restored, 3).Rows.Sum(r => r.Meter.Total) == result.TotalDamage, "Export preserves rDPS totals");
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
