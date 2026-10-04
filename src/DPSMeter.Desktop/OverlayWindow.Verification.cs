using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DPSMeter.Core;

namespace DPSMeter.Desktop;

public sealed partial class OverlayWindow
{
    private void VerifyIdleFade(Encounter encounter, string directory)
    {
        var checks = 0;
        void Check(double expected, string reason)
        {
            checks++;
            if (Math.Abs(frame.Opacity - expected) > .001) throw new InvalidOperationException("Idle opacity: " + reason);
        }
        Check(IdleOpacity, "empty startup");
        Update(encounter, "capturing"); Check(1, "active fight");
        SavePreview(directory, "overlay-active.png");
        Update(encounter, "connected"); Check(IdleOpacity, "last fight must not prevent fading");
        SavePreview(directory, "overlay-idle.png");
        RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent }); Check(1, "hover restores readability");
        RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseLeaveEvent }); Check(IdleOpacity, "leave restores idle opacity");
        var menu = BuildOptionsMenu();
        menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent)); Check(1, "options remain readable outside window");
        menu.RaiseEvent(new RoutedEventArgs(ContextMenu.ClosedEvent)); Check(IdleOpacity, "closing menu restores idle opacity");
        grip.RaiseEvent(new DragStartedEventArgs(0, 0)); Check(1, "resize keeps window readable");
        grip.RaiseEvent(new DragCompletedEventArgs(0, 0, true)); Check(IdleOpacity, "cancelled resize releases visibility");
        SetManipulating(true); Check(1, "moving keeps window readable");
        SetManipulating(false); Check(IdleOpacity, "end of move releases visibility");
        SelectFight(encounter); Check(1, "archived report remains readable");
        Update(encounter, "waiting"); Check(1, "live updates preserve archived report visibility");
        SelectFight(null); Check(IdleOpacity, "return to idle live view");
        locked = true;
        RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseEnterEvent }); Check(IdleOpacity, "locked overlay ignores hover");
        Update(encounter, "capturing"); Check(1, "locked overlay returns for combat");
        RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = MouseLeaveEvent });
        locked = false;
        foreach (var state in new[] { "paused", "stopped", "captureError" }) { Update(encounter, state); Check(IdleOpacity, state); }
        var saved = true;
        void PreferenceChanged(bool enabled) => saved = enabled;
        IdleFadeChanged += PreferenceChanged;
        menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, T("fadeWhenIdle"))).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Check(1, "disabled preference");
        if (preferences.OverlayFadeWhenIdle || saved) throw new InvalidOperationException("Idle preference did not propagate.");
        var restored = JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(preferences))!;
        if (restored.OverlayFadeWhenIdle) throw new InvalidOperationException("Idle preference did not round-trip.");
        ToggleIdleFade(); Check(IdleOpacity, "enabled preference");
        if (!saved) throw new InvalidOperationException("Idle preference could not be restored.");
        IdleFadeChanged -= PreferenceChanged;
        ChangeAppearance(true, false, .75); Check(IdleOpacity, "background setting remains independent");
        Update(encounter, "capturing"); Check(1, "combat restores full text opacity");
        if (frame.Background.Opacity != .75) throw new InvalidOperationException("Combat changed background preference.");
        File.WriteAllText(Path.Combine(directory, "idle-fade-result.txt"), $"PASS: {checks} visibility checks; preference propagation and JSON round-trip. Routed mouse/menu/resize events tested offscreen; no desktop input or game interaction.");
    }

    internal static void VerifyPreview(Preferences preferences, Encounter encounter, string directory, Style buttonStyle)
    {
        var window = new OverlayWindow(preferences with { OverlayWidth = 460 }, buttonStyle);
        window.CopySummary(_ => throw new InvalidOperationException("Empty overlay must not copy."));
        if (window.copy.IsEnabled) throw new InvalidOperationException("Empty overlay enables copy.");
        var withSource = encounter with
        {
            Participants = encounter.Participants.Append(new Participant(int.MaxValue, "Synthetic source", "Sorcerer", false, IsUnidentifiedSource: true)).ToArray(),
            Events = encounter.Events.Append(new CombatEvent(0, int.MaxValue, EncounterMath.PrimaryBoss(encounter)!.Id, 15390012, "Fire Wall", 5000, false, false, false)).ToArray()
        };
        window.Update(withSource, "capturing");
        if (!window.rows.Children.Contains(window.unresolvedToggle) || window.entries.ContainsKey("p" + int.MaxValue))
            throw new InvalidOperationException("Unidentified sources must be collapsed separately from players.");
        var beforeExpansion = window.total.Text;
        window.unresolvedToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (!window.entries.ContainsKey("p" + int.MaxValue) || window.total.Text != beforeExpansion)
            throw new InvalidOperationException("Source expansion changed totals or failed to show details.");
        window.unresolvedToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Update(null, "waiting");
        window.VerifyIdleFade(encounter, directory);
        window.Update(encounter, "capturing");
        var snapSaved = true; window.SnappingChanged += enabled => snapSaved = enabled;
        var snapOption = window.BuildOptionsMenu().Items.OfType<MenuItem>().Single(item => Equals(item.Header, window.T("snapEdges")));
        snapOption.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        if (window.preferences.OverlaySnapToEdges || snapSaved) throw new InvalidOperationException("Snapping preference was not propagated.");
        window.ToggleSnapping();
        if (!window.preferences.OverlaySnapToEdges || !snapSaved) throw new InvalidOperationException("Snapping could not be restored.");
        window.SavePreview(directory, "mini-meter.png");
        if (window.scroll.ScrollableHeight > 1) throw new InvalidOperationException("Automatic height clips party rows.");
        var first = window.rows.Children.OfType<CombatantRow>().First(); window.Update(encounter);
        if (!ReferenceEquals(first, window.rows.Children[0])) throw new InvalidOperationException("Live refresh replaced hover target.");
        if (first.HoverContent?.Invoke() is not Border { Child: StackPanel }) throw new InvalidOperationException("Player hover details missing.");
        window.scope.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (window.Target is not null) throw new InvalidOperationException("All-target scope failed.");
        window.scope.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        first.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (window.actor is null || window.entries.Count == 0) throw new InvalidOperationException("Skill drilldown failed.");
        window.SavePreview(directory, "overlay-skills.png");
        var reportOpened = false;
        window.DetailsRequested += (fight, player, healing, target) => reportOpened = fight.Id == encounter.Id && player == window.actor && !healing && target == EncounterMath.PrimaryBoss(encounter)?.Id;
        window.report.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (!reportOpened) throw new InvalidOperationException("Full report lost its scope.");
        window.SelectFight(encounter); window.Update(encounter with { Id = Guid.NewGuid(), Events = [] });
        if (window.Selected?.Id != encounter.Id || window.entries.Count == 0) throw new InvalidOperationException("Live update replaced history.");
        string? copied = null;
        window.CopySummary(value => copied = value);
        if (copied != FightSummary.Format(encounter, false, window.Target, preferences.Language) || window.hint.Text != window.T("copied"))
            throw new InvalidOperationException("Overlay copy lost the archive or scope.");
        window.CopySummary(_ => throw new System.Runtime.InteropServices.COMException("Clipboard busy"));
        if (window.hint.Text != window.T("copyError")) throw new InvalidOperationException("Overlay copy failure not shown.");
        window.healing.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (!window.heals || window.Target is not null || window.rateHead.Text != "HPS / %") throw new InvalidOperationException("Healing incorrectly filtered or labelled.");
        window.CopySummary(value => copied = value);
        if (copied != FightSummary.Format(encounter, true, null, preferences.Language)) throw new InvalidOperationException("Overlay copy lost healing mode.");
        window.copyNotice = null;
        window.SelectFight(null); if (window.Selected?.Id == encounter.Id) throw new InvalidOperationException("Return to live failed.");
        window.SetMetric(false); window.Update(encounter);
        var fullHeight = window.Height;
        window.ChangeAppearance(true, true, .75); window.SavePreview(directory, "overlay-compact.png");
        if (window.Height >= fullHeight || window.scroll.ScrollableHeight > 1 || window.Opacity != 1) throw new InvalidOperationException("Compact layout or text opacity failed.");
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                window.Apply(preferences with { Language = language, Theme = theme }); window.SetMetric(false); window.Update(encounter);
                window.CopySummary(value => copied = value);
                if (copied != FightSummary.Format(encounter, false, window.Target, language) || !copied.Contains(Text.Get("groupDps", language)))
                    throw new InvalidOperationException("Overlay copy must follow interface language changes.");
                window.copyNotice = null; window.Render();
                window.SavePreview(directory, $"overlay-{language}-{theme}.png");
                window.Width = window.MinWidth;
                foreach (var compact in new[] { false, true })
                {
                    window.ChangeAppearance(true, compact, .94);
                    window.SavePreview(directory, $"overlay-narrow-{language}-{theme}-{compact}.png");
                    if (window.scroll.ScrollableHeight > 1)
                        throw new InvalidOperationException("Minimum-width overlay clips party rows.");
                }
                window.Width = 460;
            }
        window.Close();
    }
    internal static void RenderPreview(Preferences preferences, Encounter encounter, string directory, Style buttonStyle)
    {
        var window = new OverlayWindow(preferences with { OverlayWidth = 460, OverlayAutoFit = true }, buttonStyle);
        window.Update(encounter); window.SelectFight(encounter); window.SavePreview(directory, "overlay-reel.png");
        var player = EncounterMath.Players(encounter, false, window.Target).FirstOrDefault();
        if (player is not null) SaveElement(CombatHoverCard.Create(encounter, player.Id, false, window.Target, preferences), 460, null, Path.Combine(directory, "survol-joueur.png"));
        window.Close();
    }
    private void SavePreview(string directory, string name) => SaveElement((FrameworkElement)Content, Width, Height, Path.Combine(directory, name));
    private static void SaveElement(FrameworkElement content, double width, double? height, string path)
    {
        content.Measure(new Size(width, height ?? double.PositiveInfinity)); var h = height ?? Math.Ceiling(content.DesiredSize.Height);
        content.Arrange(new Rect(0, 0, width, h)); content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)width, (int)h, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); png.Save(file);
    }
}
