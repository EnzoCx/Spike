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
        window.ToggleActivities(); window.Update(null, "stopped");
        if (window.activitiesOpen || window.activityChecklist.Visibility != Visibility.Collapsed || window.scroll.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Returning to combat must restore the meter.");
        window.Close();
    }
}
