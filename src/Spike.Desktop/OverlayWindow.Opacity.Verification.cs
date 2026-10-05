using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    internal void VerifySelectOpacity(bool idle, double value)
    {
        var menu = BuildOptionsMenu().Items.OfType<MenuItem>().Single(item => Equals(item.Header, T(idle ? "idleOpacity" : "combatOpacity")));
        menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, value.ToString("P0", Culture))).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    }

    private static void VerifyOpacityPreferences(Preferences defaults, Encounter encounter, Style style)
    {
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                var window = new OverlayWindow(defaults with { Language = language, Theme = theme }, style);
                var saved = window.preferences;
                window.VisibilityOpacityChanged += (combat, idle) => saved = saved with { OverlayCombatOpacity = combat, OverlayIdleOpacity = idle };
                void Check(double expected)
                {
                    if (Math.Abs(window.frame.Opacity - expected) > .001) throw new InvalidOperationException("Separate combat/idle opacity was not applied.");
                }
                window.Update(encounter, "capturing");
                var menu = window.BuildOptionsMenu().Items.OfType<MenuItem>().Single(item => Equals(item.Header, window.T("combatOpacity")));
                menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, .5.ToString("P0", window.Culture))).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Check(.5);
                window.ChangeVisibilityOpacity(.5, .25, true);
                foreach (var state in new[] { "paused", "waiting", "connected", "stopped" }) { window.Update(encounter, state); Check(.25); }
                window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent }); Check(1);
                window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseLeaveEvent }); Check(.25);
                window.SelectFight(encounter); Check(1);
                window.SelectFight(null); Check(.25);
                window.ToggleIdleFade(); Check(.5);
                window.ChangeVisibilityOpacity(.5, .25, true); Check(.25);
                if (!window.preferences.OverlayFadeWhenIdle) throw new InvalidOperationException("Choosing idle opacity must enable idle fading.");
                window.Close();
                window = new OverlayWindow(JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(saved))!, style);
                window.Update(encounter, "capturing"); Check(.5);
                window.Update(encounter, "paused"); Check(.25);
                window.Close();
            }
    }
}
