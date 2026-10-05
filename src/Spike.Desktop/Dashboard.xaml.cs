using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using AionDPS.Aion2.Capture;
using AionDPS.Aion2.Protocol;
using AionDPS.Combat.Sources;
using Microsoft.Win32;
using Spike.Core;
using Spike.Engine;

namespace Spike.Desktop;

public partial class Dashboard : Window
{
    private Preferences preferences;
    private readonly EncounterStore store;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly SemaphoreSlim storageGate = new(1);
    private Task pendingSave = Task.CompletedTask;
    private LiveMeter? meter;
    private Encounter? shown;
    private Encounter? lastLive;
    private bool viewingHistory, heals, refreshing, verifying;
    private int? selectedActor, target;
    private Guid? scopeFight;
    private string page = "live";
    private string? noticeKey;
    private DateTime lastCheckpoint;
    private IReadOnlyList<HistoryEntry> history = [];
    private OverlayWindow? overlay;
    private HwndSource? hwndSource;
    private CultureInfo Culture => CultureInfo.GetCultureInfo(preferences.Language);
    private string T(string key) => Text.Get(key, preferences.Language);
    private string N(double value) => value.ToString("N0", Culture);
    private static string Duration(long milliseconds) => milliseconds < 1000 ? "< 1 s" : TimeSpan.FromMilliseconds(milliseconds).ToString(milliseconds >= 3_600_000 ? @"hh\:mm\:ss" : @"mm\:ss");

