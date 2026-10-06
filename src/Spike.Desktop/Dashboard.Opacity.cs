using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Spike.Desktop;

public partial class Dashboard
{
    private bool translatingOpacity;
    private readonly System.Windows.Threading.DispatcherTimer opacitySaveTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private bool opacitySaveConfigured;

    private void QueueOpacitySave()
    {
        if (verifying) return;
        if (!opacitySaveConfigured) { opacitySaveTimer.Tick += (_, _) => FlushOpacitySave(); opacitySaveConfigured = true; }
        opacitySaveTimer.Stop(); opacitySaveTimer.Start();
    }

    private void FlushOpacitySave()
    {
        opacitySaveTimer.Stop();
        if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
    }


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
            CombatOpacityChoice.Value = double.IsFinite(preferences.OverlayCombatOpacity) ? Math.Clamp(preferences.OverlayCombatOpacity * 100, 15, 100) : 100;
            IdleOpacityChoice.Value = double.IsFinite(preferences.OverlayIdleOpacity) ? Math.Clamp(preferences.OverlayIdleOpacity * 100, 5, 100) : 15;
            IdleOpacityCheck.IsChecked = preferences.OverlayFadeWhenIdle;
            IdleOpacityChoice.IsEnabled = preferences.OverlayFadeWhenIdle;
            AutomationProperties.SetName(CombatOpacityChoice, CombatOpacityLabel.Text);
            AutomationProperties.SetName(IdleOpacityChoice, IdleOpacityLabel.Text);
        }
        finally { translatingOpacity = false; }
    }

    private void OpacitySelected(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized || translatingOpacity || sender is not Slider control) return;
        preferences = sender == CombatOpacityChoice ? preferences with { OverlayCombatOpacity = control.Value / 100 }
            : preferences with { OverlayIdleOpacity = control.Value / 100 };
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
        QueueOpacitySave();
    }
}
