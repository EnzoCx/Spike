using System.Globalization;
using System.Runtime.InteropServices;
using DPSMeter.Core;

namespace DPSMeter.Desktop;

internal static class FightSummaryVerification
{
    public static void Verify()
    {
        var fight = new Encounter(2, Guid.NewGuid(), DateTimeOffset.UnixEpoch, "global", "", "demo", "", "timeout", 30000,
            [new(1, "Alice\r\nTest", "Gladiator", true), new(2, "Bob", "Cleric", true),
             new(3, "Effect", "Sorcerer", false, IsUnidentifiedSource: true), new(100, "Boss", "", false, true), new(101, "Add", "", false)],
            [new(2000, 1, 100, 1, "Hit", 600, false, false, false), new(4000, 1, 100, 1, "Hit", 400, false, false, false),
             new(3000, 2, 100, 1, "Hit", 100, false, false, false), new(3000, 3, 100, 1, "Effect", 100, false, false, false),
             new(20000, 1, 101, 1, "Hit", 1800, false, false, false), new(25000, 2, 1, 2, "Heal", 900, true, false, false)]);
        foreach (var language in Text.Languages)
        {
            string T(string key) => Text.Get(key, language);
            string N(double number) => number.ToString("N0", CultureInfo.GetCultureInfo(language));
            var boss = FightSummary.Format(fight, false, 100, language);
            Require(boss.Contains(T("demoLabel")) && boss.Contains("00:02"), "Synthetic label or boss window lost.");
            Require(boss.Contains($"{T("damage")} : {N(1200)}") && boss.Contains($"{T("groupDps")} : 600"), "Boss total or DPS diluted by other events.");
            Require(boss.Contains("1. Alice Test : 500 DPS") && boss.Contains("2. Bob : 50 DPS"), "Names, ordering or player DPS changed.");
            Require(boss.Contains($"{T("unidentifiedSource")} : 50 DPS") && !boss.Contains("3. "), "Unidentified damage lost or ranked as a player.");
            var all = FightSummary.Format(fight, false, null, language);
            Require(all.Contains(T("allTargets")) && all.Contains("00:30") && all.Contains($"{T("damage")} : {N(3000)}"), "All-target summary lost events.");
            var healing = FightSummary.Format(fight, true, 100, language);
            Require(healing.Contains("1. Bob : 30 HPS") && healing.Contains(T("allTargets")) && healing.Contains(T("rawHealing")), "Healing filtered by boss or missing its qualifier.");
            Require(FightSummary.Format(fight with { Origin = "import-unverified" }, false, null, language).Contains(T("importLabel")), "Import label missing.");
            var single = FightSummary.Format(fight with { Events = [fight.Events[0]] }, false, 100, language);
            Require(single.Contains("< 1 s") && single.Contains("600 DPS"), "Single hit must use a one-second denominator.");
            var empty = FightSummary.Format(fight with { Events = [], DurationMs = 0 }, false, null, language);
            Require(empty.Contains($"{T("groupDps")} : 0") && !empty.Contains("NaN"), "Empty fight summary is invalid.");
        }
        Require(FightSummary.Copy("test", value => Require(value == "test", "Clipboard text changed.")) == "copied", "Copy success feedback missing.");
        Require(FightSummary.Copy("test", _ => throw new COMException("Clipboard busy")) == "copyError", "Busy clipboard must be recoverable.");
    }

    private static void Require(bool valid, string reason)
    {
        if (!valid) throw new InvalidOperationException(reason);
    }
}
