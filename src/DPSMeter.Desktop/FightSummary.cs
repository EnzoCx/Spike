using System.Globalization;
using System.Runtime.InteropServices;
using DPSMeter.Core;

namespace DPSMeter.Desktop;

internal static class FightSummary
{
    public static string Format(Encounter fight, bool heals, int? target, string language)
    {
        if (heals) target = null;
        string T(string key) => Text.Get(key, language);
        var culture = CultureInfo.GetCultureInfo(language);
        string N(double value) => value.ToString("N0", culture);
        var title = fight.Title == "—" ? T("local") : SingleLine(fight.Title);
        var scope = target is { } id ? SingleLine(fight.Participants.First(p => p.Id == id).Name) : T("allTargets");
        var rows = EncounterMath.Players(fight, heals, target);
        var total = rows.Sum(row => row.Total);
        var metric = T(heals ? "hps" : "dps");
        var duration = EncounterMath.Window(fight, target).DurationMs;
        var lines = new List<string> { $"DPSMeter · {title}" };
        if (fight.Origin == "demo") lines.Add(T("demoLabel"));
        else if (fight.Origin.Contains("unverified")) lines.Add(T("importLabel"));
        lines.Add($"{scope} · {T("duration")} : {(duration < 1000 ? "< 1 s" : CombatPresentation.Duration(duration))}");
        lines.Add($"{T(heals ? "heals" : "damage")} : {N(total)} · {T(heals ? "groupHps" : "groupDps")} : {N(total / EncounterMath.Seconds(fight, target))}");
        var rank = 0;
        foreach (var row in rows)
        {
            var person = fight.Participants.First(p => p.Id == row.Id);
            var label = person.IsUnidentifiedSource ? T("unidentifiedSource") : $"{++rank}. {SingleLine(row.Name)}";
            lines.Add($"{label} : {N(row.PerSecond)} {metric} ({row.Share.ToString("N1", culture)} %)");
        }
        if (heals) lines.Add(T("rawHealing"));
        return string.Join(Environment.NewLine, lines);
    }

    public static string Copy(string summary, Action<string> write)
    {
        try { write(summary); return "copied"; }
        catch (ExternalException) { return "copyError"; }
    }

    private static string SingleLine(string value) => string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
