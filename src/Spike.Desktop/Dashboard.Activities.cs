using System.Windows;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    private ActivityController activities = null!;
    private ActivitiesView activitiesView = null!;
    private ActivityToast? activityToast;

    private void InitializeActivities()
    {
        activities = new(verifying, SetNotice);
        activitiesView = new(activities, preferences.Language, false, TestActivityNotification);
        ActivitiesPage.Content = activitiesView;
        activities.Notify += occurrences => ShowActivityNotification(occurrences);
    }

    private void ShowActivities(object sender, RoutedEventArgs e)
    {
        SwitchPage("activities");
        if (!verifying) { WindowState = WindowState.Normal; Show(); Activate(); }
    }

    private void TickActivities()
    {
        activities.Tick();
        if (page == "activities") activitiesView.Tick();
        overlay?.TickActivities();
    }

    private void TestActivityNotification() => ShowActivityNotification([]);

    private void ShowActivityNotification(ActivityOccurrence[] occurrences)
    {
        if (verifying) return;
        var now = activities.Clock();
        if (activityToast is null)
        {
            activityToast = new(preferences, () => ShowActivities(this, new RoutedEventArgs()), overlay is { IsVisible: true } ? overlay : this);
            activityToast.Closed += (_, _) => activityToast = null;
        }
        if (occurrences.Length == 0)
        {
            // Preview a simultaneous batch, including a long event name.
            foreach (var key in new[] { "eventShugo", "eventRift", "eventSiegeBosses" })
                activityToast.Enqueue(T(key), T("testNotificationBody"));
        }
        else foreach (var occurrence in occurrences)
        {
            var name = occurrence.Activity.CustomName.Length > 0 ? occurrence.Activity.CustomName : T(occurrence.Activity.NameKey);
            activityToast.Enqueue(name, occurrence.StartsAt.ToLocalTime().ToString("HH:mm", Culture)
                + "  ·  " + string.Format(Culture, T("startsIn"), Math.Max(0, (int)Math.Ceiling((occurrence.StartsAt - now).TotalMinutes))));
        }
        if (!activityToast.IsVisible) activityToast.Show();
        if (activities.Data.Settings.Sound) ReminderSound.Play();
    }
}
