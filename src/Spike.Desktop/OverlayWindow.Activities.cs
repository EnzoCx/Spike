using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private ActivitiesView? activityChecklist;
    private Border? activitySwitch;
    private bool activitiesOpen;
    private double combatHeight;
    private ActivityController? activityController;
    private readonly StackPanel upcomingPanel = new() { Margin = new Thickness(4, 4, 4, 0) };
    private bool showingUpcoming;
    private bool keepMeterWhileIdle;
    private double beforeUpcomingHeight;
    public event Action<bool>? IdleEventsChanged;

    internal void AttachActivities(ActivityController controller, Action configure)
    {
        if (activityChecklist is not null) return;
        activityController = controller;
        activityChecklist = new(controller, preferences.Language, true, () => { });
        var layout = (Grid)frame.Child;
        Grid.SetRow(activityChecklist, 1); Grid.SetRowSpan(activityChecklist, 5);
        activityChecklist.Visibility = Visibility.Collapsed; layout.Children.Add(activityChecklist);
        Grid.SetRow(upcomingPanel, 1); Grid.SetRowSpan(upcomingPanel, 5);
        upcomingPanel.Visibility = Visibility.Collapsed; layout.Children.Add(upcomingPanel);
        RefreshActivitySwitch();
        var settings = new MenuItem { Header = T("openActivities") };
        settings.Click += (_, _) => configure();
        activityChecklist.ContextMenu = new ContextMenu { Items = { settings } };
        PreviewKeyDown += (_, e) =>
        {
            if (!locked && e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            { ToggleActivities(); e.Handled = true; }
        };
        Closed += (_, _) => activityChecklist.Detach();
        ApplyActivityVisibility();
    }

    private void RefreshActivitySwitch()
    {
        if (activityChecklist is null) return;
        var layout = (Grid)frame.Child;
        var header = (DockPanel)layout.Children[0];
        if (activitySwitch is not null) header.Children.Remove(activitySwitch);
        activitySwitch = ActivitySegments.Create(
            (T("meterTab"), !activitiesOpen, () =>
            {
                if (showingUpcoming) { keepMeterWhileIdle = true; Render(); }
                else if (activitiesOpen) ToggleActivities();
            }),
            (T("checklistTab"), activitiesOpen, () => { if (!activitiesOpen) ToggleActivities(); }));
        activitySwitch.ToolTip = T("idleEventsHint") + "\nCtrl + Tab"; activitySwitch.Margin = new Thickness(0, 0, 4, 0);
        activitySwitch.Padding = new Thickness(1); activitySwitch.Width = 132; activitySwitch.Height = 26;
        foreach (var button in Dashboard.ActivityControls<Button>(activitySwitch))
        { button.FontSize = 10.5; button.Padding = new Thickness(3, 2, 3, 2); button.Margin = new Thickness(1); button.BorderThickness = new Thickness(0); }
        DockPanel.SetDock(activitySwitch, Dock.Right); header.Children.Insert(header.Children.Count - 1, activitySwitch);
        heading.FontSize = 12; duration.FontSize = 11; duration.Margin = new Thickness(3, 0, 3, 0);
        foreach (var button in new[] { close, locking, options, expand })
        { button.MinWidth = 18; button.Width = 18; button.Margin = new Thickness(0); button.Padding = new Thickness(1, 2, 1, 2); }
        var brand = (DockPanel)header.Children[header.Children.Count - 1];
        if (brand.Children[0] is BrandMark mark) { mark.Width = mark.Height = 14; mark.Margin = new Thickness(0, 0, 4, 0); }
        if (activityChecklist.ContextMenu?.Items[0] is MenuItem item) item.Header = T("openActivities");
    }

    internal void TickActivities() { if (activitiesOpen) activityChecklist?.Tick(); else if (showingUpcoming) RenderUpcoming(); }

    private bool WantsUpcoming => activityController is not null && preferences.OverlayIdleEvents
        && !activitiesOpen && !keepMeterWhileIdle && archived is null && captureStatus != "capturing";

    private void UpdateUpcomingMode()
    {
        if (captureStatus == "capturing") keepMeterWhileIdle = false;
        var wanted = WantsUpcoming;
        if (wanted == showingUpcoming) return;
        SetIdleCollapsed(false);
        if (wanted) beforeUpcomingHeight = Height;
        showingUpcoming = wanted;
        if (!wanted) { placement.SetHeight(beforeUpcomingHeight); MinHeight = ExpandedMinHeight; }
        ApplyActivityVisibility();
    }

    private void RenderUpcoming()
    {
        if (activityController is null) return;
        var now = activityController.Clock();
        var data = activityController.Data;
        var events = data.Events.Where(e => e.Notify).Select(e => ActivitySchedule.Next(e, data.Settings, now))
            .OrderBy(e => e.StartsAt).ThenBy(e => e.Activity.Id, StringComparer.Ordinal).Take(3).ToArray();
        upcomingPanel.Children.Clear();
        var caption = new TextBlock { Text = T("upNext"), FontSize = 10, Margin = new Thickness(0, 0, 0, 4) };
        caption.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); upcomingPanel.Children.Add(caption);
        foreach (var occurrence in events)
        {
            var remaining = occurrence.StartsAt - now;
            var name = occurrence.Activity.CustomName.Length > 0 ? occurrence.Activity.CustomName : T(occurrence.Activity.NameKey);
            var row = new DockPanel { Height = 26, LastChildFill = true, ToolTip = occurrence.StartsAt.ToLocalTime().ToString("f", Culture) };
            var countdown = new TextBlock { Text = remaining.TotalHours >= 24 ? $"{(int)remaining.TotalDays}d {remaining.Hours:00}h" : $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}", FontSize = 12, FontWeight = FontWeights.Medium, Margin = new Thickness(10, 0, 0, 0) };
            DockPanel.SetDock(countdown, Dock.Right); row.Children.Add(countdown);
            row.Children.Add(new TextBlock { Text = name, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis });
            upcomingPanel.Children.Add(row);
        }
        if (events.Length == 0) upcomingPanel.Children.Add(new TextBlock { Text = T("noUpcomingEvents"), FontSize = 12, Margin = new Thickness(0, 3, 0, 3) });
        heading.Text = Text.ProductName; duration.Text = "";
        MinHeight = 80; placement.SetHeight(76 + Math.Max(1, events.Length) * 26);
        ApplyActivityVisibility();
    }

    private void ToggleIdleEvents()
    {
        preferences = preferences with { OverlayIdleEvents = !preferences.OverlayIdleEvents };
        IdleEventsChanged?.Invoke(preferences.OverlayIdleEvents); Render();
    }

    private void ToggleActivities()
    {
        if (activityChecklist is null) return;
        if (!activitiesOpen && showingUpcoming)
        {
            showingUpcoming = false; placement.SetHeight(beforeUpcomingHeight); MinHeight = ExpandedMinHeight;
        }
        SetIdleCollapsed(false);
        activitiesOpen = !activitiesOpen;
        idleSince = DateTimeOffset.UtcNow;
        if (activitiesOpen) { combatHeight = Height; placement.SetHeight(Math.Max(Height, 560)); }
        else placement.SetHeight(combatHeight);
        RefreshActivitySwitch(); ApplyActivityVisibility(); Render();
    }

    private void ApplyActivityVisibility()
    {
        if (activityChecklist is null) return;
        foreach (UIElement child in ((Grid)frame.Child).Children)
            if (Grid.GetRow(child) > 0)
                child.Visibility = !idleCollapsed && (ReferenceEquals(child, activityChecklist) ? activitiesOpen
                    : ReferenceEquals(child, upcomingPanel) ? showingUpcoming : !activitiesOpen && !showingUpcoming)
                    ? Visibility.Visible : Visibility.Collapsed;
        if (activitySwitch is not null)
        {
            activitySwitch.Visibility = Visibility.Visible;
            activitySwitch.IsEnabled = !locked;
        }
    }
}
