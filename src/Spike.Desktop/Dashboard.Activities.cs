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
        var message = occurrences.Length == 0 ? T("testNotificationBody") : string.Join("\n", occurrences.Take(3).Select(o =>
        {
            var name = o.Activity.CustomName.Length > 0 ? o.Activity.CustomName : T(o.Activity.NameKey);
            return name + " · " + o.StartsAt.ToLocalTime().ToString("HH:mm", Culture)
                + " · " + string.Format(Culture, T("startsIn"), Math.Max(0, (int)Math.Ceiling((o.StartsAt - now).TotalMinutes)));
        }));
        if (occurrences.Length > 3) message += "\n" + string.Format(Culture, T("moreEvents"), occurrences.Length - 3);
        activityToast?.Close();
        activityToast = new(preferences, message, () => ShowActivities(this, new RoutedEventArgs()));
        activityToast.Show();
        if (activities.Data.Settings.Sound) System.Media.SystemSounds.Asterisk.Play();
    }
}
