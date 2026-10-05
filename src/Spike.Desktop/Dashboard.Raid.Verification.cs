using System.IO;
using System.Windows;
using System.Windows.Controls;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifyRaid(string directory)
    {
        var demo = PreviewEncounter();
        var estimate = demo with
        {
            RdpsModel = RaidDamage.Model,
            Events = demo.Events.Select(hit => hit.Heal ? hit : hit with
            { Raid = new RaidCredit(hit.Source == 1 ? 1 : 2, 18190000, hit.Source is 1 or 2 ? 0 : hit.Amount / 10) }).ToArray()
        };
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = new(language, theme, false, AutoStart: false);
                ApplyTheme(); Translate(); DisplayEncounter(estimate, 2, false);
                RaidButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (!rdps || heals || RaidNote.Visibility != Visibility.Visible || !RaidNote.Text.Contains(T("rdpsPartial"))
                    || !PlayerList.Items.Cast<PlayerDisplay>().Any(row => row.RateLabel.StartsWith("≈")) || MetricNote.Text != T("rdpsModel"))
                    throw new InvalidOperationException("Beta report must display estimated values and model limitations.");
                string? copied = null; CopySummary(value => copied = value);
                if (copied is null || !copied.Contains(T("rdps")) || !copied.Contains(T("rdpsPartial")) || !copied.Contains(T("demoLabel")))
                    throw new InvalidOperationException("Copied estimates must retain beta, partial and demo labels.");
                lastLive = demo with { Id = Guid.NewGuid(), Events = [] }; Tick();
                if (shown?.Id != estimate.Id || !rdps) throw new InvalidOperationException("Live refresh changed archived rDPS view.");
                SaveDashboard(directory, $"{language}-{theme}-rdps.png", 1100, 780);
                SaveDashboard(directory, $"{language}-{theme}-rdps-minimum.png", 884, 600);
                DisplayEncounter(demo, null, false);
                if (!RaidNote.Text.Contains(T("rdpsLegacy")) || PlayerList.Items.Cast<PlayerDisplay>().Any(row => row.RateLabel != "—"))
                    throw new InvalidOperationException("Old archives must remain unavailable in beta mode.");
                HealingButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (rdps || !heals) throw new InvalidOperationException("Healing did not leave rDPS mode.");
                DamageButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (rdps || heals) throw new InvalidOperationException("Raw damage did not leave rDPS mode.");
                OverlayWindow.VerifyRaid(preferences, estimate, demo, directory, (Style)FindResource(typeof(Button)));
            }
        shown = lastLive = null; viewingHistory = heals = rdps = false; scopeFight = null; selectedActor = target = null;
        File.WriteAllText(Path.Combine(directory, "rdps-result.txt"), "PASS: beta/partial/unavailable labels, immutable archives, raw/healing switching, localized copy and estimates, stable overlay rows; FR/EN/ES and three themes, both densities at minimum width. Synthetic demo only. No capture or visible window.");
    }
}
