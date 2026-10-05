using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Spike.Desktop;

public partial class Dashboard
{
    private const string DiscordContact = "Phobie";
    private const string RequestsUrl = "https://github.com/EnzoCx/Spike/issues";

    private void TranslateSupport()
    {
        RequestsLabel.Text = T("githubRequests");
        SupportHint.Text = T("supportHint");
        RequestsButton.ToolTip = RequestsUrl;
        DiscordLabel.Text = string.Format(T("discordContact"), DiscordContact);
        DiscordCopyLabel.Text = T("copyDiscord");
        DiscordCopyButton.ToolTip = T("discordHint");
    }

    private void OpenRequests(object sender, RoutedEventArgs e)
    {
        if (verifying) return;
        try { Process.Start(new ProcessStartInfo(RequestsUrl) { UseShellExecute = true }); }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            SetNotice("supportBrowserError");
        }
    }

    private void CopyDiscord(object sender, RoutedEventArgs e)
    {
        if (verifying) return;
        CopyDiscordContact(Clipboard.SetText);
    }

    private void CopyDiscordContact(Action<string> write)
    {
        var result = FightSummary.Copy(DiscordContact, write);
        SetNotice(result == "copied" ? "discordCopied" : "copyError");
    }
}
