using System.Windows;
using System.Windows.Automation;
using Spike.Desktop.Updates;

namespace Spike.Desktop;

public partial class Dashboard
{
    private string? updateStatusKey;
    private bool downloadingUpdate;
    private bool restartingUpdate;
    private CancellationTokenSource? updatePolling;
    private bool UpdateReady => updateStatusKey is "updateReady" or "updateRestartFailed" or "updateRestarting";

    internal void StartUpdates()
    {
        if (verifying || updatePolling is not null) return;
        updatePolling = new CancellationTokenSource();
        _ = UpdatePolling.RunAsync(() => DownloadUpdatesAsync(), updatePolling.Token);
    }

    private void StopUpdates()
    {
        updatePolling?.Cancel();
    }

    private void TranslateUpdate()
    {
        VersionLabel.Text = typeof(Dashboard).Assembly.GetName().Version!.ToString(3);
        UpdateButton.ToolTip = T("downloadUpdate");
        AutomationProperties.SetName(UpdateButton, T("downloadUpdate"));
        UpdateButton.IsEnabled = !downloadingUpdate && !restartingUpdate;
        UpdateButton.Visibility = UpdateReady || updateStatusKey == "updateCurrent" ? Visibility.Collapsed : Visibility.Visible;
        RestartUpdateButton.Visibility = UpdateReady ? Visibility.Visible : Visibility.Collapsed;
        RestartUpdateButton.IsEnabled = !downloadingUpdate && !restartingUpdate;
        RestartUpdateLabel.Text = T("restartUpdate");
        RestartUpdateButton.ToolTip = T("restartUpdateHint");
        AutomationProperties.SetName(RestartUpdateButton, T("restartUpdate"));
        UpdateStatus.Text = updateStatusKey is null ? "" : T(updateStatusKey);
        UpdateStatus.Visibility = updateStatusKey is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateButton.Foreground = (System.Windows.Media.Brush)FindResource(updateStatusKey == "updateReady" ? "Accent" : "Muted");
    }

    private async void DownloadUpdate(object sender, RoutedEventArgs e) => await DownloadUpdatesAsync();

    private async void RestartUpdate(object sender, RoutedEventArgs e)
    {
        if (verifying) return;
        await RestartForUpdateAsync(() => Task.Run(() =>
        {
            var executable = AutomaticUpdater.PublishedExecutable();
            return executable is not null && AutomaticUpdater.TryApplyPending(executable);
        }), () => ((App)Application.Current).ExitApplication());
    }

    private async Task RestartForUpdateAsync(Func<Task<bool>> prepare, Action close)
    {
        if (!UpdateReady || downloadingUpdate || restartingUpdate) return;
        restartingUpdate = true;
        updateStatusKey = "updateRestarting";
        TranslateUpdate();
        // Only close after the verified installer is running. Normal closing drains and saves capture.
        if (await prepare()) { close(); return; }
        restartingUpdate = false;
        updateStatusKey = "updateRestartFailed";
        TranslateUpdate();
    }

    internal async Task DownloadUpdatesAsync(Func<Task<UpdateDownloadResult>>? download = null)
    {
        if ((verifying && download is null) || downloadingUpdate || restartingUpdate || updatePolling?.IsCancellationRequested == true) return;
        downloadingUpdate = true;
        var previousState = updateStatusKey;
        var wasReady = UpdateReady;
        if (!wasReady) updateStatusKey = "updateDownloading";
        TranslateUpdate();
        try
        {
            var executable = verifying ? null : AutomaticUpdater.PublishedExecutable();
            var result = download is not null ? await download() : executable is null ? UpdateDownloadResult.Unavailable :
                await Task.Run(() => AutomaticUpdater.DownloadAsync(executable));
            updateStatusKey = wasReady && result != UpdateDownloadResult.Ready ? previousState : result switch
            {
                UpdateDownloadResult.Ready => "updateReady",
                UpdateDownloadResult.Current => "updateCurrent",
                UpdateDownloadResult.Unavailable => "updateUnavailable",
                _ => "updateFailed"
            };
        }
        finally { downloadingUpdate = false; TranslateUpdate(); }
    }
}
