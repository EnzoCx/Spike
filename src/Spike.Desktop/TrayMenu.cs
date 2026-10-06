using Forms = System.Windows.Forms;

namespace Spike.Desktop;

internal static class TrayMenu
{
    internal static Forms.ContextMenuStrip Create(string language, Func<bool> mainVisible, Func<bool> overlayVisible,
        Action showMain, Action hideMain, Action<bool> setOverlayVisible, Action quit)
    {
        var menu = new Forms.ContextMenuStrip();
        var main = new Forms.ToolStripMenuItem();
        var overlay = new Forms.ToolStripMenuItem();
        main.Click += (_, _) => { if (mainVisible()) hideMain(); else showMain(); };
        overlay.Click += (_, _) => setOverlayVisible(!overlayVisible());
        void Refresh()
        {
            main.Text = Text.Get(mainVisible() ? "hideSpike" : "openSpike", language);
            overlay.Text = Text.Get(overlayVisible() ? "hideOverlay" : "showOverlay", language);
        }
        menu.Items.Add(main); menu.Items.Add(overlay); menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Text.Get("quitSpike", language), null, (_, _) => quit());
        menu.Opening += (_, _) => Refresh(); Refresh();
        return menu;
    }
}
