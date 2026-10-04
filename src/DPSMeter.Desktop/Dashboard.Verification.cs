using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionDPS.Combat.Sources;
using DPSMeter.Core;
using DPSMeter.Engine;

namespace DPSMeter.Desktop;

public partial class Dashboard
{
    internal void VerifyViews(string directory)
    {
        Directory.CreateDirectory(directory);
        if (!Text.IsComplete) throw new InvalidOperationException("Missing translations.");
        VerifySharing(directory);
        VerifySetup(directory);
        var placementChecks = OverlayPlacement.Verify();
        GameArtwork.Verify();
        preferences = JsonSerializer.Deserialize<Preferences>("{\"Language\":\"fr\"}")!;
        if (!preferences.ShowOverlayOnStartup) throw new InvalidOperationException("Existing settings must enable the overlay by default.");
        if (!preferences.OverlaySnapToEdges) throw new InvalidOperationException("Existing settings must enable edge snapping by default.");
        if (!preferences.OverlayFadeWhenIdle) throw new InvalidOperationException("Existing settings must enable idle fading by default.");
        StartSession();
        if (overlay is null || (string)OverlayButton.Content != T("hideOverlay")) throw new InvalidOperationException("Startup overlay or hide label missing.");
        var firstOverlay = overlay; OpenOverlay();
        if (!ReferenceEquals(firstOverlay, overlay)) throw new InvalidOperationException("Duplicate overlay created.");
        OverlayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (overlay is not null || (string)OverlayButton.Content != T("showOverlay")) throw new InvalidOperationException("Hide overlay failed.");
        preferences = preferences with { ShowOverlayOnStartup = false }; StartSession();
        if (overlay is not null) throw new InvalidOperationException("Startup preference ignored.");
        SwitchPage("settings"); OverlayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (overlay is null) throw new InvalidOperationException("Overlay not accessible from settings.");
        RecoverOverlayButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (overlay is null || !double.IsFinite(overlay.Left)) throw new InvalidOperationException("Overlay recovery from settings failed.");
        overlay.Close();
        var demo = PreviewEncounter();
        shown = demo;
        RenderFight();
        PlayerList.SelectedIndex = 1;
        if (selectedActor != 2 || SpellList.Items.Count == 0) throw new InvalidOperationException("Player selection did not show skills.");
        var skillCount = SpellList.Items.Count;
        SpellSearch.Text = "no-skill-has-this-name";
        if (SpellList.Items.Count != 0) throw new InvalidOperationException("Skill search ignored its filter.");
        SpellSearch.Clear();
        if (SpellList.Items.Count != skillCount) throw new InvalidOperationException("Clearing skill search lost skills.");
        HealingButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (!heals || PlayerList.Items.Count != 0) throw new InvalidOperationException("Healing tab did not isolate healing.");
        DamageButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (heals || PlayerList.Items.Count != 4) throw new InvalidOperationException("Damage tab did not restore damage.");
        TargetBox.SelectedIndex = 1;
        if (target != 100) throw new InvalidOperationException("Target filter did not update.");
        SettingsNav.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (SettingsPage.Visibility != Visibility.Visible) throw new InvalidOperationException("Settings navigation failed.");
        DisplayEncounter(demo, 2, false);
        lastLive = demo with { Id = Guid.NewGuid(), Events = [] }; Tick();
        if (shown.Id != demo.Id || selectedActor != 2 || SpellList.Items.Count == 0) throw new InvalidOperationException("History report changed while capture updated.");
        history = [new(demo.Id, demo.StartedAt, demo.Title, demo.DurationMs, 100, "demo", 4, true, "TestPlayer"),
            new(Guid.NewGuid(), demo.StartedAt, "Other fight", 1000, 50, "demo", 1, false, "Someone")];
        HistorySearch.Text = "testplayer";
        if (HistoryList.Items.Count != 1) throw new InvalidOperationException("History player search failed.");
        HistorySearch.Clear(); HistoryBossOnly.IsChecked = true;
        if (HistoryList.Items.Count != 1) throw new InvalidOperationException("History boss filter failed.");
        HistoryBossOnly.IsChecked = false;
        if (HistoryList.Items.Count != 2) throw new InvalidOperationException("Clearing history filter lost fights.");
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
                foreach (var view in new[] { "live", "settings", "history" })
                {
                    preferences = new(language, theme, false, AutoStart: false); shown = lastLive = demo;
                    history = [new(demo.Id, demo.StartedAt, demo.Title, demo.DurationMs, demo.Events.Sum(hit => hit.Amount), "demo", 4, true)];
                    ApplyTheme(); Translate(); SwitchPage(view); FilterHistory(); RenderFight();
                    if (view == "history")
                    {
                        HistoryList.SelectedIndex = 0;
                        if (!OpenHistoryButton.IsEnabled || PageTitle.Text != T("history")) throw new InvalidOperationException("History report button or title failed.");
                    }
                    SaveDashboard(directory, $"{language}-{theme}-{view}.png", 1100, 780);
                }
        OverlayWindow.VerifyPreview(new Preferences(), demo, directory, (Style)FindResource(typeof(Button)));
        File.WriteAllText(System.IO.Path.Combine(directory, "placement-result.txt"), $"PASS: {placementChecks} placement checks, including a hidden native HWND; corners, taskbar work area, negative coordinates, scaled threshold, free movement, screen recovery and bottom anchoring. No window shown, no mouse input sent.");
        File.WriteAllText(System.IO.Path.Combine(directory, "result.txt"), "PASS: nine embedded class icons, atlas bounds/decoding, skill variants, separate buff namespace and unknown icon fallback; overlay startup, settings migration, opt-out, no duplicate window, show/hide; player selection, skill search, damage/healing, target filtering, archived report during live updates, history search by player and boss filter; stable overlay rows, hover content, scope and compact sizing; dashboard and overlay renders in all languages and themes, including both densities at minimum width. No capture or window shown.");
    }

