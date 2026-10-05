using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Spike.Desktop;

public partial class Dashboard
{
    private bool translatingOpacity;

    private void TranslateOpacity()
    {
        translatingOpacity = true;
        try
        {
            OpacityCaption.Text = T("overlayOpacitySettings");
            CombatOpacityLabel.Text = T("combatOpacity");
            IdleOpacityLabel.Text = T("idleOpacity");
            IdleOpacityCheck.Content = T("useIdleOpacity");
            OpacityHint.Text = T("opacitySettingsHint");
            SetOpacityChoices(CombatOpacityChoice, preferences.OverlayCombatOpacity, 15, 100);
            SetOpacityChoices(IdleOpacityChoice, preferences.OverlayIdleOpacity, 5, 15);
            IdleOpacityCheck.IsChecked = preferences.OverlayFadeWhenIdle;
            IdleOpacityChoice.IsEnabled = preferences.OverlayFadeWhenIdle;
            AutomationProperties.SetName(CombatOpacityChoice, CombatOpacityLabel.Text);
            AutomationProperties.SetName(IdleOpacityChoice, IdleOpacityLabel.Text);
        }
        finally { translatingOpacity = false; }
    }

    private void SetOpacityChoices(ComboBox control, double opacity, int minimum, int fallback)
    {
        var percent = double.IsFinite(opacity) ? Math.Round(Math.Clamp(opacity * 100, minimum, 100), 6) : fallback;
        var values = Enumerable.Range(minimum / 5, (100 - minimum) / 5 + 1).Select(value => value * 5d).Append(percent).Distinct().Order();
        var choices = values.Select(value => new OpacityChoice(value / 100, (value / 100).ToString("P0", Culture))).ToArray();
        control.ItemsSource = choices;
        control.SelectedItem = choices.Single(value => value.Value == percent / 100);
    }

    private void OpacitySelected(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || translatingOpacity || ((ComboBox)sender).SelectedItem is not OpacityChoice choice) return;
        preferences = sender == CombatOpacityChoice ? preferences with { OverlayCombatOpacity = choice.Value }
            : preferences with { OverlayIdleOpacity = choice.Value };
        SaveOpacity();
    }

    private void IdleOpacityToggled(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || translatingOpacity) return;
        preferences = preferences with { OverlayFadeWhenIdle = IdleOpacityCheck.IsChecked == true };
        IdleOpacityChoice.IsEnabled = preferences.OverlayFadeWhenIdle;
        SaveOpacity();
    }

    private void SaveOpacity()
    {
        overlay?.Apply(preferences);
        if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
    }
}

public sealed record OpacityChoice(double Value, string Name);
