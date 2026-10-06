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
        var task = controller.Data.Tasks.Single(t => t.Id == "shugo");
        var check = Dashboard.ActivityControls<CheckBox>(window.activityChecklist).Single(c => AutomationProperties.GetName(c) == Text.Get("taskShugo", preferences.Language));
        check.IsChecked = false; check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        if (ActivitySchedule.Count(controller.Data, task, controller.Clock()) != 0) throw new InvalidOperationException("Overlay changes must use the shared checklist.");
        window.SavePreview(directory, $"checklist-overlay-{preferences.Language}-{preferences.Theme}.png");
        window.ToggleActivities(); window.Update(null, "stopped");
        if (window.activitiesOpen || window.activityChecklist.Visibility != Visibility.Collapsed || window.scroll.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Returning to combat must restore the meter.");
        window.Close();
    }
}
