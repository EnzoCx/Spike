using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Spike.Desktop;

/// <summary>A short, non-activating notification; no modal dialog and no game interaction.</summary>
internal sealed class ActivityToast : Window
{
    public ActivityToast(Preferences preferences, string message, Action open)
    {
        var palette = Themes.Get(preferences.Theme);
        foreach (var (key, color) in new[] { ("Background", palette.Background), ("Surface", palette.Surface), ("Foreground", palette.Foreground), ("Muted", palette.Muted), ("Accent", palette.Accent), ("Border", palette.Border), ("Hover", palette.Hover) })
            Resources[key] = Themes.Brush(color);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CommonStyles.xaml") });
        string T(string key) => Text.Get(key, preferences.Language);
        Title = Text.ProductName + " · " + T("activities");
        Width = 380; SizeToContent = SizeToContent.Height; MaxHeight = 400;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent; Topmost = true;
        ShowActivated = false; ShowInTaskbar = false;
        FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Geist");
        var panel = new StackPanel();
        var header = new DockPanel();
        var close = new Button { Content = "×", ToolTip = T("dismissReminder"), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0) };
        System.Windows.Automation.AutomationProperties.SetName(close, T("dismissReminder"));
        close.Click += (_, _) => Close(); DockPanel.SetDock(close, Dock.Right); header.Children.Add(close);
        header.Children.Add(new TextBlock { Text = Text.ProductName + " · " + T("activities"), FontWeight = FontWeights.SemiBold, FontSize = 13 });
        panel.Children.Add(header);
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 14), FontSize = 14 });
        var view = new Button { Content = T("openActivities"), HorizontalAlignment = HorizontalAlignment.Left };
        view.Click += (_, _) => { open(); Close(); }; panel.Children.Add(view);
        Content = new Border { Child = panel, Padding = new Thickness(18), CornerRadius = new CornerRadius(14), Background = Themes.Brush(palette.Background), BorderBrush = Themes.Brush(palette.Border), BorderThickness = new Thickness(1) };
        Loaded += (_, _) => { Left = SystemParameters.WorkArea.Right - ActualWidth - 18; Top = SystemParameters.WorkArea.Bottom - ActualHeight - 18; };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        timer.Tick += (_, _) => { if (!IsMouseOver && !IsKeyboardFocusWithin) Close(); };
        Closed += (_, _) => timer.Stop(); timer.Start();
    }
}
