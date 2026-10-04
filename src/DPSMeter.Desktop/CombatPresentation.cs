using System.Globalization;
using System.Windows;

namespace DPSMeter.Desktop;

internal static class CombatPresentation
{
    public static string Duration(long milliseconds) => TimeSpan.FromMilliseconds(milliseconds).ToString(milliseconds >= 3_600_000 ? @"hh\:mm\:ss" : @"mm\:ss");
    public static string Short(double number, string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        return number >= 1_000_000 ? (number / 1_000_000).ToString("N2", culture) + " M"
            : number >= 10_000 ? (number / 1000).ToString("N1", culture) + " k" : number.ToString("N0", culture);
    }

    public static FrameworkElement Emblem(string name, double size = 28) => GameArtwork.ClassIcon(name, size);
}
