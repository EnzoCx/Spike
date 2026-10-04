using System.Windows;

namespace DPSMeter.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 3 && e.Args[0] == "--render-file")
        {
            try { new Dashboard(verification: true).RenderSaved(e.Args[1], e.Args[2]); Shutdown(0); }
            catch (Exception error) { System.IO.Directory.CreateDirectory(e.Args[2]); System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[2], "failure.txt"), error.ToString()); Shutdown(1); }
            return;
        }
        if (e.Args.Length == 3 && e.Args[0] == "--verify-live")
        {
            try { await new Dashboard(verification: true).VerifyLive(int.Parse(e.Args[1]), e.Args[2]); Shutdown(0); }
            catch (Exception error)
            {
                System.IO.Directory.CreateDirectory(e.Args[2]);
                System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[2], "failure.txt"), error.ToString()); Shutdown(1);
            }
            return;
        }
        if (e.Args.Length == 2 && e.Args[0] == "--verify-views")
        {
            try { new Dashboard(verification: true).VerifyViews(e.Args[1]); Shutdown(0); }
            catch (Exception error)
            {
                System.IO.Directory.CreateDirectory(e.Args[1]);
                System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[1], "failure.txt"), error.ToString());
                Shutdown(1);
            }
            return;
        }
        MainWindow = new Dashboard();
        MainWindow.Show();
    }
}
