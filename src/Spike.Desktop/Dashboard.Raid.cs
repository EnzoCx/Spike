using System.Windows;
using System.Windows.Media;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    private bool rdps;
    private RaidResult? raidResult;

    private void ShowRaid(object sender, RoutedEventArgs e)
    {
        rdps = true; heals = false;
        target = shown is null ? null : EncounterMath.PrimaryBoss(shown)?.Id;
        RenderFight();
    }

    private void RenderRaid()
    {
        RaidButton.Content = T("rdps"); RaidButton.ToolTip = T("rdpsModel");
        RaidButton.Background = (Brush)Resources[rdps ? "Accent" : "Surface"];
        RaidButton.Foreground = (Brush)Resources[rdps ? "Background" : "Foreground"];
        RaidNote.Visibility = rdps ? Visibility.Visible : Visibility.Collapsed;
        if (shown is null || raidResult is null) return;
        RaidNote.Text = RaidPresentation.Status(shown, raidResult, preferences.Language) + "\n" + T("rdpsRawDetails");
        RaidNote.ToolTip = T("rdpsModel");
    }
}
