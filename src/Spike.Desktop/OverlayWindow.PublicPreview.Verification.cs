using System.Windows;
using System.Windows.Controls;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    internal static void RenderPublicPreview(Preferences preferences, Encounter demo, string directory, Style buttonStyle)
    {
        var window = new OverlayWindow(preferences with { OverlayWidth = 460, OverlayAutoFit = true }, buttonStyle);
        window.Update(demo, "capturing");
        window.SavePreview(directory, $"public-overlay-en-{preferences.Theme}.png");
        window.rows.Children.OfType<CombatantRow>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.SavePreview(directory, $"public-skills-en-{preferences.Theme}.png");
        window.Close();
    }
}
