using System.Windows;
using System.Windows.Controls;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private ActivitiesView? activityChecklist;
    private Button? activitiesButton;
    private bool activitiesOpen;
    private double combatHeight;

    internal void AttachActivities(ActivityController controller, Action configure)
    {
        if (activityChecklist is not null) return;
        activityChecklist = new(controller, preferences.Language, true, () => { });
        var layout = (Grid)frame.Child;
        Grid.SetRow(activityChecklist, 1); Grid.SetRowSpan(activityChecklist, 5);
        activityChecklist.Visibility = Visibility.Collapsed;
        layout.Children.Add(activityChecklist);
        activitiesButton = SmallButton("✓", ToggleActivities);
        activitiesButton.ToolTip = T("checklist");
        System.Windows.Automation.AutomationProperties.SetName(activitiesButton, T("checklist"));
        DockPanel.SetDock(activitiesButton, Dock.Right);
        ((DockPanel)layout.Children[0]).Children.Insert(0, activitiesButton);
        var menu = new ContextMenu();
        var settings = new MenuItem { Header = T("openActivities") };
        settings.Click += (_, _) => configure(); menu.Items.Add(settings);
        activitiesButton.ContextMenu = menu;
        Closed += (_, _) => activityChecklist.Detach();
    }

    internal void TickActivities() { if (activitiesOpen) activityChecklist?.Tick(); }

    private void ToggleActivities()
    {
        if (activityChecklist is null) return;
        SetIdleCollapsed(false);
        activitiesOpen = !activitiesOpen;
        idleSince = DateTimeOffset.UtcNow;
        if (activitiesOpen) { combatHeight = Height; placement.SetHeight(Math.Max(Height, 380)); }
        else placement.SetHeight(combatHeight);
        ApplyActivityVisibility(); Render();
    }

    private void ApplyActivityVisibility()
    {
        if (activityChecklist is null) return;
        foreach (UIElement child in ((Grid)frame.Child).Children)
            if (Grid.GetRow(child) > 0)
                child.Visibility = !idleCollapsed && (ReferenceEquals(child, activityChecklist) == activitiesOpen) ? Visibility.Visible : Visibility.Collapsed;
        if (activitiesButton is not null)
        {
            activitiesButton.Content = activitiesOpen ? "DPS" : "✓";
            activitiesButton.ToolTip = T(activitiesOpen ? "returnToMeter" : "checklist");
            System.Windows.Automation.AutomationProperties.SetName(activitiesButton, (string)activitiesButton.ToolTip);
            activitiesButton.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
