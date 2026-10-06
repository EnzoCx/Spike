using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    internal static IEnumerable<TControl> ActivityControls<TControl>(DependencyObject parent) where TControl : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is TControl control) yield return control;
            foreach (var nested in ActivityControls<TControl>(child)) yield return nested;
        }
    }

    private void VerifyActivities(string directory)
    {
        var original = activities.Data;
        var now = DateTimeOffset.Parse("2026-10-06T11:54:59Z");
        activities.Clock = () => now;
        var task = activities.Data.Tasks.Single(t => t.Id == "quests");
        activities.SetCount(task, 1);
        var reminders = 0;
        void Notification(ActivityOccurrence[] values) => reminders += values.Length;
        activities.Notify += Notification;
        activities.Change(activities.Data with { Settings = activities.Data.Settings with { Notifications = true } });
        activities.Tick(); now = now.AddSeconds(2); activities.Tick(); activities.Tick();
        if (reminders != 2) throw new InvalidOperationException("One notification batch per occurrence is required.");
        activities.Notify -= Notification;
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = new(language, theme, AutoStart: false);
                ApplyTheme(); Translate(); SwitchPage("activities");
                if (ActivitiesPage.Visibility != Visibility.Visible || FightPage.Visibility != Visibility.Collapsed)
                    throw new InvalidOperationException("Activities navigation must hide the combat report.");
                var daily = ActivityControls<Button>(activitiesView).Single(b => Equals(b.Content, T("daily")));
                daily.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var check = ActivityControls<CheckBox>(activitiesView).First(c => AutomationProperties.GetName(c) == T("taskQuests"));
                check.IsChecked = true; check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                if (ActivitySchedule.Count(activities.Data, task, now) != 5) throw new InvalidOperationException("Checklist checkbox did not persist.");
                SaveDashboard(directory, $"activities-{language}-{theme}.png", 1100, 780);
                SaveDashboard(directory, $"activities-{language}-{theme}-minimum.png", 884, 600);
                ActivityControls<Button>(activitiesView).Single(b => Equals(b.Content, T("reserves"))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                activities.SetCount(activities.Data.Tasks.Single(t => t.Id == "shugo"), 10);
                activities.SetCount(activities.Data.Tasks.Single(t => t.Id == "nightmare"), 12);
                activities.SetCount(activities.Data.Tasks.Single(t => t.Id == "odyle"), 480);
                if (ActivityControls<CheckBox>(activitiesView).Any(c => AutomationProperties.GetName(c) == T("taskShugo")))
                    throw new InvalidOperationException("Reserves must never be presented as daily completion checkboxes.");
                SaveDashboard(directory, $"reserves-{language}-{theme}.png", 1100, 780);
                SaveDashboard(directory, $"reserves-{language}-{theme}-minimum.png", 884, 600);
                ActivityControls<Button>(activitiesView).Single(b => Equals(b.Content, T("weekly"))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                SaveDashboard(directory, $"weekly-{language}-{theme}.png", 1100, 780);
                var schedule = ActivityControls<Button>(activitiesView).Single(b => Equals(b.Content, T("schedule")));
                schedule.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var reminderChoices = ActivityControls<CheckBox>(activitiesView).Where(c => AutomationProperties.GetName(c).StartsWith(T("notifyMe") + " · ", StringComparison.Ordinal)).ToArray();
                if (reminderChoices.Length != 7) throw new InvalidOperationException("Every event needs its own reminder control.");
                SaveDashboard(directory, $"schedule-{language}-{theme}.png", 1100, 780);
                SaveDashboard(directory, $"schedule-{language}-{theme}-minimum.png", 884, 600);
                var settings = ActivityControls<Expander>(activitiesView).Single(e => Equals(e.Header, T("reminderSettings")));
                settings.IsExpanded = true;
                SaveDashboard(directory, $"activity-settings-{language}-{theme}.png", 884, 600);
                settings.IsExpanded = false;
                ActivityToast.Verify(preferences, this, directory);
                OverlayWindow.VerifyActivities(activities, preferences, directory, (Style)FindResource(typeof(Button)));
                ActivityControls<Button>(activitiesView).Single(b => Equals(b.Content, T("checklist"))).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        activities.Change(original); activities.Clock = () => DateTimeOffset.UtcNow;
        File.WriteAllText(Path.Combine(directory, "activities-result.txt"), "PASS: activities navigation, checklist persistence, one reminder batch, overlay synchronization and 3 languages × 3 themes.\n");
    }
}
