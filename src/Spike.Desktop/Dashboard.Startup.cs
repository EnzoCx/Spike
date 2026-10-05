using System.IO;
using System.Security;
using Spike.Desktop.Updates;

namespace Spike.Desktop;

public partial class Dashboard
{
    private bool SaveGameStartup(bool enabled)
    {
        if (enabled == preferences.LaunchWithGame) return true;
        var previous = preferences;
        var executable = AutomaticUpdater.PublishedExecutable();
        if (!verifying && executable is null) { SetNotice("startupError"); return false; }
        try
        {
            if (!verifying) GameLaunch.SetWindowsStartup(enabled, executable!);
            preferences = preferences with { LaunchWithGame = enabled };
            if (!verifying) preferences.Save();
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            preferences = previous;
            try { if (!verifying) GameLaunch.SetWindowsStartup(previous.LaunchWithGame, executable!); }
            catch (Exception rollback) when (rollback is IOException or UnauthorizedAccessException or SecurityException) { }
            LaunchWithGameCheck.IsChecked = previous.LaunchWithGame;
            SetNotice("startupError");
            return false;
        }
    }
}
