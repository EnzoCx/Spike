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
        if (window.activitiesOpen || window.scroll.Visibility != Visibility.Visible || window.eventTimers.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Header timers must preserve the meter outside combat.");
        window.Width = 520; window.Update(null, "stopped");
        window.SavePreview(directory, $"header-timers-{preferences.Language}-{preferences.Theme}.png");
        if (!window.eventTimers.Text.Contains("Shugo [00:05]") || !window.eventTimers.Text.Contains("Rift [00:05]"))
            throw new InvalidOperationException("A normal header must show both compact Shugo and Rift countdowns in hours:minutes.");
        var beforeHeight = window.Height;
        var beforeRows = window.rows.Children.Count;
        window.ToggleIdleEvents();
        if (window.eventTimers.Visibility != Visibility.Collapsed || window.Height != beforeHeight || window.rows.Children.Count != beforeRows || window.scroll.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Hiding event timers must not change the damage view or overlay height.");
        window.ToggleIdleEvents(); window.Update(null, "capturing");
        if (window.eventTimers.Visibility != Visibility.Visible || window.scroll.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Timers and combat must coexist.");
        window.ToggleActivities();
        if (!window.activitiesOpen || window.activityChecklist.Visibility != Visibility.Visible || window.eventTimers.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Timers must coexist with the checklist too.");
        window.ToggleActivities(); window.Update(null, "stopped");
        foreach (var width in new[] { 320, 460, 640 })
        {
            window.Width = width; window.Update(null, "stopped");
            window.UpdateIdleLayout(DateTimeOffset.UtcNow.AddHours(1));
            window.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent });
            window.SavePreview(directory, $"header-timers-{preferences.Language}-{preferences.Theme}-{width}.png");
            var timerBounds = window.eventTimers.TransformToAncestor(header).TransformBounds(new Rect(window.eventTimers.RenderSize));
            var tabBounds = window.activitySwitch!.TransformToAncestor(header).TransformBounds(new Rect(window.activitySwitch.RenderSize));
            if (!window.idleCollapsed || window.eventTimers.ActualWidth < 15 || timerBounds.Right > tabBounds.Left + 1
                || window.Height < header.ActualHeight + window.frame.Padding.Top + window.frame.Padding.Bottom + 2)
                throw new InvalidOperationException($"Collapsed header timers must fit: width={width}, collapsed={window.idleCollapsed}, timer={timerBounds}, tabs={tabBounds}, height={window.Height}, header={header.ActualHeight}.");
        }
        window.Width = 460; window.Update(null, "stopped");
        window.SavePreview(directory, $"upcoming-overlay-{preferences.Language}-{preferences.Theme}.png");
        var original = controller.Data;
        controller.Change(original with { Events = original.Events.Select(e => e with { Notify = false }).ToArray() });
        window.TickActivities();
        if (window.eventTimers.Text != "—") throw new InvalidOperationException("An empty schedule must remain explicit.");
        controller.Change(original); window.TickActivities();
        if (HeaderCountdown(TimeSpan.FromSeconds(1)) != "00:01" || HeaderCountdown(TimeSpan.FromHours(27)) != "27:00"
            || HeaderCountdown(TimeSpan.Zero) != "00:00") throw new InvalidOperationException("Compact countdowns must round up and retain hours beyond midnight.");
        window.ToggleIdleEvents();
        var restored = System.Text.Json.JsonSerializer.Deserialize<Preferences>(System.Text.Json.JsonSerializer.Serialize(window.preferences))!;
        if (restored.OverlayIdleEvents || !new Preferences().OverlayIdleEvents || !new Preferences().ShowOverlayOnStartup)
            throw new InvalidOperationException("Header timers and startup overlay must default on and preserve user choices.");
        window.Close();
    }
}
