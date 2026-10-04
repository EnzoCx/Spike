using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DPSMeter.Desktop;

/// <summary>Updated in place so hovering and keyboard focus survive live refreshes.</summary>
internal sealed class CombatantRow : Button
{
    private readonly TextBlock name = new(), rate = new(), share = new(), detail = new(), total = new();
    private readonly ContentControl emblem = new();
    private readonly ColumnDefinition fill = new(), rest = new();
    private readonly Border bar = new();
    private readonly Grid layout = new();
    private readonly ToolTip tooltip = new()
    {
        Padding = new Thickness(0),
        BorderThickness = new Thickness(0),
        HasDropShadow = true,
        Placement = System.Windows.Controls.Primitives.PlacementMode.Right,
        HorizontalOffset = 12
    };
    public Action? Selected { get; set; }
    public Func<FrameworkElement>? HoverContent { get; set; }

    public CombatantRow()
    {
        Padding = new Thickness(0); Margin = new Thickness(0, 0, 0, 2); HorizontalContentAlignment = HorizontalAlignment.Stretch;
        var track = new Grid { IsHitTestVisible = false };
        track.ColumnDefinitions.Add(fill); track.ColumnDefinitions.Add(rest); track.Children.Add(bar);
        var surface = new Grid(); surface.Children.Add(track);
        layout.Margin = new Thickness(8, 6, 8, 6);
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRowSpan(emblem, 2); layout.Children.Add(emblem);
        name.FontSize = 13; name.FontWeight = FontWeights.SemiBold; name.TextTrimming = TextTrimming.CharacterEllipsis;
        name.Margin = new Thickness(4, 0, 5, 0); Grid.SetColumn(name, 1); layout.Children.Add(name);
        detail.FontSize = 10; detail.TextTrimming = TextTrimming.CharacterEllipsis; detail.Margin = new Thickness(4, 2, 4, 0);
        Grid.SetColumn(detail, 1); Grid.SetRow(detail, 1); layout.Children.Add(detail);
        rate.FontSize = 19; rate.FontWeight = FontWeights.SemiBold; rate.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(rate, 2); layout.Children.Add(rate);
        share.FontSize = 11; share.HorizontalAlignment = HorizontalAlignment.Right; share.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(share, 3); layout.Children.Add(share);
        total.FontSize = 10; total.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(total, 2); Grid.SetColumnSpan(total, 2); Grid.SetRow(total, 1); layout.Children.Add(total);
        surface.Children.Add(layout); Content = surface;
        ToolTip = tooltip;
        ToolTipService.SetInitialShowDelay(this, 200); ToolTipService.SetBetweenShowDelay(this, 0); ToolTipService.SetShowDuration(this, 60000);
        ToolTipOpening += (_, _) => tooltip.Content = HoverContent?.Invoke();
        Click += (_, _) => Selected?.Invoke();
    }

    public void Update(string title, string subtitle, string number, string percentage, string amount, double fraction,
        string className, Preferences preferences, bool self = false, int? skillId = null)
    {
        var palette = Themes.Get(preferences.Theme);
        var discreet = preferences.OverlayDiscreet;
        Height = discreet ? (preferences.OverlayCompact ? 24 : 30) : preferences.OverlayCompact ? 28 : 42;
        layout.Margin = new Thickness(6, 2, 6, 2);
        rate.FontSize = discreet || preferences.OverlayCompact ? 15 : 17;
        name.FontWeight = discreet && !self ? FontWeights.Medium : FontWeights.SemiBold;
        detail.Visibility = total.Visibility = discreet || preferences.OverlayCompact ? Visibility.Collapsed : Visibility.Visible;
        name.VerticalAlignment = VerticalAlignment.Center;
        name.Text = title; rate.Text = number; share.Text = percentage; detail.Text = subtitle; total.Text = amount;
        name.Foreground = rate.Foreground = Themes.Brush(palette.Foreground);
        share.Foreground = detail.Foreground = total.Foreground = Themes.Brush(palette.Muted);
        Background = discreet ? Brushes.Transparent : Themes.Brush(palette.Surface);
        BorderBrush = Themes.Brush(self ? palette.Accent : palette.Border); BorderThickness = new Thickness(!discreet && self ? 1 : 0);
        bar.Background = Dashboard.ClassColor(className);
        bar.Height = discreet ? 3 : double.NaN;
        bar.VerticalAlignment = discreet ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;
        bar.Opacity = discreet ? (preferences.Theme == "contrast" ? .8 : .35) : preferences.Theme == "contrast" ? .13 : .2;
        fill.Width = new GridLength(Math.Max(.001, fraction), GridUnitType.Star);
        rest.Width = new GridLength(Math.Max(.001, 100 - fraction), GridUnitType.Star);
        var iconSize = discreet ? 18 : preferences.OverlayCompact ? 20 : 24;
        emblem.Content = skillId is { } id ? GameArtwork.SkillIcon(id, title, iconSize) : CombatPresentation.Emblem(className, iconSize);
        System.Windows.Automation.AutomationProperties.SetName(this, title + ", " + subtitle + ", " + number);
    }
}
