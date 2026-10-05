using System.Runtime.InteropServices;
using Spike.Core;

namespace Spike.Desktop;

internal static class FightSummary
{
    public static string Format(Encounter fight, bool heals, int? target, string language, bool rdps = false)
    {
        if (heals) target = null;
        if (heals) rdps = false;
        string T(string key) => Text.Get(key, language);
        string N(double value) => CombatPresentation.Compact(value, language);
        var scope = target is { } id ? SingleLine(fight.Participants.First(p => p.Id == id).Name)
            : fight.Title == "—" ? T("allTargets") : $"{SingleLine(fight.Title)} / {T("allTargets")}";
        var raid = rdps ? RaidDamage.Calculate(fight, target) : null;
        var rows = raid is not null ? raid.Rows.Select(r => r.Meter).ToArray() : EncounterMath.Players(fight, heals, target);
        var total = rows.Sum(row => row.Total);
        var duration = EncounterMath.Window(fight, target).DurationMs;
        var parts = new List<string>();
        if (fight.Origin == "demo") parts.Add(T("demoLabel"));
        else if (fight.Origin.Contains("unverified")) parts.Add(T("importLabel"));
        parts.Add($"{scope} {(duration < 1000 ? "<1s" : CombatPresentation.Duration(duration))}");
        parts.Add($"{T(heals ? "shareHps" : "shareDps")} {N(total / EncounterMath.Seconds(fight, target))}");
        if (rdps) parts.Add(T("rdps") + " · " + (raid!.Available ? T("rdpsPartial") : "—"));
        foreach (var row in rows)
        {
            var person = fight.Participants.First(p => p.Id == row.Id);
            var label = person.IsUnidentifiedSource ? T("unidentifiedSource") : SingleLine(row.Name);
            var unknown = rdps && raid!.Rows.First(r => r.Meter.Id == row.Id).HasEvidence == false;
            parts.Add($"{label} {(unknown ? "—" : (rdps ? "≈" : "") + N(row.PerSecond))}");
        }
        if (heals) parts.Add(T("shareRawHealing"));
        return string.Join(" | ", parts);
    }

    public static string Copy(string summary, Action<string> write)
    {
        try { write(summary); return "copied"; }
        catch (ExternalException) { return "copyError"; }
    }

    private static string SingleLine(string value) => string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
