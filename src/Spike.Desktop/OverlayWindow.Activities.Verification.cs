using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    internal static void VerifyActivities(ActivityController controller, Preferences preferences, string directory, Style buttonStyle)
    {
        var window = new OverlayWindow(preferences with { OverlayWidth = 360, OverlayHeight = 280 }, buttonStyle);
        window.AttachActivities(controller, () => { });
        window.ToggleActivities();
        window.Update(null, "stopped");
        if (!window.activitiesOpen || window.activityChecklist?.Visibility != Visibility.Visible || window.scroll.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("Overlay checklist must replace combat controls.");
        window.UpdateIdleLayout(DateTimeOffset.UtcNow.AddHours(1));
        if (window.idleCollapsed || window.visibilityOpacity != 1) throw new InvalidOperationException("An open checklist must stay readable and expanded.");
        var task = controller.Data.Tasks.Single(t => t.Id == "quests");
        var check = Dashboard.ActivityControls<CheckBox>(window.activityChecklist).Single(c => AutomationProperties.GetName(c) == Text.Get("taskQuests", preferences.Language));
        check.IsChecked = false; check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        if (ActivitySchedule.Count(controller.Data, task, controller.Clock()) != 0) throw new InvalidOperationException("Overlay changes must use the shared checklist.");
        window.SavePreview(directory, $"checklist-overlay-{preferences.Language}-{preferences.Theme}.png");
        Dashboard.ActivityControls<Button>(window.activityChecklist).Single(b => Equals(b.Content, Text.Get("reserves", preferences.Language))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.SavePreview(directory, $"reserves-overlay-{preferences.Language}-{preferences.Theme}.png");
        window.Width = 320;
        window.SavePreview(directory, $"reserves-overlay-{preferences.Language}-{preferences.Theme}-minimum.png");
        if (window.activitySwitch?.Visibility != Visibility.Visible) throw new InvalidOperationException("Both overlay tabs must remain discoverable.");
        var header = (DockPanel)((Grid)window.frame.Child).Children[0];
        var switchBounds = window.activitySwitch!.TransformToAncestor(header).TransformBounds(new Rect(window.activitySwitch.RenderSize));
        var closeBounds = window.close.TransformToAncestor(header).TransformBounds(new Rect(window.close.RenderSize));
        if (switchBounds.Y < 0 || switchBounds.Bottom > header.ActualHeight + 1 || switchBounds.Right > closeBounds.Left)
            throw new InvalidOperationException("Tabs must share the title row without overlapping close at minimum width.");
        window.ToggleActivities(); window.Update(null, "stopped");
        if (window.activitiesOpen || !window.showingUpcoming || window.upcomingPanel.Visibility != Visibility.Visible || window.scroll.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("Outside combat the meter must show upcoming events automatically.");
        window.UpdateIdleLayout(DateTimeOffset.UtcNow.AddHours(1));
        if (window.idleCollapsed) throw new InvalidOperationException("Upcoming events must remain visible during extended idle time.");
        window.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent });
        window.SavePreview(directory, $"upcoming-overlay-{preferences.Language}-{preferences.Theme}.png");
        Dashboard.ActivityControls<Button>(window.activitySwitch!).Single(b => Equals(b.Content, Text.Get("meterTab", preferences.Language))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (window.showingUpcoming || window.scroll.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Choosing Meter must let the player review the last fight while idle.");
        window.Update(null, "capturing");
        if (window.showingUpcoming || window.scroll.Visibility != Visibility.Visible || window.upcomingPanel.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("Combat must immediately restore meter controls.");
        window.ToggleActivities(); window.Update(null, "capturing");
        if (!window.activitiesOpen || window.activityChecklist.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Automatic switching must respect an explicitly opened checklist.");
        window.ToggleActivities(); window.Update(null, "stopped");
        window.ToggleIdleEvents();
        if (window.showingUpcoming || window.scroll.Visibility != Visibility.Visible)
            throw new InvalidOperationException("The automatic events view must be optional.");
        window.UpdateIdleLayout(DateTimeOffset.UtcNow.AddHours(1));
        window.SavePreview(directory, $"collapsed-tabs-{preferences.Language}-{preferences.Theme}.png");
        if (!window.idleCollapsed || window.Height < header.ActualHeight + window.frame.Padding.Top + window.frame.Padding.Bottom + 2)
            throw new InvalidOperationException("Collapsed overlays must retain the complete single-row header and tabs.");
        var restored = System.Text.Json.JsonSerializer.Deserialize<Preferences>(System.Text.Json.JsonSerializer.Serialize(window.preferences))!;
        if (restored.OverlayIdleEvents || !new Preferences().OverlayIdleEvents || !new Preferences().ShowOverlayOnStartup)
            throw new InvalidOperationException("Idle events and startup overlay must default on and preserve user choices.");
        window.Close();
    }
}
