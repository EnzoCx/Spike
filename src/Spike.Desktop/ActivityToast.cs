using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace Spike.Desktop;

/// <summary>One non-activating top-right stack. Each visible reminder gets its own full reading time.</summary>
internal sealed partial class ActivityToast : Window
{
    private readonly Queue<(string Title, string Message)> pending = new();
    private readonly List<(Border Card, DateTimeOffset Expires)> visible = [];
    private readonly StackPanel cards = new();
    private readonly TextBlock waiting = new() { FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 10, 0) };
    private readonly Preferences preferences;
    private readonly Action open;
    private readonly Window reference;
    private readonly OverlayPlacement placement;
    private readonly DispatcherTimer timer;
    private string T(string key) => Text.Get(key, preferences.Language);
    internal int VisibleCount => visible.Count;
    internal int PendingCount => pending.Count;

    public ActivityToast(Preferences preferences, Action open, Window reference)
    {
        this.preferences = preferences; this.open = open; this.reference = reference;
        var palette = Themes.Get(preferences.Theme);
        foreach (var (key, color) in new[] { ("Background", palette.Background), ("Surface", palette.Surface), ("Foreground", palette.Foreground), ("Muted", palette.Muted), ("Accent", palette.Accent), ("Border", palette.Border), ("Hover", palette.Hover), ("Brand", palette.Brand) })
            Resources[key] = Themes.Brush(color);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CommonStyles.xaml") });
        Title = Text.ProductName + " · " + T("activities");
        Width = 340; SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent; Topmost = true;
        ShowActivated = false; ShowInTaskbar = false; UseLayoutRounding = true;
        FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Geist");
        Foreground = Themes.Brush(palette.Foreground); waiting.Foreground = Themes.Brush(palette.Muted);
        var stack = new StackPanel(); stack.Children.Add(cards); stack.Children.Add(waiting); Content = stack;
        placement = new(this, () => false, () => { });
        Loaded += (_, _) => { Fill(); placement.PlaceNotification(reference); };
        SizeChanged += (_, _) => placement.PlaceNotification(reference);
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) => Expire(DateTimeOffset.UtcNow, IsMouseOver || IsKeyboardFocusWithin);
        Closed += (_, _) => { timer.Stop(); pending.Clear(); visible.Clear(); };
        timer.Start();
    }

    public void Enqueue(string title, string message) { pending.Enqueue((title, message)); Fill(); }

    private void Fill()
    {
        var limit = Math.Clamp((int)((placement.NotificationHeight(reference) - 60) / 112), 1, 4);
        while (visible.Count < limit && pending.TryDequeue(out var entry))
        {
            var panel = new DockPanel();
            var close = new Button { Content = "×", ToolTip = T("dismissReminder"), Padding = new Thickness(5, 1, 5, 1), Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top };
            System.Windows.Automation.AutomationProperties.SetName(close, T("dismissReminder"));
            DockPanel.SetDock(close, Dock.Right); panel.Children.Add(close);
            var body = new StackPanel();
            var brand = new TextBlock { Text = "SPIKE  /  " + T("upNext"), FontSize = 10, Margin = new Thickness(0, 0, 0, 6) };
            brand.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); body.Children.Add(brand);
            body.Children.Add(new TextBlock { Text = entry.Title, ToolTip = entry.Title, TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 14, FontWeight = FontWeights.SemiBold });
            var detail = new TextBlock { Text = entry.Message, TextWrapping = TextWrapping.Wrap, MaxHeight = 32, FontSize = 11, Margin = new Thickness(0, 5, 0, 0), ToolTip = entry.Message };
            detail.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); body.Children.Add(detail);
            var view = new Button { Content = body, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), Margin = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch };
            System.Windows.Automation.AutomationProperties.SetName(view, entry.Title + " · " + entry.Message + " · " + T("openActivities"));
            panel.Children.Add(view);
            var card = new Border { Child = panel, Height = 104, Padding = new Thickness(14, 12, 12, 12), CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 8) };
            card.SetResourceReference(Border.BackgroundProperty, "Background"); card.SetResourceReference(Border.BorderBrushProperty, "Border");
            card.Loaded += (_, _) =>
            {
                if (!IsVisible || !SystemParameters.ClientAreaAnimation) return;
                var shift = new TranslateTransform(); card.RenderTransform = shift;
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                shift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
                card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { FillBehavior = FillBehavior.Stop });
            };
            close.Click += (_, _) => Dismiss(card);
            view.Click += (_, _) => { open(); Dismiss(card); };
            visible.Add((card, DateTimeOffset.UtcNow.AddSeconds(12))); cards.Children.Add(card);
        }
        waiting.Text = pending.Count > 0 ? string.Format(System.Globalization.CultureInfo.GetCultureInfo(preferences.Language), T("moreEvents"), pending.Count) : "";
        waiting.Visibility = pending.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Dismiss(Border card)
    {
        visible.RemoveAll(v => ReferenceEquals(v.Card, card)); cards.Children.Remove(card); Fill();
        if (visible.Count == 0 && pending.Count == 0) Close();
    }

    internal void Expire(DateTimeOffset now, bool reading)
    {
        if (reading)
        {
            for (var i = 0; i < visible.Count; i++) visible[i] = (visible[i].Card, now.AddSeconds(12));
            return;
        }
        // Snapshot: queued cards promoted by Dismiss receive a new full lifetime.
        foreach (var item in visible.Where(v => v.Expires <= now).ToArray()) Dismiss(item.Card);
    }
}
