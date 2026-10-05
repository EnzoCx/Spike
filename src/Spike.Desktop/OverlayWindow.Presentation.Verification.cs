using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Spike.Core;

namespace Spike.Desktop;

public sealed partial class OverlayWindow
{
    private static void VerifyDiscreet(Preferences preferences, Encounter encounter, string directory, Style style)
    {
        var migrated = JsonSerializer.Deserialize<Preferences>("{\"Language\":\"fr\",\"OverlayCompact\":false,\"OverlayAutoFit\":false,\"OverlayOpacity\":0.75}")!;
        if (!migrated.OverlayDiscreet || migrated.OverlayCompact || migrated.OverlayAutoFit || migrated.OverlayOpacity != .75)
            throw new InvalidOperationException("Discreet layout must preserve existing appearance preferences.");
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                var window = new OverlayWindow(preferences with { Language = language, Theme = theme, OverlayWidth = 360, OverlayAutoFit = false, OverlayDiscreet = false }, style);
                window.Update(encounter, "capturing");
                var first = window.rows.Children[0];
                var detailedHeight = window.Height;
                var saved = false;
                window.DiscreetChanged += enabled => saved = enabled;
                window.ToggleDiscreet();
                double savedHeight = 0;
                window.LayoutSaved += (_, _, _, height) => savedHeight = height;
                window.SaveLayout();
                if (savedHeight != detailedHeight)
                    throw new InvalidOperationException("Discreet sizing must preserve the saved detailed height.");
                window.SavePreview(directory, $"overlay-discreet-{language}-{theme}.png");
                if (!saved || !ReferenceEquals(first, window.rows.Children[0]) || window.Height >= detailedHeight || window.scroll.ScrollableHeight > 0)
                    throw new InvalidOperationException($"Discreet layout must shrink without replacing or clipping rows: {language}/{theme}, saved={saved}, stable={ReferenceEquals(first, window.rows.Children[0])}, height={window.Height}/{detailedHeight}, overflow={window.scroll.ScrollableHeight}.");
                if (window.status.Visibility != Visibility.Visible || window.healthArea.Visibility != Visibility.Visible || !window.status.Text.Contains(Text.Get("demoLabel", language)))
                    throw new InvalidOperationException("Discreet preview must retain its synthetic-data label.");
                var opacity = window.frame.Background.Opacity;
                var discreetHeight = window.Height;
                window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent });
                if (window.frame.Background.Opacity < opacity || window.Height != discreetHeight || window.frame.Opacity != 1)
                    throw new InvalidOperationException("Hover must improve contrast without moving rows or fading text.");
                window.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseLeaveEvent });
                if (window.frame.Background.Opacity != opacity)
                    throw new InvalidOperationException("Leaving must restore the discreet surface.");
                window.CopySummary(_ => { });
                if (window.total.Text != Text.Get("copiedShort", language))
                    throw new InvalidOperationException("Discreet copy must provide visible feedback.");
                window.Update(encounter with { Origin = "import-unverified" });
                if (!window.status.Text.Contains(Text.Get("importLabel", language)) || window.healthArea.Visibility != Visibility.Visible)
                    throw new InvalidOperationException("Discreet layout must retain unverified-import labels.");
                var extraPlayers = Enumerable.Range(10, 9).ToArray();
                var crowded = encounter with
                {
                    Participants = encounter.Participants.Concat(extraPlayers.Select(id => new Participant(id, $"Preview {id}", "Gladiator", true))).ToArray(),
                    Events = encounter.Events.Concat(extraPlayers.Select(id => new CombatEvent(0, id, 100, 1, "Sample hit", 100, false, false, false))).ToArray()
                };
                window.Update(crowded, "capturing");
                window.SavePreview(directory, $"overlay-many-{language}-{theme}.png");
                if (window.rows.Children.Count != 13 || window.Height > 400 || window.scroll.ScrollableHeight <= 0)
                    throw new InvalidOperationException("Long rankings must scroll without dropping participants or growing indefinitely.");
                window.Update(null, "waiting");
                window.SavePreview(directory, $"overlay-empty-{language}-{theme}.png");
                if (window.scroll.ScrollableHeight > 0)
                    throw new InvalidOperationException("Empty-state guidance must fit in the discreet layout.");
                window.ToggleDiscreet();
                if (saved || window.preferences.OverlayDiscreet || window.preferences.OverlayAutoFit || window.preferences.OverlayCompact || window.frame.Background.Opacity != preferences.OverlayOpacity || window.Height != detailedHeight)
                    throw new InvalidOperationException("Detailed layout must remain available with existing preferences.");
                window.Close();
            }
        File.WriteAllText(Path.Combine(directory, "discreet-result.txt"), "PASS: discreet/detailed layout, migration, retained row identity, no clipped rows, hover contrast, visible copy feedback and provenance in FR/EN/ES and all themes. No visible window, capture or system clipboard changes.");
    }
}
