using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private void VerifyMenuDesign(string directory, string language, string theme)
    {
        var menu = BuildOptionsMenu();
        // Measure the menu directly; never open a popup over the desktop.
        SaveElement(menu, 320, null, Path.Combine(directory, $"menu-{language}-{theme}.png"));
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            item.ApplyTemplate();
            if (item.Template.FindName("Check", item) is not FrameworkElement check ||
                check.Visibility != (item.IsChecked ? Visibility.Visible : Visibility.Hidden))
                throw new InvalidOperationException("The styled menu must retain checked preference indicators.");
            if (item.HasItems && item.Template.FindName("PART_Popup", item) is not Popup)
                throw new InvalidOperationException("The styled menu must retain access to placement submenus.");
        }
        if (menu.IsOpen) throw new InvalidOperationException("Menu verification must remain offscreen.");
    }
}
