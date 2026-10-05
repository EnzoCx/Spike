using System.Runtime.InteropServices;
using Spike.Core;

namespace Spike.Desktop;

internal static class FightSummaryVerification
{
    public static void Verify()
    {
        var fight = new Encounter(2, Guid.NewGuid(), DateTimeOffset.UnixEpoch, "global", "", "demo", "", "timeout", 30000,
            [new(1, "Alice\r\nTest", "Gladiator", true), new(2, "Bob", "Cleric", true),
             new(3, "Effect", "Sorcerer", false, IsUnidentifiedSource: true), new(100, "Boss\r\nTest", "", false, true), new(101, "Add", "", false)],
            [new(2000, 1, 100, 1, "Hit", 600, false, false, false), new(4000, 1, 100, 1, "Hit", 400, false, false, false),
             new(3000, 2, 100, 1, "Hit", 100, false, false, false), new(3000, 3, 100, 1, "Effect", 100, false, false, false),
             new(20000, 1, 101, 1, "Hit", 1800, false, false, false), new(25000, 2, 1, 2, "Heal", 900, true, false, false)]);
        foreach (var language in Text.Languages)
        {
            string T(string key) => Text.Get(key, language);
            var boss = FightSummary.Format(fight, false, 100, language);
            Require(boss.Contains(T("demoLabel")) && boss.Contains("00:02"), "Synthetic label or boss window lost.");
            Require(boss.Contains($"{T("shareDps")} 600"), "Boss DPS diluted by other events or missing unidentified damage.");
            Require(boss.Contains("Alice Test 500 | Bob 50"), "Names, ordering or player DPS changed.");
            Require(boss.Contains($"{T("unidentifiedSource")} 50") && !boss.Contains("| 3."), "Unidentified damage lost or ranked as a player.");
            Require(!boss.Any(char.IsControl) && boss.Contains("Boss Test 00:02"), "Summary must be one line, including names.");
            Require(boss.Split("Boss Test").Length == 2 && boss.Split("DPS").Length == 2, "Target or metric repeated unnecessarily.");
            Require(!boss.Contains('%') && !boss.Contains("1."), "Chat summary must omit shares and redundant ranks.");
            var all = FightSummary.Format(fight, false, null, language);
            Require(all.Contains(T("allTargets")) && all.Contains("00:30") && all.Contains($"{T("shareDps")} 100"), "All-target summary lost events.");
            var healing = FightSummary.Format(fight, true, 100, language);
            Require(healing.Contains("Bob 30") && healing.Contains(T("shareHps")) && healing.Contains(T("allTargets")) && healing.Contains(T("shareRawHealing")), "Healing filtered by boss or missing its qualifier.");
            Require(FightSummary.Format(fight with { Origin = "import-unverified" }, false, null, language).Contains(T("importLabel")), "Import label missing.");
            var single = FightSummary.Format(fight with { Events = [fight.Events[0]] }, false, 100, language);
            Require(single.Contains("<1s") && single.Contains($"{T("shareDps")} 600"), "Single hit must use a one-second denominator.");
            var empty = FightSummary.Format(fight with { Events = [], DurationMs = 0 }, false, null, language);
            Require(empty.Contains($"{T("shareDps")} 0") && !empty.Contains("NaN"), "Empty fight summary is invalid.");
            var thousands = FightSummary.Format(fight with { Events = [fight.Events[0] with { Amount = 12893 }] }, false, 100, language);
            var millions = FightSummary.Format(fight with { Events = [fight.Events[0] with { Amount = 1289345 }] }, false, 100, language);
            Require(thousands.Contains(language == "en" ? "12.9k" : "12,9k") && millions.Contains(language == "en" ? "1.29M" : "1,29M"), "Compact numbers must use the selected language.");
        }
        Require(FightSummary.Copy("test", value => Require(value == "test", "Clipboard text changed.")) == "copied", "Copy success feedback missing.");
        Require(FightSummary.Copy("test", _ => throw new COMException("Clipboard busy")) == "copyError", "Busy clipboard must be recoverable.");
    }

    private static void Require(bool valid, string reason)
    {
        if (!valid) throw new InvalidOperationException(reason);
    }
}
