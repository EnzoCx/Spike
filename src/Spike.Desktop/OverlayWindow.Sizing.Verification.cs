using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private static void VerifyManualHeight(Preferences defaults, Encounter encounter, string directory, Style style)
    {
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
                foreach (var discreet in new[] { true, false })
                {
                    var saved = defaults with { Language = language, Theme = theme, OverlayDiscreet = discreet };
                    var window = new OverlayWindow(saved, style);
                    window.AppearanceChanged += (auto, compact, opacity) => saved = saved with { OverlayAutoFit = auto, OverlayCompact = compact, OverlayOpacity = opacity };
                    window.LayoutSaved += (left, top, width, height) => saved = saved with { OverlayLeft = left, OverlayTop = top, OverlayWidth = width, OverlayHeight = height };
                    window.Update(encounter, "capturing");
                    var first = window.rows.Children[0];
                    var height = discreet ? 184 : 240;
                    window.grip.RaiseEvent(new DragStartedEventArgs(0, 0));
                    window.grip.RaiseEvent(new DragDeltaEventArgs(0, height - window.Height));
                    window.grip.RaiseEvent(new DragCompletedEventArgs(0, 0, false));
                    window.Update(encounter, "capturing");
                    window.SavePreview(directory, $"overlay-manual-{language}-{theme}-{discreet}.png");
                    if (saved.OverlayAutoFit || window.Height != height || saved.OverlayHeight != height || window.scroll.ScrollableHeight <= 0 || !ReferenceEquals(first, window.rows.Children[0]))
                        throw new InvalidOperationException("Manual height must survive live refresh, preserve rows and allow scrolling.");
                    window.Update(null, "connected");
                    window.UpdateIdleLayout(window.idleSince!.Value.AddMinutes(3));
                    window.SaveLayout();
                    if (saved.OverlayHeight != height) throw new InvalidOperationException("Idle collapse overwrote manual height.");
                    window.Close();
                    var restored = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(saved))!;
                    window = new OverlayWindow(restored, style);
                    window.Update(encounter, "capturing");
                    if (window.Height != height) throw new InvalidOperationException("Reopening lost manual height.");
                    window.BuildOptionsMenu().Items.OfType<MenuItem>().Single(item => Equals(item.Header, window.T("autoFit"))).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    if (!window.preferences.OverlayAutoFit || window.Height <= height) throw new InvalidOperationException("Automatic sizing cannot be restored.");
                    window.Close();
                }
    }
}
