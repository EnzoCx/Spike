using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using AionDPS.Aion2.Capture;

namespace DPSMeter.Desktop;

public partial class Dashboard
{
    private Func<bool> detectSetupNpcap = () => NpcapAvailability.Detect().IsInstalled;
    private string setupStatusKey = "setupPending";
    private bool setupStartsCapture;

    private void ShowSetup(bool startCapture = false, Func<bool>? detect = null)
    {
        detectSetupNpcap = detect ?? (() => NpcapAvailability.Detect().IsInstalled);
        setupStartsCapture = startCapture;
        setupStatusKey = "setupPending";
        SetupContinue.IsEnabled = false;
        SetSetupVisible(true);
        TranslateSetup();
        SetupDownload.Focus();
    }

    private void SetSetupVisible(bool visible)
    {
        foreach (UIElement child in ((Grid)Content).Children)
            if (child != SetupPanel) child.IsEnabled = !visible;
        SetupPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TranslateSetup()
    {
        SetupTitle.Text = T("setupTitle"); SetupIntro.Text = T("setupIntro");
        SetupSteps.Text = T("setupSteps"); SetupStatus.Text = T(setupStatusKey);
        SetupDownload.Content = T("setupDownload"); SetupCheck.Content = T("setupCheck");
        SetupContinue.Content = T("setupContinue"); SetupLater.Content = T("setupLater");
        SetupLanguages.Children.Clear();
        string[] names = ["Français", "English", "Español"];
        for (var i = 0; i < Text.Languages.Length; i++)
        {
            var language = Text.Languages[i];
            SetupLanguages.Children.Add(Choice(names[i], preferences.Language == language, () =>
            {
                Change(preferences with { Language = language });
                TranslateSetup();
            }));
        }
    }

    private void DownloadNpcap(object sender, RoutedEventArgs e)
    {
        if (verifying) return;
        try { Process.Start(new ProcessStartInfo(NpcapAvailability.DownloadUrl) { UseShellExecute = true }); }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            setupStatusKey = "setupBrowserError";
            SetupStatus.Text = T(setupStatusKey);
        }
    }

    private bool RecheckNpcap()
    {
        var installed = detectSetupNpcap();
        SetupContinue.IsEnabled = installed;
        setupStatusKey = installed ? "setupDetected" : "setupMissing";
        SetupStatus.Text = T(setupStatusKey);
        return installed;
    }

    private void CheckNpcap(object sender, RoutedEventArgs e) => RecheckNpcap();

    private void CompleteSetup(object sender, RoutedEventArgs e)
    {
        if (!RecheckNpcap()) return;
        SetSetupVisible(false);
        noticeKey = null;
        Translate();
        if (!verifying)
        {
            StartSession();
            if (setupStartsCapture) StartCapture();
        }
        StartButton.Focus();
    }

    private void DeferSetup(object sender, RoutedEventArgs e)
    {
        SetSetupVisible(false);
        SetNotice("npcapRequired");
        StartButton.Focus();
    }
}
