using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifySetup(string directory)
    {
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = new(language, theme);
                ApplyTheme(); Translate();
                var installed = false;
                ShowSetup(detect: () => installed);
                var download = new Uri(SetupDownloadUrl.Text);
                if (download.Scheme != "https" || download.Host != "npcap.com" || !download.AbsolutePath.EndsWith(".exe") || !SetupDownloadUrl.IsReadOnly)
                    throw new InvalidOperationException("Npcap must expose the direct official installer URL and allow copying it.");
                if (LiveNav.IsEnabled || SetupContinue.IsEnabled)
                    throw new InvalidOperationException("Setup must block background navigation and completion.");
                SetupCheck.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (SetupContinue.IsEnabled || setupStatusKey != "setupMissing")
                    throw new InvalidOperationException("Missing Npcap accepted.");
                SaveDashboard(directory, $"{language}-{theme}-setup.png", 884, 600);
                installed = true;
                SetupCheck.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (!SetupContinue.IsEnabled || setupStatusKey != "setupDetected")
                    throw new InvalidOperationException("New installation was not detected.");
                installed = false;
                SetupContinue.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (SetupPanel.Visibility != Visibility.Visible || SetupContinue.IsEnabled)
                    throw new InvalidOperationException("Setup did not recheck before completion.");
                installed = true;
                SetupCheck.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                SetupContinue.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (SetupPanel.Visibility != Visibility.Collapsed || !LiveNav.IsEnabled || meter is not null)
                    throw new InvalidOperationException("Setup completion did not restore the dashboard safely.");
                ShowSetup(startCapture: true, detect: () => false);
                SetupLater.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (SetupPanel.Visibility != Visibility.Collapsed || !LiveNav.IsEnabled || meter is not null)
                    throw new InvalidOperationException("Deferring setup should preserve access without capture.");
            }
        preferences = new(); ApplyTheme(); Translate();
        File.WriteAllText(Path.Combine(directory, "setup-result.txt"),
            "PASS: missing, newly installed and removed Npcap; completion recheck; defer and reopen; FR/EN/ES and all themes at minimum window size. Simulated detection; no driver installed, browser opened, capture started or window shown.");
    }
}
