using System.Windows;
using System.Windows.Automation;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifyUpdates()
    {
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = preferences with { Language = language, Theme = theme };
                ApplyTheme();
                foreach (var state in new[] { "updateDownloading", "updateReady", "updateCurrent", "updateFailed", "updateUnavailable" })
                {
                    updateStatusKey = state;
                    downloadingUpdate = state == "updateDownloading";
                    TranslateUpdate();
                    if (UpdateStatus.Text != T(state) || UpdateStatus.Visibility != Visibility.Visible ||
                        UpdateButton.Visibility != (state == "updateCurrent" ? Visibility.Collapsed : Visibility.Visible) ||
                        UpdateButton.IsEnabled == downloadingUpdate || AutomationProperties.GetName(UpdateButton) != T("downloadUpdate"))
                        throw new InvalidOperationException("Update control state or translation failed.");
                }
                updateStatusKey = null;
                downloadingUpdate = false;
                TranslateUpdate();
                UpdateButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                if (updateStatusKey is not null) throw new InvalidOperationException("Offscreen update button must not contact the network.");
            }
    }
}
