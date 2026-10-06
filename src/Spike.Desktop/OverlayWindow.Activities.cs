using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private ActivitiesView? activityChecklist;
    private Border? activitySwitch;
    private bool activitiesOpen;
    private double combatHeight;

    internal void AttachActivities(ActivityController controller, Action configure)
    {
        if (activityChecklist is not null) return;
        activityChecklist = new(controller, preferences.Language, true, () => { });
        var layout = (Grid)frame.Child;
        foreach (UIElement child in layout.Children)
            if (Grid.GetRow(child) > 0) Grid.SetRow(child, Grid.GetRow(child) + 1);
        layout.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(activityChecklist, 2); Grid.SetRowSpan(activityChecklist, 5);
        activityChecklist.Visibility = Visibility.Collapsed; layout.Children.Add(activityChecklist);
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
        if (activitySwitch is not null) layout.Children.Remove(activitySwitch);
        activitySwitch = ActivitySegments.Create(
            (T("meterTab"), !activitiesOpen, () => { if (activitiesOpen) ToggleActivities(); }),
            (T("checklistTab"), activitiesOpen, () => { if (!activitiesOpen) ToggleActivities(); }));
        activitySwitch.ToolTip = "Ctrl + Tab"; activitySwitch.Margin = new Thickness(0, 2, 0, 8);
        Grid.SetRow(activitySwitch, 1); layout.Children.Add(activitySwitch);
        if (activityChecklist.ContextMenu?.Items[0] is MenuItem item) item.Header = T("openActivities");
    }

    internal void TickActivities() { if (activitiesOpen) activityChecklist?.Tick(); }

    private void ToggleActivities()
    {
        if (activityChecklist is null) return;
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
            if (Grid.GetRow(child) > 1)
                child.Visibility = !idleCollapsed && (ReferenceEquals(child, activityChecklist) == activitiesOpen) ? Visibility.Visible : Visibility.Collapsed;
        if (activitySwitch is not null)
        {
            activitySwitch.Visibility = idleCollapsed ? Visibility.Collapsed : Visibility.Visible;
            activitySwitch.IsEnabled = !locked;
        }
    }
}
