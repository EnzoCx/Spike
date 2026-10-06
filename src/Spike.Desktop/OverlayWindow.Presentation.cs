using System.Windows;
using System.Windows.Media;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private double ExpandedMinHeight => preferences.OverlayDiscreet ? 112 : 180;

    private void ToggleDiscreet()
    {
        preferences = preferences with { OverlayDiscreet = !preferences.OverlayDiscreet };
        DiscreetChanged?.Invoke(preferences.OverlayDiscreet);
        Apply(preferences);
    }

    private void UpdateSurface()
    {
        if (frame.Background is null) return;
        var reading = activitiesOpen || !locked && (pointerInside || IsKeyboardFocusWithin || manipulating || openMenus > 0);
        var factor = !preferences.OverlayDiscreet || preferences.Theme == "contrast" ? 1
            : preferences.Theme == "light" ? (reading ? 1 : .88) : reading ? .9 : .7;
        frame.Background.Opacity = Bounded(preferences.OverlayOpacity, .94, .65, 1) * factor;
        if (preferences.OverlayDiscreet && preferences.Theme == "dark")
            frame.Background.Opacity = Math.Max(.64, frame.Background.Opacity);
    }

    private void ApplyPresentation()
    {
        var discreet = preferences.OverlayDiscreet;
        if (!idleCollapsed) MinHeight = ExpandedMinHeight;
        scroll.MaxHeight = double.PositiveInfinity;
        frame.BorderBrush.Opacity = discreet && preferences.Theme != "contrast" ? .35 : 1;
        frame.CornerRadius = new CornerRadius(14);
        heading.FontSize = duration.FontSize = discreet ? 14 : 16;
        total.FontSize = discreet ? 13 : 16;
        duration.FontWeight = discreet ? FontWeights.Normal : FontWeights.SemiBold;
        foreach (var button in new[] { picker, historyPicker, damage, healing, scope, back, report, copy, locking, close, options, expand })
        {
            button.BorderThickness = new Thickness(discreet ? 0 : 1);
            button.Background = (Brush)Resources["Surface"];
            button.Foreground = (Brush)Resources["Foreground"];
            if (discreet) button.Background = Brushes.Transparent;
        }
        picker.FontWeight = archived is null ? FontWeights.Normal : FontWeights.SemiBold;
        if (archived is not null)
        {
            picker.Background = (Brush)Resources["Accent"];
            picker.Foreground = (Brush)Resources["Background"];
        }
        var active = heals ? healing : damage;
        active.Background = discreet ? Brushes.Transparent : (Brush)Resources["Accent"];
        active.Foreground = (Brush)Resources[discreet ? "Accent" : "Background"];
        active.FontWeight = FontWeights.SemiBold;
        foreach (var inactive in new[] { damage, healing }.Where(button => button != active)) inactive.FontWeight = FontWeights.Normal;
        report.Content = T(discreet ? "reportShort" : "fightDetails");
        report.ToolTip = T("fightDetails");
        hint.Visibility = discreet ? Visibility.Collapsed : Visibility.Visible;
        var qualified = Selected?.Origin == "demo" || Selected?.Origin.Contains("unverified") == true || archived is not null
            || captureStatus is "captureError" or "paused" or "stopped";
        if (!idleCollapsed)
        {
            healthArea.Visibility = !discreet || qualified ? Visibility.Visible : Visibility.Collapsed;
            columnHead.Visibility = discreet ? Visibility.Collapsed : Visibility.Visible;
        }
        health.Visibility = hpBar.Visibility = discreet ? Visibility.Collapsed : Visibility.Visible;
        heading.ToolTip = heading.Text + "\n" + status.Text + "\n" + health.Text + "\n" + T("dragOverlayHint");
        if (discreet)
        {
            total.ToolTip = hint.Text + "\n" + total.ToolTip;
            if (copyNotice is not null && DateTime.UtcNow < copyNoticeUntil)
                total.Text = T(copyNotice == "copied" ? "copiedShort" : "copyError");
        }
        UpdateSurface();
    }
}
