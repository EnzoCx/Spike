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
    private readonly TextBlock eventTimers = new() { FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(7, 0, 4, 0), TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
    public event Action<bool>? IdleEventsChanged;

    internal void AttachActivities(ActivityController controller, Action configure)
    {
        if (activityChecklist is not null) return;
        activityController = controller;
        activityChecklist = new(controller, preferences.Language, true, () => { });
        var layout = (Grid)frame.Child;
        Grid.SetRow(activityChecklist, 1); Grid.SetRowSpan(activityChecklist, 5);
        activityChecklist.Visibility = Visibility.Collapsed; layout.Children.Add(activityChecklist);
        var header = (DockPanel)layout.Children[0];
        var brand = (DockPanel)header.Children[header.Children.Count - 1];
        DockPanel.SetDock(heading, Dock.Left);
        brand.Children.Add(eventTimers);
        eventTimers.SizeChanged += (_, _) => RefreshEventTimers();
        SizeChanged += (_, _) => { RefreshHeaderDensity(); RefreshEventTimers(); };
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
            (T("meterTab"), !activitiesOpen, () => { if (activitiesOpen) ToggleActivities(); }),
            (T("checklistTab"), activitiesOpen, () => { if (!activitiesOpen) ToggleActivities(); }));
        activitySwitch.ToolTip = "Ctrl + Tab"; activitySwitch.Margin = new Thickness(0, 0, 4, 0);
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
        RefreshHeaderDensity(); RefreshEventTimers();
    }

    private void RefreshHeaderDensity()
    {
        if (activitySwitch is null) return;
        var compactHeader = preferences.OverlayIdleEvents && Width < 520;
        activitySwitch.Width = compactHeader ? 52 : 132;
        var buttons = Dashboard.ActivityControls<Button>(activitySwitch).ToArray();
        for (var i = 0; i < buttons.Length; i++)
        {
            var label = T(i == 0 ? "meterTab" : "checklistTab");
            if (compactHeader)
            {
                var icon = new System.Windows.Shapes.Path { Width = 13, Height = 13, Stretch = System.Windows.Media.Stretch.Uniform,
                    StrokeThickness = 1.3, Data = System.Windows.Media.Geometry.Parse(i == 0 ? "M1,12 L1,8 M6,12 L6,4 M11,12 L11,0" : "M0,0 L12,0 12,12 0,12 Z M3,6 L5,8 9,3") };
                icon.SetBinding(System.Windows.Shapes.Shape.StrokeProperty, new System.Windows.Data.Binding("Foreground") { Source = buttons[i] });
                buttons[i].Content = icon;
            }
            else buttons[i].Content = label;
            buttons[i].ToolTip = label + " · Ctrl + Tab";
        }
        eventTimers.Visibility = preferences.OverlayIdleEvents ? Visibility.Visible : Visibility.Collapsed;
    }

    internal void TickActivities() { if (activitiesOpen) activityChecklist?.Tick(); RefreshEventTimers(); }

    private static string HeaderCountdown(TimeSpan remaining)
    {
        var minutes = Math.Max(0, (long)Math.Ceiling(remaining.TotalMinutes));
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    private void RefreshEventTimers()
    {
        if (activityController is null || !preferences.OverlayIdleEvents) return;
        var now = activityController.Clock();
        var data = activityController.Data;
        var events = data.Events.Where(e => e.Notify).Select(e => ActivitySchedule.Next(e, data.Settings, now))
            .OrderBy(e => e.StartsAt).ToArray();
        string Name(ActivityOccurrence value) => value.Activity.CustomName.Length > 0 ? value.Activity.CustomName : T(value.Activity.NameKey);
        string ShortName(ActivityOccurrence value) => value.Activity.CustomName.Length > 0 ? value.Activity.CustomName
            : value.Activity.NameKey switch { "eventShugo" => "Shugo", "eventRift" => "Rift", "eventSiege" => T("siegeShort"), "eventSiegeBosses" => T("bossShort"), "eventNahma" => "Nahma", "eventKaira" => "Kaira", "eventInvasion" => T("invasionShort"), _ => Name(value) };
        var tokens = events.Select(e => { var name = ShortName(e); return (name.Length > 14 ? name[..13] + "…" : name) + " [" + HeaderCountdown(e.StartsAt - now) + "]"; }).ToArray();
        var all = string.Join(" · ", tokens);
        var available = eventTimers.ActualWidth;
        var text = tokens.Length == 0 ? "—" : all;
        if (available > 0)
        {
            double Measure(string value) => new System.Windows.Media.FormattedText(value, Culture, FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), eventTimers.FontSize,
                System.Windows.Media.Brushes.Black, System.Windows.Media.VisualTreeHelper.GetDpi(this).PixelsPerDip).Width;
            for (var count = tokens.Length; count > 0 && Measure(text) > available; count--)
                text = count == 1 ? "+" + tokens.Length : string.Join(" · ", tokens.Take(count - 1)) + " · +" + (tokens.Length - count + 1);
        }
        eventTimers.Text = text;
        var details = events.Length == 0 ? T("noUpcomingEvents") : string.Join("\n", events.Select(e => Name(e) + " · " + e.StartsAt.ToLocalTime().ToString("ddd HH:mm", Culture) + " · [" + HeaderCountdown(e.StartsAt - now) + "]"));
        eventTimers.ToolTip = details + "\n" + T("headerTimersHint");
        System.Windows.Automation.AutomationProperties.SetName(eventTimers, all);
    }

    private void ToggleIdleEvents()
    {
        preferences = preferences with { OverlayIdleEvents = !preferences.OverlayIdleEvents };
        IdleEventsChanged?.Invoke(preferences.OverlayIdleEvents); RefreshHeaderDensity(); RefreshEventTimers(); Render();
    }

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
            if (Grid.GetRow(child) > 0)
                child.Visibility = !idleCollapsed && (ReferenceEquals(child, activityChecklist) == activitiesOpen)
                    ? Visibility.Visible : Visibility.Collapsed;
        if (activitySwitch is not null)
        {
            activitySwitch.Visibility = Visibility.Visible;
            activitySwitch.IsEnabled = !locked;
        }
    }
}