    internal void RenderSaved(string path, string directory)
    {
        Directory.CreateDirectory(directory);
        shown = EncounterFile.Read(File.ReadAllText(path)); viewingHistory = true;
        ApplyTheme(); Translate(); SwitchPage("live"); RenderFight();
        StatusLabel.Text = T("saved");
        SaveDashboard(directory, "combat-reel.png", 1100, 780);
        SaveDashboard(directory, "combat-petite-fenetre.png", 884, 600);
        OverlayWindow.RenderPreview(preferences, shown, directory, (Style)FindResource(typeof(Button)));
        File.WriteAllText(System.IO.Path.Combine(directory, "metrics.json"), JsonSerializer.Serialize(new
        {
            boss = EncounterMath.PrimaryBoss(shown)?.Name,
            durationMs = EncounterMath.Window(shown, target).DurationMs,
            players = EncounterMath.Players(shown, false, target),
            skills = selectedActor is { } actor ? EncounterMath.Spells(shown, actor, false, target) : []
        }, new JsonSerializerOptions { WriteIndented = true }));
        history = store.List(); SwitchPage("history"); FilterHistory();
        SaveDashboard(directory, "historique.png", 1100, 780);
    }

    private void SaveDashboard(string directory, string name, int width, int height)
    {
        var content = (FrameworkElement)Content;
        content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout(); DrawTimeline(); content.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(System.IO.Path.Combine(directory, name)); png.Save(file);
    }

    internal static Encounter PreviewEncounter()
    {
        var source = Demo.Create();
        var players = source.Actors.Select((actor, i) => new Participant(i + 1, actor.Name, new[] { "Gladiator", "Sorcerer", "Ranger", "Cleric" }[i], true))
            .Append(new Participant(100, "Training guardian · preview", "", false, true)).ToArray();
        return new(2, Guid.NewGuid(), DateTimeOffset.Now, "Global", "preview", "demo", "", "preview", source.DurationMs,
            players, source.Hits.Select(hit => new CombatEvent(hit.OffsetMs, Array.FindIndex(source.Actors, actor => actor.Id == hit.ActorId) + 1, 100,
                11010000, hit.Skill, hit.Damage, false, hit.Critical, false)).ToArray());
    }

    internal async Task VerifyLive(int seconds, string directory)
    {
        Directory.CreateDirectory(directory);
        var localStore = new EncounterStore(System.IO.Path.Combine(directory, "fights"));
        using var capture = new LiveMeter(preferences.PlayerName);
        var completed = new List<Encounter>();
        capture.Completed += encounter => { localStore.Save(encounter); completed.Add(encounter); };
        capture.Start();
        var until = DateTime.UtcNow.AddSeconds(Math.Clamp(seconds, 5, 120));
        while (DateTime.UtcNow < until && capture.State != SourceState.Error)
        {
            await Task.Delay(500);
            capture.Poll();
        }
        var packets = capture.Packets;
        var decoded = capture.DecodedEvents;
        var errors = capture.Errors;
        capture.Dispose();
        var best = completed.OrderByDescending(item => item.Events.Length).FirstOrDefault();
        if (best is not null)
        {
            shown = localStore.Load(best.Id); lastLive = shown;
            if (shown.Events.Sum(hit => hit.Amount) != best.Events.Sum(hit => hit.Amount)) throw new InvalidDataException("History round-trip failed.");
            viewingHistory = true; SwitchPage("live"); ApplyTheme(); Translate(); RenderFight();
            StatusLabel.Text = T("saved");
            var content = (FrameworkElement)Content;
            content.Measure(new Size(1100, 1000)); content.Arrange(new Rect(0, 0, 1100, 1000)); content.UpdateLayout(); DrawTimeline(); content.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1100, 1000, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(System.IO.Path.Combine(directory, "real-combat.png")); png.Save(file);
        }
        File.WriteAllText(System.IO.Path.Combine(directory, "live-result.json"), JsonSerializer.Serialize(new
        {
            packets,
            decoded,
            errors,
            savedFights = completed.Count,
            roundTrip = best is not null,
            displayedPlayers = best is null ? 0 : EncounterMath.Players(best, false).Count,
            namedPlayers = best?.Participants.Count(actor => actor.IsPlayer && !actor.Name.StartsWith("Player #") && !actor.Name.StartsWith('#')) ?? 0,
            noWindowShown = true,
            rawPacketsSaved = false
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
