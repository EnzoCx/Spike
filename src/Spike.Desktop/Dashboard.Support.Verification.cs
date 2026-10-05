using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifySupportAndStartup(string directory)
    {
        var legacy = JsonSerializer.Deserialize<Preferences>("{\"Language\":\"fr\"}")!;
        if (legacy.LaunchWithGame || legacy.OverlayCombatOpacity != 1 || legacy.OverlayIdleOpacity != .15)
            throw new InvalidOperationException("New preferences must preserve defaults and keep startup opt-in.");
        var launch = new GameLaunch();
        if (launch.ShouldOpen(false, false) || !launch.ShouldOpen(true, false) || launch.ShouldOpen(true, false))
            throw new InvalidOperationException("Game launch must trigger once, including a game already running at Windows login.");
        launch.ShouldOpen(false, false);
        if (launch.ShouldOpen(true, true) || launch.ShouldOpen(true, false))
            throw new InvalidOperationException("An existing or manually closed meter must not reopen during the same game session.");
        launch.ShouldOpen(false, false);
        if (!launch.ShouldOpen(true, false)) throw new InvalidOperationException("A new game session must reopen the meter.");
        if (GameLaunch.StartupCommand(@"C:\Example Folder\Spike.exe") != "\"C:\\Example Folder\\Spike.exe\" --watch-game")
            throw new InvalidOperationException("Startup must quote executable paths containing spaces.");
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = new(language, theme);
                ApplyTheme(); Translate();
                string? copied = null;
                CopyDiscordContact(value => copied = value);
                if (copied != "Phobie" || Notice.Text != T("discordCopied") || !DiscordLabel.Text.Contains(copied) || RequestsLabel.Text != T("githubRequests") || SupportHint.Text != T("supportHint"))
                    throw new InvalidOperationException("Support contact and translated copy feedback are missing.");
                CopyDiscordContact(_ => throw new COMException("Clipboard unavailable"));
                if (Notice.Text != T("copyError")) throw new InvalidOperationException("Clipboard failure must be visible.");
                if (!SaveGameStartup(true) || !preferences.LaunchWithGame || !SaveGameStartup(false) || preferences.LaunchWithGame)
                    throw new InvalidOperationException("Startup must remain reversible without registry access during verification.");
                SaveDashboard(directory, $"support-{language}-{theme}.png", 884, 600);
                if (RequestsLabel.ActualHeight < 12 || RequestsLabel.ActualWidth > RequestsButton.ActualWidth - 16 || DiscordCopyLabel.ActualWidth > DiscordCopyButton.ActualWidth - 16)
                    throw new InvalidOperationException("Support labels must fit at minimum window size.");
            }
        preferences = new(); ApplyTheme(); Translate();
        File.WriteAllText(Path.Combine(directory, "support-startup-result.txt"), "PASS: contact/copy/failure feedback and support sizing in FR/EN/ES and all themes; opt-in settings, quoted startup command, simulated game sessions and duplicate suppression. No registry writes, tray icon, real game launch, clipboard writes or browser opened.");
    }
}
