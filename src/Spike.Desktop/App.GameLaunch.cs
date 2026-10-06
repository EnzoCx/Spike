using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Spike.Desktop.Updates;
using Forms = System.Windows.Forms;

namespace Spike.Desktop;

public partial class App
{
    private EventWaitHandle? openRequest;
    private RegisteredWaitHandle? openWait;
    private readonly DispatcherTimer gameTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly GameLaunch gameLaunch = new();
    private Dashboard? dashboard;
    private Forms.NotifyIcon? tray;
    private bool watchGame;
    private bool timerConfigured;
    private bool exiting;

    private bool AcquireApplicationInstance(bool background)
    {
        var user = System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
        openRequest = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Spike.Open." + user, out var created);
        if (!created)
        {
            if (!background) openRequest.Set();
            return false;
        }
        openWait = ThreadPool.RegisterWaitForSingleObject(openRequest, (_, _) => Dispatcher.BeginInvoke(() => OpenDashboard(false)), null, Timeout.Infinite, false);
        return true;
    }

    internal void ConfigureGameLaunch(Preferences preferences)
    {
        watchGame = preferences.LaunchWithGame;
        if (!timerConfigured) { gameTimer.Tick += (_, _) => CheckGame(); timerConfigured = true; }
        if (tray is null)
        {
            using var stream = GetResourceStream(new Uri("pack://application:,,,/Brand/spike.ico"))!.Stream;
            using var icon = new System.Drawing.Icon(stream);
            tray = new Forms.NotifyIcon { Icon = (System.Drawing.Icon)icon.Clone(), Text = Text.ProductName, Visible = true };
            tray.DoubleClick += (_, _) => OpenDashboard(false);
        }
        tray.ContextMenuStrip?.Dispose();
        tray.ContextMenuStrip = TrayMenu.Create(preferences.Language,
            () => dashboard is { IsVisible: true, WindowState: not WindowState.Minimized },
            () => dashboard?.OverlayOpen == true,
            () => OpenDashboard(false), () => dashboard?.Hide(),
            visible => { if (dashboard is null) OpenDashboard(true); dashboard?.SetOverlayVisible(visible); },
            ExitApplication);
        if (watchGame) gameTimer.Start(); else gameTimer.Stop();
    }

    internal bool IsExiting => exiting;
    internal void ExitApplication() { exiting = true; Shutdown(); }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        exiting = true;
        base.OnSessionEnding(e);
    }

    private void CheckGame()
    {
        try
        {
            if (gameLaunch.ShouldOpen(GameLaunch.IsGameRunning(), dashboard is not null)) OpenDashboard(true);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException) { }
    }

    private void OpenDashboard(bool fromGame)
    {
        if (exiting) return;
        if (dashboard is not null)
        {
            if (!fromGame) { dashboard.WindowState = WindowState.Normal; dashboard.Show(); dashboard.Activate(); }
            return;
        }
        dashboard = new Dashboard();
        if (fromGame) { dashboard.ShowActivated = false; dashboard.WindowState = WindowState.Minimized; }
        MainWindow = dashboard;
        dashboard.Closed += (_, _) => dashboard = null;
        dashboard.Show();
        if (AutomaticUpdater.PublishedExecutable() is not null) dashboard.StartUpdates();
    }

    private void DisposeTray()
    {
        tray?.ContextMenuStrip?.Dispose();
        tray?.Icon?.Dispose();
        tray?.Dispose();
        tray = null;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        exiting = true;
        gameTimer.Stop();
        DisposeTray();
        openWait?.Unregister(null);
        openRequest?.Dispose();
        base.OnExit(e);
    }
}
