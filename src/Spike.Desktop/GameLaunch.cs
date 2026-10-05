using System.Diagnostics;
using Microsoft.Win32;

namespace Spike.Desktop;

internal sealed class GameLaunch
{
    private bool wasRunning;

    internal bool ShouldOpen(bool gameRunning, bool meterOpen)
    {
        var open = gameRunning && !wasRunning && !meterOpen;
        wasRunning = gameRunning;
        return open;
    }

    internal static bool IsGameRunning()
    {
        // Only enumerate process names. Never open game memory or interact with its window.
        var processes = Process.GetProcessesByName("AION2");
        try { return processes.Length > 0; }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    internal static string StartupCommand(string executable) => $"\"{executable}\" --watch-game";

    internal static void SetWindowsStartup(bool enabled, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (enabled) key.SetValue("Spike", StartupCommand(executable));
        else key.DeleteValue("Spike", throwOnMissingValue: false);
    }
}
