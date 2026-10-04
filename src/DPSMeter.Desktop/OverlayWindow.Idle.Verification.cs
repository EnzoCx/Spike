using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DPSMeter.Core;

namespace DPSMeter.Desktop;

public sealed partial class OverlayWindow
{
    private static void VerifyIdleCollapse(Preferences preferences, Encounter encounter, string directory, Style style)
    {
        var checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Idle collapse: " + message);
            checks++;
        }
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
                foreach (var discreet in new[] { false, true })
                    foreach (var autoFit in new[] { false, true })
                    {
                        var window = new OverlayWindow(preferences with { Language = language, Theme = theme, OverlayAutoFit = autoFit, OverlayDiscreet = discreet }, style);
                        window.Update(encounter, "capturing");
                        // Create an invisible HWND to verify real resizing and bottom anchoring.
                        new WindowInteropHelper(window).EnsureHandle();
                        window.placement.Place("bottomRight");
                        var height = window.Height;
                        var top = window.Top;
                        var bottom = top + height;
                        var first = window.rows.Children[0];
                        window.Update(encounter, "connected");
                        var start = window.idleSince!.Value;
                        window.UpdateIdleLayout(start.AddSeconds(119));
                        Check(!window.idleCollapsed, "must allow two minutes to read results");
                        window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent });
                        window.UpdateIdleLayout(start.AddMinutes(2));
                        Check(window.idleCollapsed && window.Height == 44 && window.scroll.Visibility == Visibility.Collapsed, "must reduce the actual window even under the pointer");
                        Check(Math.Abs(window.Top + window.Height - bottom) < 2, "reduction must keep bottom anchoring");
                        window.Update(encounter, "connected");
                        Check(window.Height == 44 && ReferenceEquals(first, window.rows.Children[0]), "refresh must preserve reduction and rows");
                        window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseLeaveEvent });
                        window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent });
                        Check(window.idleCollapsed, "passing the mouse must not expand");
                        double savedHeight = 0, savedTop = 0;
                        window.LayoutSaved += (_, y, _, h) => { savedHeight = h; savedTop = y; };
                        window.SaveLayout();
                        var expectedSavedHeight = discreet ? preferences.OverlayHeight : height;
                        Check(savedHeight == expectedSavedHeight, $"reduced height must not overwrite saved dimensions ({savedHeight} != {expectedSavedHeight})");
                        Check(Math.Abs(savedTop - top) < 2, "reduction must preserve the expanded saved position");
                        window.SavePreview(directory, $"overlay-reduced-{language}-{theme}-{autoFit}-{discreet}.png");
                        window.expand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Check(!window.idleCollapsed && window.Height == height, "explicit click must restore the previous size");
                        Check(Math.Abs(window.Top - top) < 2, "expansion must restore bottom anchored position");
                        window.UpdateIdleLayout(window.idleSince!.Value.AddSeconds(119));
                        Check(!window.idleCollapsed, "manual expansion must restart the grace period");
                        window.UpdateIdleLayout(window.idleSince.Value.AddMinutes(2));
                        window.Update(encounter with { Id = Guid.NewGuid() }, "capturing");
                        Check(!window.idleCollapsed && window.Height == height && window.idleSince is null, "combat must restore full size immediately");
                        window.Update(encounter, "waiting");
                        window.openMenus = 1;
                        window.UpdateIdleLayout(window.idleSince!.Value.AddMinutes(3));
                        Check(!window.idleCollapsed, "open menus must delay reduction");
                        window.openMenus = 0; window.manipulating = true;
                        window.UpdateIdleLayout(window.idleSince.Value.AddMinutes(3));
                        Check(!window.idleCollapsed, "moving or resizing must delay reduction");
                        window.manipulating = false;
                        window.UpdateIdleLayout(window.idleSince.Value.AddMinutes(3));
                        window.SelectFight(encounter);
                        Check(!window.idleCollapsed && window.Height == height, "archives must remain expanded");
                        window.UpdateIdleLayout(window.idleSince.Value.AddMinutes(10));
                        Check(!window.idleCollapsed, "archive reading must not time out");
                        window.SelectFight(null);
                        window.locked = true;
                        window.UpdateIdleLayout(window.idleSince.Value.AddMinutes(3));
                        Check(window.idleCollapsed && window.expand.Visibility == Visibility.Collapsed, "locked overlay must also reduce");
                        window.Update(encounter, "capturing");
                        Check(!window.idleCollapsed && window.locked, "new fight must preserve click-through lock");
                        window.Update(null, "stopped");
                        window.UpdateIdleLayout(window.idleSince!.Value.AddMinutes(3));
                        Check(window.idleCollapsed, "empty stopped overlay must reduce too");
                        Check(!window.IsVisible, "verification must remain invisible");
                        window.Close();
                    }
        File.WriteAllText(Path.Combine(directory, "idle-collapse-result.txt"), $"PASS: {checks} idle reduction checks, three languages/themes, automatic/manual height; no capture or desktop input.");
    }
}
