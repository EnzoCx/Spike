using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifyOpacitySettings(string directory)
    {
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = new(language, theme, OverlayFadeWhenIdle: false);
                ApplyTheme(); Translate(); SwitchPage("settings");
                if (IdleOpacityChoice.IsEnabled || CombatOpacityChoice.Value / 100 != 1)
                    throw new InvalidOperationException("Opacity settings must reflect saved defaults and idle fading.");
                OpenOverlay();
                CharacterName.Text = "Unsaved preview";
                CombatOpacityChoice.Value = 100 * .55;
                IdleOpacityCheck.IsChecked = true;
                IdleOpacityChoice.Value = 100 * .25;
                if (preferences.OverlayCombatOpacity != .55 || preferences.OverlayIdleOpacity != .25 || !preferences.OverlayFadeWhenIdle || CharacterName.Text != "Unsaved preview")
                    throw new InvalidOperationException("Opacity changes must apply immediately without discarding other settings edits.");
                var surface = ((Grid)overlay!.Content).Children[0];
                overlay.Update(null, "capturing");
                if (Math.Abs(surface.Opacity - .55) > .001) throw new InvalidOperationException("Combat opacity was not applied to the open overlay.");
                overlay.Update(null, "paused");
                if (Math.Abs(surface.Opacity - .25) > .001) throw new InvalidOperationException("Idle opacity was not applied to the open overlay.");
                preferences = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(preferences))!;
                TranslateOpacity();
                if (CombatOpacityChoice.Value / 100 != .55 || IdleOpacityChoice.Value / 100 != .25)
                    throw new InvalidOperationException("Opacity choices were lost after restoring saved settings.");
                IdleOpacityCheck.IsChecked = false;
                if (IdleOpacityChoice.IsEnabled || Math.Abs(surface.Opacity - .55) > .001)
                    throw new InvalidOperationException("Disabling separate idle opacity must restore combat opacity.");
                overlay.VerifySelectOpacity(true, .15);
                overlay.VerifySelectOpacity(false, .75);
                if (IdleOpacityCheck.IsChecked != true || !IdleOpacityChoice.IsEnabled || IdleOpacityChoice.Value / 100 != .15 || CombatOpacityChoice.Value / 100 != .75)
                    throw new InvalidOperationException("Overlay menu changes must synchronize the settings page.");
                SaveDashboard(directory, $"opacity-settings-{language}-{theme}.png", 884, 600);
                if (OpacityCaption.ActualHeight < 12 || CombatOpacityChoice.ActualWidth < 100 || IdleOpacityChoice.ActualWidth < 100)
                    throw new InvalidOperationException("Opacity settings must remain readable at minimum size.");
                overlay.Close();
            }
        preferences = new(); ApplyTheme(); Translate(); SwitchPage("live");
        File.WriteAllText(Path.Combine(directory, "opacity-settings-result.txt"), "PASS: combat/idle opacity controls, immediate application, saved values, preserving drafts and two-way menu synchronization in all languages/themes. No capture, visible window or real settings writes.");
    }
}
