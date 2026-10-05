using System.Windows;
using System.Windows.Automation;
using Spike.Desktop.Updates;

namespace Spike.Desktop;

public partial class Dashboard
{
    private string? updateStatusKey;
    private bool downloadingUpdate;

    private void TranslateUpdate()
    {
        VersionLabel.Text = typeof(Dashboard).Assembly.GetName().Version!.ToString(3);
        UpdateButton.ToolTip = T("downloadUpdate");
        AutomationProperties.SetName(UpdateButton, T("downloadUpdate"));
        UpdateButton.IsEnabled = !downloadingUpdate;
        UpdateButton.Visibility = updateStatusKey == "updateCurrent" ? Visibility.Collapsed : Visibility.Visible;
        UpdateStatus.Text = updateStatusKey is null ? "" : T(updateStatusKey);
        UpdateStatus.Visibility = updateStatusKey is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateButton.Foreground = (System.Windows.Media.Brush)FindResource(updateStatusKey == "updateReady" ? "Accent" : "Muted");
    }

    private async void DownloadUpdate(object sender, RoutedEventArgs e) => await DownloadUpdatesAsync();

    internal async Task DownloadUpdatesAsync()
    {
        if (verifying || downloadingUpdate) return;
        downloadingUpdate = true;
        updateStatusKey = "updateDownloading";
        TranslateUpdate();
        try
        {
            var executable = AutomaticUpdater.PublishedExecutable();
            var result = executable is null ? UpdateDownloadResult.Unavailable : await Task.Run(() => AutomaticUpdater.DownloadAsync(executable));
            updateStatusKey = result switch
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