    public Dashboard(bool verification = false)
    {
        verifying = verification;
        preferences = verification ? new Preferences() : Preferences.Load();
        store = new(System.IO.Path.Combine(verification ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spike") : ApplicationData.Root, "fights"));
        InitializeComponent();
        LicensesText.Text = string.Join("\n\n", new[] { "Licenses/Engine.txt", "Licenses/Spike.txt", "Licenses/ThirdParty.txt", "GameArt/CREDITS.txt", "Fonts/Barlow-OFL.txt", "Fonts/BarlowCondensed-OFL.txt", "Fonts/Geist-OFL.txt" }.Select(path =>
        {
            using var stream = Application.GetResourceStream(new Uri($"pack://application:,,,/{path}"))!.Stream;
            using var reader = new StreamReader(stream); return reader.ReadToEnd();
        }));
        ApplyTheme(); Translate(); RenderFight();
        timer.Tick += (_, _) => Tick();
        Loaded += (_, _) => { if (!verifying) StartSession(); };
        SourceInitialized += (_, _) =>
        {
            if (verifying) return;
            hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource?.AddHook(Hotkey);
            if (!OverlayWindow.RegisterHotKey(hwndSource!.Handle, 73, 0x4003, 0x4D))
                SetNotice("overlayShortcutUnavailable");
        };
        Closing += (_, _) =>
        {
            StopUpdates();
            timer.Stop();
            overlay?.Close();
            if (hwndSource is not null) { OverlayWindow.UnregisterHotKey(hwndSource.Handle, 73); hwndSource.RemoveHook(Hotkey); }
            meter?.Dispose(); meter = null;
            // Dispose drains received events and queues the final completed fight.
            // Wait for that write, rather than overwriting it with an earlier snapshot.
            pendingSave.GetAwaiter().GetResult();
        };
    }

    private IntPtr Hotkey(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0312 && wParam.ToInt32() == 73)
        {
            if (SetupPanel.Visibility != Visibility.Visible) ToggleOverlay(this, new RoutedEventArgs());
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void StartSession()
    {
        if (!verifying)
        {
            timer.Start();
            if (!NpcapAvailability.Detect().IsInstalled) { ShowSetup(); return; }
        }
        if (preferences.ShowOverlayOnStartup) OpenOverlay();
        if (verifying) return;
        if (preferences.AutoStart) StartCapture();
    }

    private void ApplyTheme()
    {
        var p = Themes.Get(preferences.Theme);
        foreach (var (key, value) in new[] { ("Background", p.Background), ("Surface", p.Surface), ("Foreground", p.Foreground), ("Muted", p.Muted), ("Accent", p.Accent), ("Border", p.Border), ("Sidebar", p.Sidebar), ("Hover", p.Hover), ("Brand", p.Brand) })
            Resources[key] = Themes.Brush(value);
        Topmost = !verifying && preferences.AlwaysOnTop;
        Aion2SkillNames.Language = preferences.Language;
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(preferences.Language);
        overlay?.Apply(preferences);
    }

    private void Translate()
    {
        Title = Text.ProductName;
        TranslateUpdate();
        TranslateProgress();
        LiveNav.Content = T("live"); HistoryNav.Content = T("history"); SettingsNav.Content = T("settings");
        SidebarFoot.Text = T("localHistory"); PageTitle.Text = T(page);
        StartButton.Content = T(meter is null ? "start" : "stop"); PauseButton.Content = T(meter?.Paused == true ? "resume" : "pause");
        FinishButton.Content = T("finish"); UpdateOverlayButton(); ExportButton.Content = T("export");
        CopyButton.Content = T("copy"); CopyButton.ToolTip = T("copyHint");
        EmptyTitle.Text = T("noFight"); EmptyHint.Text = T("liveHint");
        DamageButton.Content = T("damage"); HealingButton.Content = T("heals");
        DurationCaption.Text = T("duration"); TimelineCaption.Text = T("timeline"); ImportButton.Content = T("import");
        HistorySearch.ToolTip = T("search"); System.Windows.Automation.AutomationProperties.SetName(HistorySearch, T("search"));
        OpenHistoryButton.Content = T("fightDetails"); HistoryHint.Text = T("historyHint");
        HistoryBossOnly.Content = T("bossOnlyHistory"); SpellSearch.ToolTip = T("skillSearch");
        SpellPlaceholder.Text = T("skillSearch");
        System.Windows.Automation.AutomationProperties.SetName(SpellSearch, T("skillSearch"));
        TimelinePlayer.Content = T("selectedPlayer");
        TimelineExpander.Header = T("timeline"); HistoryPlaceholder.Text = T("search");
        LanguageCaption.Text = T("language"); ThemeCaption.Text = T("theme"); NameCaption.Text = T("playerName"); NameHint.Text = T("nameHint");
        CharacterName.Text = preferences.PlayerName; AutoStartCheck.Content = T("autoStart"); AutoStartCheck.IsChecked = preferences.AutoStart;
        OnTopCheck.Content = T("top"); OnTopCheck.IsChecked = preferences.AlwaysOnTop; SaveSettingsButton.Content = T("apply");
        OverlayStartupCheck.Content = T("overlayStartup"); OverlayStartupCheck.IsChecked = preferences.ShowOverlayOnStartup;
        RecoverOverlayButton.Content = T("recoverOverlay");
        OverlayHint.Text = T("overlayHint"); DiagnosticsExpander.Header = T("diagnostics"); LicenseNote.Text = T("licenseNote"); RulesNote.Text = T("rulesNote");
        LicensesExpander.Header = "Licences / Licenses / Licencias";
        LanguageChoices.Children.Clear();
        string[] languageNames = ["Français", "English", "Español"];
        for (var i = 0; i < Text.Languages.Length; i++)
        {
            var id = Text.Languages[i];
            LanguageChoices.Children.Add(Choice(languageNames[i], preferences.Language == id, () => Change(preferences with { Language = id })));
        }
        ThemeChoices.Children.Clear();
        foreach (var id in Themes.Ids) ThemeChoices.Children.Add(Choice(T(id), preferences.Theme == id, () => Change(preferences with { Theme = id })));
        Notice.Text = noticeKey is null ? T("captureNote") : T(noticeKey);
    }

    private Button Choice(string text, bool selected, Action action)
    {
        var button = new Button { Content = text };
        if (selected) { button.SetResourceReference(Control.BackgroundProperty, "Accent"); button.SetResourceReference(Control.ForegroundProperty, "Background"); }
        button.Click += (_, _) => action();
        return button;
    }

    private void Change(Preferences value)
    {
        preferences = value;
        if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
        ApplyTheme(); Translate(); RenderFight(); FilterHistory();
    }

    private void StartCapture()
    {
        if (verifying || meter is not null) return;
        if (!NpcapAvailability.Detect().IsInstalled) { ShowSetup(startCapture: true); return; }
        try
        {
            meter = new(preferences.PlayerName);
            meter.Completed += encounter => { lastLive = encounter; pendingSave = SaveFight(encounter); };
            meter.Start();
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or DllNotFoundException or System.TypeInitializationException)
        { meter?.Dispose(); meter = null; SetNotice("captureError"); }
        Translate(); Tick();
    }

    private void Tick()
    {
        if (meter is not null)
        {
            meter.Poll();
            var snapshot = meter.Snapshot();
            if (snapshot is not null) lastLive = snapshot;
            if (snapshot is not null && (DateTime.Now - lastCheckpoint).TotalSeconds >= 10)
            { lastCheckpoint = DateTime.Now; pendingSave = SaveFight(snapshot); }
            DiagnosticsText.Text = $"{N(meter.Packets)} {T("packets")}\n{N(meter.DecodedEvents)} {T("events")}\n{N(meter.Errors)} {T("errors")}";
        }
        var state = meter is null ? "stopped" : meter.State == SourceState.Error ? "captureError" : meter.Paused ? "paused" : meter.HasCombat ? "capturing" : meter.State == SourceState.Connected ? "connected" : "waiting";
        StatusLabel.Text = T(state);
        if (!viewingHistory) shown = lastLive;
        if (page == "live" && !viewingHistory) RenderFight();
        overlay?.Update(lastLive, state);
    }

    private async Task SaveFight(Encounter encounter)
    {
        if (verifying) return;
        await storageGate.WaitAsync().ConfigureAwait(false);
        try { await Task.Run(() => store.Save(encounter)).ConfigureAwait(false); }
        catch (Exception error) when (IsFileError(error)) { _ = Dispatcher.BeginInvoke(() => SetNotice("saveError")); }
        finally { storageGate.Release(); }
    }

    private void RenderFight()
    {
        if (!IsInitialized) return;
        refreshing = true;
        PauseButton.IsEnabled = meter is not null; FinishButton.IsEnabled = meter?.CanFinish == true;
        ExportButton.IsEnabled = shown is not null;
        CopyButton.IsEnabled = shown is not null;
        CompareButton.IsEnabled = shown is not null;
        CaptureControls.Visibility = viewingHistory ? Visibility.Collapsed : Visibility.Visible;
        EmptyPanel.Visibility = shown is null ? Visibility.Visible : Visibility.Collapsed;
        CombatPanel.Visibility = shown is null ? Visibility.Collapsed : Visibility.Visible;
        if (shown is null) { refreshing = false; return; }
        if (scopeFight != shown.Id) { scopeFight = shown.Id; target = heals ? null : EncounterMath.PrimaryBoss(shown)?.Id; }
        if (heals) target = null;
        if (page == "live") PageTitle.Text = T(viewingHistory ? "analysis" : "live");
        FightTitle.Text = shown.Title == "—" ? T("local") : shown.Title;
        OriginLabel.Text = shown.Origin == "demo" ? T("demoLabel") : shown.Origin.Contains("unverified") ? T("importLabel") : viewingHistory ? T("localHistory") : T("live");
        var damagedTargets = shown.Events.Where(hit => !hit.Heal).Select(hit => hit.Target).ToHashSet();
        var targets = new[] { new TargetChoice(null, T("allTargets")) }.Concat(shown.Participants.Where(actor => !actor.IsPlayer && !actor.IsUnidentifiedSource && damagedTargets.Contains(actor.Id)).Select(actor => new TargetChoice(actor.Id, actor.Name))).ToArray();
        if (!targets.Any(choice => choice.Id == target)) target = null;
        TargetBox.ItemsSource = targets; TargetBox.SelectedItem = targets.First(choice => choice.Id == target);
        TargetBox.IsEnabled = !heals;
        var rows = EncounterMath.Players(shown, heals, target);
        var total = rows.Sum(row => row.Total);
        DpsCaption.Text = T(heals ? "groupHps" : "groupDps"); TotalCaption.Text = T(heals ? "heals" : "damage");
        DpsValue.Text = N(total / EncounterMath.Seconds(shown, target)); TotalValue.Text = N(total); DurationValue.Text = Duration(EncounterMath.Window(shown, target).DurationMs);
        ScopeLabel.Text = $"{T("scopeNote")} : {targets.First(choice => choice.Id == target).Name} · {shown.StartedAt.ToLocalTime().ToString("g", Culture)}";
        DurationValue.ToolTip = T(target is null ? "durationAllHint" : "durationBossHint");
        DamageButton.Background = (Brush)Resources[heals ? "Surface" : "Accent"]; DamageButton.Foreground = (Brush)Resources[heals ? "Foreground" : "Background"];
        HealingButton.Background = (Brush)Resources[heals ? "Accent" : "Surface"]; HealingButton.Foreground = (Brush)Resources[heals ? "Background" : "Foreground"];
        var displays = rows.Select((row, index) => Display(row, index)).ToArray();
        PlayerList.ItemsSource = displays;
        if (!displays.Any(row => row.Id == selectedActor)) selectedActor = displays.FirstOrDefault()?.Id;
        PlayerList.SelectedItem = displays.FirstOrDefault(row => row.Id == selectedActor);
        refreshing = false;
        RenderDetails(); DrawTimeline();
    }

    private PlayerDisplay Display(MeterRow row, int index)
    {
        var unresolved = shown?.Participants.First(p => p.Id == row.Id).IsUnidentifiedSource == true;
        var className = unresolved ? "" : row.ClassName;
        return new(row.Id, unresolved ? "—" : (index + 1).ToString("00"), unresolved ? $"{T("sourceLabel")} #{row.Id}" : row.Name,
            unresolved ? T("unidentifiedSource") : string.IsNullOrEmpty(row.ClassName) ? T("unknown") : T(row.ClassName), N(row.PerSecond), N(row.Total),
            row.Share.ToString("N1", Culture) + " %", new GridLength(Math.Max(0.01, row.MaximumShare), GridUnitType.Star),
            new GridLength(Math.Max(.01, 100 - row.MaximumShare), GridUnitType.Star), ClassColor(className), CombatPresentation.Emblem(className, 24));
    }

    internal static Brush ClassColor(string name) => GameArtwork.ClassColor(name);

    private void RenderDetails()
    {
        if (shown is null || selectedActor is null) { SpellList.ItemsSource = null; DetailsTitle.Text = T("selectPlayer"); DetailsSummary.Text = ""; return; }
        var person = shown.Participants.First(actor => actor.Id == selectedActor);
        DetailsTitle.Text = $"{(person.IsUnidentifiedSource ? $"{T("sourceLabel")} #{person.Id}" : person.Name)}  /  {T("skills")}";
        var events = shown.Events.Where(e => e.Source == selectedActor && e.Heal == heals && (target is null || e.Target == target)).ToArray();
        var hits = events.Count(e => !e.Tick);
        var critical = hits == 0 ? 0 : events.Count(e => e.Critical && !e.Tick) * 100d / hits;
        DetailsSummary.Text = $"{CombatPresentation.Short(events.Sum(e => e.Amount), preferences.Language)} {T(heals ? "heals" : "damage")} · {N(hits)} {T("hits")} · {critical.ToString("N1", Culture)} % {T("criticalShort")}\n"
            + $"{T("biggestHit")} : {N(events.Length == 0 ? 0 : events.Max(e => e.Amount))}     ·     {T("averageHit")} : {N(events.Length == 0 ? 0 : events.Average(e => e.Amount))}";
        SpellList.ItemsSource = EncounterMath.Spells(shown, selectedActor.Value, heals, target).Where(row => Aion2SkillNames.Display(row.Name).Contains(SpellSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)).Select(row => new
        {
            Name = Aion2SkillNames.Display(row.Name),
            Total = N(row.Total),
            Rate = N(row.PerSecond) + (heals ? " HPS" : " DPS"),
            Share = row.Share.ToString("N1", Culture) + " %",
            Icon = GameArtwork.SkillIcon(shown.Origin == "demo" ? 0 : row.Id, Aion2SkillNames.Display(row.Name), 32),
            Bar = new GridLength(row.Share, GridUnitType.Star),
            Rest = new GridLength(Math.Max(.01, 100 - row.Share), GridUnitType.Star),
            Extra = $"{N(row.Hits)} {T("hits")} · {N(row.Ticks)} {T("ticks")} · {row.CriticalRate.ToString("N1", Culture)} % {T("critical")}"
        }).ToArray();
        if (person.IsPlayer) DetailsSummary.Text += $"\n{T("observedDeaths")} : {Deaths(person.ObservedDeaths)}";
        MetricNote.Text = T(person.IsUnidentifiedSource ? "sourceHint" : heals ? "rawHealing" : "critNote");
    }

    private void DrawTimeline()
    {
        Timeline.Children.Clear();
        if (shown is null || Timeline.ActualWidth < 1) return;
        var values = EncounterMath.Timeline(shown, heals, target, 60, TimelinePlayer.IsChecked == true ? selectedActor : null);
        var maximum = Math.Max(1, values.Max());
        var line = new Polyline { Stroke = (Brush)Resources["Accent"], StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
        for (var i = 0; i < values.Length; i++) line.Points.Add(new Point(i * Timeline.ActualWidth / (values.Length - 1), 70 - values[i] / maximum * 60));
        for (var i = 1; i <= 3; i++) Timeline.Children.Add(new Line { X1 = 0, X2 = Timeline.ActualWidth, Y1 = i * 22, Y2 = i * 22, Stroke = (Brush)Resources["Border"], StrokeThickness = 0.5 });
        Timeline.Children.Add(line);
        Timeline.ToolTip = $"{T("peakBucket")} : {N(maximum)} {(heals ? "HPS" : "DPS")} · {Duration(EncounterMath.Window(shown, target).DurationMs)}";
    }

    private void SwitchPage(string value)
    {
        page = value; PageTitle.Text = T(value);
        FightPage.Visibility = value == "live" ? Visibility.Visible : Visibility.Collapsed;
        HistoryPage.Visibility = value == "history" ? Visibility.Visible : Visibility.Collapsed;
        ProgressPage.Visibility = value == "progress" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = value == "settings" ? Visibility.Visible : Visibility.Collapsed;
        foreach (var (button, name) in new[] { (LiveNav, "live"), (HistoryNav, "history"), (ProgressNav, "progress"), (SettingsNav, "settings") })
        {
            if (name == value) button.SetResourceReference(BackgroundProperty, "Surface");
            else button.Background = Brushes.Transparent;
        }
    }
    private void ShowLive(object sender, RoutedEventArgs e) { viewingHistory = false; shown = lastLive; SwitchPage("live"); Tick(); }
    private async void ShowHistory(object sender, RoutedEventArgs e)
    {
        SwitchPage("history");
        try { history = await Task.Run(store.List); FilterHistory(); if (history.Count == 0) SetNotice("emptyHistory"); }
        catch (Exception error) when (IsFileError(error)) { SetNotice("loadError"); }
    }
    private void ShowSettings(object sender, RoutedEventArgs e) { SwitchPage("settings"); Translate(); }
    private void StartStop(object sender, RoutedEventArgs e)
    {
        if (meter is null) StartCapture(); else { meter.Dispose(); meter = null; Translate(); Tick(); }
    }
    private void Pause(object sender, RoutedEventArgs e) { meter?.TogglePause(); Translate(); Tick(); }
    private void Finish(object sender, RoutedEventArgs e) { meter?.Finish(); Tick(); }
    private void ShowDamage(object sender, RoutedEventArgs e) { heals = false; target = shown is null ? null : EncounterMath.PrimaryBoss(shown)?.Id; RenderFight(); }
    private void ShowHealing(object sender, RoutedEventArgs e) { heals = true; target = null; RenderFight(); }
    private void PlayerSelected(object sender, SelectionChangedEventArgs e) { if (refreshing) return; selectedActor = (PlayerList.SelectedItem as PlayerDisplay)?.Id; RenderDetails(); DrawTimeline(); }
    private void SearchSkills(object sender, TextChangedEventArgs e)
    {
        if (SpellPlaceholder is not null) SpellPlaceholder.Visibility = string.IsNullOrEmpty(SpellSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
        if (IsInitialized) RenderDetails();
    }
    private void TimelineModeChanged(object sender, RoutedEventArgs e) { if (IsInitialized) DrawTimeline(); }
    private void FilterBossHistory(object sender, RoutedEventArgs e) => FilterHistory();
    private void TargetChanged(object sender, SelectionChangedEventArgs e) { if (refreshing) return; target = (TargetBox.SelectedItem as TargetChoice)?.Id; RenderFight(); }
    private void TimelineResized(object sender, SizeChangedEventArgs e) => DrawTimeline();
    private void SaveSettings(object sender, RoutedEventArgs e) { Change(preferences with { PlayerName = CharacterName.Text.Trim(), AutoStart = AutoStartCheck.IsChecked == true, AlwaysOnTop = OnTopCheck.IsChecked == true, ShowOverlayOnStartup = OverlayStartupCheck.IsChecked == true }); SetNotice("restartHint"); }
    private void SearchHistory(object sender, TextChangedEventArgs e)
    {
        if (HistoryPlaceholder is not null) HistoryPlaceholder.Visibility = string.IsNullOrEmpty(HistorySearch.Text) ? Visibility.Visible : Visibility.Collapsed;
        FilterHistory();
    }
    private void FilterHistory()
    {
        if (HistoryList is null) return;
        var search = HistorySearch.Text.Trim();
        var matches = history.Where(item => (item.Title + " " + item.SearchPlayers).Contains(search, StringComparison.OrdinalIgnoreCase) && (HistoryBossOnly.IsChecked != true || item.Boss)).ToArray();
        HistoryCount.Text = $"{matches.Length} {T("fightCount")} · {T("localHistory")}";
        HistoryHint.Text = matches.Length == 0 ? T(history.Count == 0 ? "emptyHistory" : "historyNoMatch") : T("historyHint");
        HistoryList.ItemsSource = matches
            .Select(item => new HistoryDisplay(item.Id, item.Title == "—" ? T("local") : item.Title,
                item.StartedAt.ToLocalTime().ToString("g", Culture), N(item.Damage) + " " + T("damage"), Duration(item.DurationMs),
                $"{item.Players} {T("players")} · {N(item.Damage / Math.Max(1, item.DurationMs / 1000d))} DPS · {T(item.Boss ? "scopeBoss" : "allTargets")}")).ToArray();
    }
    private async void OpenHistory(object sender, MouseButtonEventArgs e) => await LoadSelectedHistory();
    private async void OpenHistoryReport(object sender, RoutedEventArgs e) => await LoadSelectedHistory();
    private void HistorySelected(object sender, SelectionChangedEventArgs e) => CompareHistoryButton.IsEnabled = OpenHistoryButton.IsEnabled = HistoryList.SelectedItem is HistoryDisplay;
    private async void HistoryKey(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await LoadSelectedHistory(); }
    private async Task LoadSelectedHistory()
    {
        if (HistoryList.SelectedItem is not HistoryDisplay entry) return;
        try { DisplayEncounter(await Task.Run(() => store.Load(entry.Id)), null, false); }
        catch (Exception error) when (IsFileError(error)) { SetNotice("loadError"); }
    }
    private async void Import(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = T("fightFileFilter") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var path = dialog.FileName;
            var encounter = await Task.Run(() => { if (new FileInfo(path).Length > EncounterFile.MaxBytes) throw new InvalidDataException(); return EncounterFile.Read(File.ReadAllText(path)); });
            shown = encounter with { Id = Guid.NewGuid(), Origin = encounter.Origin == "demo" ? "demo" : "import-unverified" };
            pendingSave = SaveFight(shown); await pendingSave; viewingHistory = true; selectedActor = target = null; SwitchPage("live"); RenderFight();
        }
        catch (Exception error) when (IsFileError(error)) { SetNotice("loadError"); }
    }
    private void CopyFight(object sender, RoutedEventArgs e) => CopySummary(Clipboard.SetText);
    private void CopySummary(Action<string> write)
    {
        if (shown is null) return;
        SetNotice(FightSummary.Copy(FightSummary.Format(shown, heals, target, preferences.Language), write));
    }

    private async void Export(object sender, RoutedEventArgs e)
    {
        if (shown is null) return;
        var dialog = new SaveFileDialog { Filter = T("fightFileFilter"), FileName = $"combat-{shown.StartedAt:yyyyMMdd-HHmm}.json" };
        if (dialog.ShowDialog(this) != true) return;
        try { await File.WriteAllTextAsync(dialog.FileName, JsonSerializer.Serialize(EncounterFile.PrivateExport(shown), EncounterFile.Json)); SetNotice("exported"); }
        catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
    }
    private void ToggleOverlay(object sender, RoutedEventArgs e)
    {
        if (overlay is not null) { overlay.Close(); return; }
        OpenOverlay();
    }
    private void RecoverOverlay(object sender, RoutedEventArgs e) { OpenOverlay(); overlay!.RecoverPosition(this); }

    private void UpdateOverlayButton()
    {
        OverlayButton.Content = T(overlay is null ? "showOverlay" : "hideOverlay");
        OverlayButton.ToolTip = "Ctrl+Alt+M";
    }

    private void OpenOverlay()
    {
        if (overlay is not null) return;
        overlay = new(preferences, (Style)FindResource(typeof(Button)), store);
        overlay.LayoutSaved += (left, top, width, height) =>
        {
            if (verifying) return;
            preferences = preferences with { OverlayLeft = left, OverlayTop = top, OverlayWidth = width, OverlayHeight = height };
            try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
        };
        overlay.AppearanceChanged += (autoFit, compact, opacity) =>
        {
            preferences = preferences with { OverlayAutoFit = autoFit, OverlayCompact = compact, OverlayOpacity = opacity };
            if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
        };
        overlay.NewFightRequested += () => { meter?.Finish(); Tick(); };
        overlay.DiscreetChanged += enabled =>
        {
            preferences = preferences with { OverlayDiscreet = enabled };
            if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
        };
        overlay.SnappingChanged += enabled =>
        {
            preferences = preferences with { OverlaySnapToEdges = enabled };
            if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
        };
        overlay.IdleFadeChanged += enabled =>
        {
            preferences = preferences with { OverlayFadeWhenIdle = enabled };
            if (!verifying) try { preferences.Save(); } catch (Exception error) when (IsFileError(error)) { SetNotice("saveError"); }
        };
        overlay.DetailsRequested += (fight, player, healing, selectedTarget) =>
        {
            DisplayEncounter(fight, player, healing, selectedTarget, false);
            if (verifying) return;
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        };
        overlay.Closed += (_, _) => { overlay = null; UpdateOverlayButton(); };
        if (!verifying) overlay.Show();
        overlay.Update(lastLive); UpdateOverlayButton();
    }
    private void DisplayEncounter(Encounter fight, int? player, bool healing, int? selectedTarget = null, bool defaultTarget = true)
    {
        shown = fight; viewingHistory = true; selectedActor = player; heals = healing; scopeFight = fight.Id;
        target = healing ? null : defaultTarget ? EncounterMath.PrimaryBoss(fight)?.Id : selectedTarget;
        SwitchPage("live"); RenderFight();
    }
    private void SetNotice(string key) { noticeKey = key; Notice.Text = T(key); }
    private static bool IsFileError(Exception error) => error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException;


}

public sealed record TargetChoice(int? Id, string Name);
public sealed record PlayerDisplay(int Id, string Rank, string Name, string ClassLabel, string RateLabel, string TotalLabel,
    string ShareLabel, GridLength BarWidth, GridLength RestWidth, Brush Color, FrameworkElement Emblem);
public sealed record HistoryDisplay(Guid Id, string Title, string When, string Damage, string Duration, string Summary);
