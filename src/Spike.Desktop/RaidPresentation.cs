using System.Globalization;
using Spike.Core;

namespace Spike.Desktop;

internal static class RaidPresentation
{
    public static string Status(Encounter fight, RaidResult result, string language)
    {
        string T(string key) => Text.Get(key, language);
        if (fight.RdpsModel != RaidDamage.Model) return T("rdpsLegacy");
        if (!result.Available) return T("rdpsUnavailable");
        return $"{T("rdpsPartial")} · {T("rdpsObserved")} : {result.ObservedPercent.ToString("N1", CultureInfo.GetCultureInfo(language))} %";
    }

    public static string Details(Encounter fight, int actor, int? target, string language)
    {
        string T(string key) => Text.Get(key, language);
        string N(double number) => number.ToString("N0", CultureInfo.GetCultureInfo(language));
        var result = RaidDamage.Calculate(fight, target);
        var row = result.Rows.FirstOrDefault(r => r.Meter.Id == actor);
        var rate = row?.HasEvidence == true ? "≈ " + N(row.Meter.PerSecond) : "—";
        var seconds = EncounterMath.Seconds(fight, target);
        return $"{T("rdps")} : {rate}\n{Status(fight, result, language)}"
            + (row?.HasEvidence == true ? $"\n− {T("rdpsReceived")} : {N(row.Received / seconds)} DPS · + {T("rdpsProvided")} : {N(row.Provided / seconds)} DPS" : "");
    }
}
