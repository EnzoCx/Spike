using System.Windows;
using System.Windows.Controls;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private static readonly TimeSpan IdleCollapseDelay = TimeSpan.FromMinutes(2);
    private readonly Button expand;
    private DateTimeOffset? idleSince;
    private bool idleCollapsed;
    private double expandedHeight, expandedTop, collapsedTop;

    private void UpdateIdleLayout(DateTimeOffset now)
    {
        if (captureStatus == "capturing") idleSince = null;
        else idleSince ??= now;

        // Hover only restores opacity. Crossing the overlay must not expand it again.
        var collapse = idleSince is { } since && now - since >= IdleCollapseDelay
            && archived is null && !manipulating && openMenus == 0;
        // Do not unfold an already reduced overlay just to use its menu or move it.
        if (idleCollapsed && archived is null && captureStatus != "capturing") return;
        SetIdleCollapsed(collapse);
    }

    private void SetIdleCollapsed(bool value)
    {
        if (idleCollapsed == value) return;
        if (value)
        {
            expandedHeight = Height;
            expandedTop = Top;
        }
        idleCollapsed = value;
        var layout = (Grid)frame.Child;
        foreach (UIElement child in layout.Children)
            if (Grid.GetRow(child) > 0) child.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
        expand.Visibility = value && !locked ? Visibility.Visible : Visibility.Collapsed;
        // Resize the window itself: the hidden rows must no longer intercept game clicks.
        // Raise the minimum only after restoring height, so bottom anchoring uses
        // the reduced bounds rather than WPF's already-coerced minimum height.
        MinHeight = 44;
        placement.SetHeight(value ? 44 : expandedHeight);
        if (!value) MinHeight = ExpandedMinHeight;
        if (value) collapsedTop = Top;
    }

    private void ExpandIdle()
    {
        idleSince = DateTimeOffset.UtcNow;
        SetIdleCollapsed(false);
        Render();
    }
}
