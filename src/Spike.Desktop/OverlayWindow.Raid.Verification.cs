using System.Windows;
using System.Windows.Controls;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    internal static void VerifyRaid(Preferences preferences, Encounter estimate, Encounter legacy, string directory, Style buttonStyle)
    {
        foreach (var discreet in new[] { true, false })
        {
            var window = new OverlayWindow(preferences with { OverlayWidth = 320, OverlayDiscreet = discreet, OverlayAutoFit = true }, buttonStyle);
            window.Update(estimate, "capturing"); window.raid.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (!window.RaidMode || window.heals || !window.hint.Text.Contains(window.T("rdpsPartial")) || window.hint.Visibility != Visibility.Visible)
                throw new InvalidOperationException("Overlay must expose beta limitations even in discreet mode.");
            var row = window.entries.First().Value; window.Update(estimate);
            if (!window.entries.Values.Contains(row)) throw new InvalidOperationException("rDPS refresh replaced hover rows.");
            window.SelectFight(estimate); window.Update(legacy);
            if (window.Selected != estimate || !window.RaidMode) throw new InvalidOperationException("Overlay lost archived beta view.");
            string? copied = null; window.CopySummary(value => copied = value);
            if (copied is null || !copied.Contains(window.T("rdps"))) throw new InvalidOperationException("Overlay copy lost beta marker.");
            window.SavePreview(directory, $"{preferences.Language}-{preferences.Theme}-rdps-overlay-{discreet}.png");
            if (window.scroll.ScrollableHeight > 1) throw new InvalidOperationException("rDPS auto height clips party rows.");
            window.SelectFight(legacy);
            if (!window.hint.Text.Contains(window.T("rdpsLegacy"))) throw new InvalidOperationException("Legacy overlay must show unavailable.");
            window.healing.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (window.RaidMode || !window.heals) throw new InvalidOperationException("Overlay healing retained beta mode.");
            window.Close();
        }
    }
}
