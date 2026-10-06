using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Spike.Desktop;

internal static class ActivitySegments
{
    public static Border Create(params (string Label, bool Selected, Action Select)[] choices)
    {
        var panel = new UniformGrid { Rows = 1 };
        foreach (var choice in choices)
        {
            var button = new Button { Content = choice.Label, Tag = "segment-" + choice.Label, Padding = new Thickness(5, 6, 5, 6),
                Margin = new Thickness(2), FontSize = 12, MinWidth = 0,
                FontWeight = choice.Selected ? FontWeights.SemiBold : FontWeights.Normal };
            button.SetResourceReference(Control.BackgroundProperty, choice.Selected ? "Accent" : "Surface");
            button.SetResourceReference(Control.ForegroundProperty, choice.Selected ? "Background" : "Muted");
            AutomationProperties.SetName(button, choice.Label);
            AutomationProperties.SetItemStatus(button, choice.Selected ? "selected" : "");
            button.Click += (_, _) => choice.Select(); panel.Children.Add(button);
        }
        var frame = new Border { Child = panel, CornerRadius = new CornerRadius(10), Padding = new Thickness(2), Margin = new Thickness(0, 0, 0, 12) };
        frame.SetResourceReference(Border.BackgroundProperty, "Surface"); return frame;
    }
}
