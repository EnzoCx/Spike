using System.Windows;
using System.Windows.Automation;
using Spike.Desktop.Updates;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifyUpdates(string directory)
    {
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = preferences with { Language = language, Theme = theme };
                ApplyTheme();
                foreach (var state in new[] { "updateDownloading", "updateReady", "updateCurrent", "updateFailed", "updateUnavailable", "updateRestarting", "updateRestartFailed" })
                {
                    updateStatusKey = state;
                    downloadingUpdate = state == "updateDownloading";
                    restartingUpdate = state == "updateRestarting";
                    TranslateUpdate();
                    var ready = state is "updateReady" or "updateRestarting" or "updateRestartFailed";
                    if (UpdateStatus.Text != T(state) || UpdateStatus.Visibility != Visibility.Visible ||
                        UpdateButton.Visibility != (ready || state == "updateCurrent" ? Visibility.Collapsed : Visibility.Visible) ||
                        UpdateButton.IsEnabled == (downloadingUpdate || restartingUpdate) || AutomationProperties.GetName(UpdateButton) != T("downloadUpdate") ||
                        RestartUpdateButton.Visibility != (ready ? Visibility.Visible : Visibility.Collapsed) ||
                        RestartUpdateButton.IsEnabled == (downloadingUpdate || restartingUpdate) ||
                        AutomationProperties.GetName(RestartUpdateButton) != T("restartUpdate") || RestartUpdateLabel.Text != T("restartUpdate"))
                        throw new InvalidOperationException("Update control state or translation failed.");
                    if (state == "updateReady")
                    {
                        SaveDashboard(directory, $"update-ready-{language}-{theme}.png", 884, 600);
                        if (RestartUpdateLabel.ActualWidth > RestartUpdateButton.ActualWidth - 20 || RestartUpdateLabel.ActualHeight < 12 ||
                            RestartUpdateLabel.Foreground != RestartUpdateButton.Foreground)
                            throw new InvalidOperationException("Restart label must fit and use the themed button foreground.");
                        RestartUpdateButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                        if (updateStatusKey != "updateReady") throw new InvalidOperationException("Verification must not restart the app.");
                    }
                }
                updateStatusKey = null;
                downloadingUpdate = false;
                restartingUpdate = false;
                TranslateUpdate();
                UpdateButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                if (updateStatusKey is not null) throw new InvalidOperationException("Offscreen update button must not contact the network.");
            }
        StartUpdates();
        if (updatePolling is not null) throw new InvalidOperationException("Verification must not start periodic network checks.");
        DownloadUpdatesAsync(() => Task.FromResult(UpdateDownloadResult.Ready)).GetAwaiter().GetResult();
        DownloadUpdatesAsync(() => Task.FromResult(UpdateDownloadResult.Failed)).GetAwaiter().GetResult();
        if (!UpdateReady) throw new InvalidOperationException("An offline check must retain the ready update.");
        var closed = false;
        RestartForUpdateAsync(() => Task.FromResult(false), () => closed = true).GetAwaiter().GetResult();
        if (closed || updateStatusKey != "updateRestartFailed" || !RestartUpdateButton.IsEnabled)
            throw new InvalidOperationException("Installer failure must keep the meter open and allow retry.");
        RestartForUpdateAsync(() => Task.FromResult(true), () => closed = true).GetAwaiter().GetResult();
        if (!closed || !restartingUpdate) throw new InvalidOperationException("Prepared installer must use normal closing.");
        RestartForUpdateAsync(() => throw new InvalidOperationException("Duplicate installer"), () => { }).GetAwaiter().GetResult();
        DownloadUpdatesAsync(() => throw new InvalidOperationException("Download during restart")).GetAwaiter().GetResult();
        restartingUpdate = false;
        updateStatusKey = null;
        TranslateUpdate();
    }
}
