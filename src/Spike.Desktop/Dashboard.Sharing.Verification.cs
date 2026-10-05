using System.IO;
using System.Runtime.InteropServices;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifySharing(string directory)
    {
        FightSummaryVerification.Verify();
        shown = null; RenderFight();
        CopySummary(_ => throw new InvalidOperationException("An empty view must not copy."));
        if (CopyButton.IsEnabled) throw new InvalidOperationException("Copy is enabled without a fight.");
        var demo = PreviewEncounter();
        DisplayEncounter(demo, 2, false);
        lastLive = demo with { Id = Guid.NewGuid(), Events = [] }; Tick();
        string? copied = null;
        CopySummary(value => copied = value);
        if (copied != FightSummary.Format(demo, false, target, preferences.Language) || noticeKey != "copied")
            throw new InvalidOperationException("Copy must use the displayed archive and target.");
        CopySummary(_ => throw new COMException("Clipboard busy"));
        if (noticeKey != "copyError") throw new InvalidOperationException("Copy failure not shown.");
        DisplayEncounter(demo, null, true);
        CopySummary(value => copied = value);
        if (copied != FightSummary.Format(demo, true, null, preferences.Language))
            throw new InvalidOperationException("Copy must follow the healing metric.");
        foreach (var language in Text.Languages)
        {
            Change(preferences with { Language = language });
            CopySummary(value => copied = value);
            if (copied != FightSummary.Format(demo, true, null, language) || !copied.Contains(Text.Get("shareHps", language)))
                throw new InvalidOperationException("Report copy must follow interface language changes.");
        }
        shown = lastLive = null; viewingHistory = heals = false; scopeFight = null; selectedActor = target = null;
        noticeKey = null; Translate(); RenderFight();
        File.WriteAllText(Path.Combine(directory, "sharing-result.txt"), "PASS: compact single-line boss/all-target/healing summaries without redundant ranks or shares, localized k/M values, short and empty fights, separate unidentified sources, demo/import labels; report and overlay copy follow interface language changes and preserve selected archives during live updates; clipboard success and busy feedback simulated. System clipboard unchanged.");
        File.WriteAllText(Path.Combine(directory, "sharing-demo.txt"), FightSummary.Format(demo, false, EncounterMath.PrimaryBoss(demo)?.Id, "fr"));
    }
}
