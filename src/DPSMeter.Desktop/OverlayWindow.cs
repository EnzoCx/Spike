using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AionDPS.Aion2.Protocol;
using DPSMeter.Core;

namespace DPSMeter.Desktop;

public sealed partial class OverlayWindow : Window
{
    private Preferences preferences;
    private readonly EncounterStore? store;
    private readonly StackPanel rows = new();
    private readonly Dictionary<string, CombatantRow> entries = new();
    private readonly Border frame;
    private readonly TextBlock heading = new() { FontSize = 16, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock duration = new() { FontSize = 16, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock health = new() { FontSize = 10 }, status = new() { FontSize = 10 };
    private readonly TextBlock hint = new() { FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis }, total = new() { FontSize = 16, FontWeight = FontWeights.SemiBold };
    private readonly ProgressBar hpBar = new() { Height = 3, Minimum = 0, Maximum = 100, BorderThickness = new Thickness(0) };
    private readonly TextBlock empty = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8), FontSize = 12 };
    private readonly TextBlock columns = new() { FontSize = 10, Margin = new Thickness(4, 0, 4, 4) };
    private readonly TextBlock rateHead = new() { FontSize = 10, Width = 120, HorizontalAlignment = HorizontalAlignment.Right, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 0, 6, 4) };
    private readonly ScrollViewer scroll;
    private readonly Button picker, damage, healing, scope, back, report, locking, close, options, unresolvedToggle;
    private readonly Thumb grip;
    private readonly OverlayPlacement placement;
    private Encounter? live, archived;
    private int? actor;
    private bool heals, locked, bossOnly = true, showUnidentified;
    private bool pointerInside, manipulating;
    private int openMenus;
    private double visibilityOpacity = double.NaN;
    private const double IdleOpacity = .15;
    private string captureStatus = "waiting";
    private Encounter? Selected => archived ?? live;
    private int? Target => bossOnly && !heals && Selected is { } fight ? EncounterMath.PrimaryBoss(fight)?.Id : null;
    private string T(string key) => Text.Get(key, preferences.Language);
    private CultureInfo Culture => CultureInfo.GetCultureInfo(preferences.Language);
    private string N(double value) => value.ToString("N0", Culture);
    public event Action<Encounter, int?, bool, int?>? DetailsRequested;
    public event Action<double, double, double, double>? LayoutSaved;
    public event Action<bool, bool, double>? AppearanceChanged;
    public event Action? NewFightRequested;
    public event Action<bool>? SnappingChanged;
    public event Action<bool>? IdleFadeChanged;

    public OverlayWindow(Preferences preferences, Style? buttonStyle = null, EncounterStore? store = null)
    {
        this.preferences = preferences; this.store = store;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CommonStyles.xaml") });
        Resources[typeof(ScrollBar)] = Resources["MeterScrollBar"];
        if (buttonStyle is not null) Resources[typeof(Button)] = buttonStyle;
        Title = "DPSMeter · Overlay"; MinWidth = 360; MinHeight = 180; MaxWidth = 800; MaxHeight = 1000;
        Width = Bounded(preferences.OverlayWidth, 460, MinWidth, MaxWidth);
        Height = Bounded(preferences.OverlayHeight, 460, MinHeight, MaxHeight);
        Left = double.IsFinite(preferences.OverlayLeft) ? preferences.OverlayLeft : 40;
        Top = double.IsFinite(preferences.OverlayTop) ? preferences.OverlayTop : 80;
        placement = new OverlayPlacement(this, () => this.preferences.OverlaySnapToEdges, SaveLayout);
        ResizeMode = ResizeMode.NoResize; WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        Topmost = true; ShowInTaskbar = false; ShowActivated = false;
        FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Barlow");
        var layout = new Grid();
        foreach (var height in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            layout.RowDefinitions.Add(new RowDefinition { Height = height });
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 4), Background = Brushes.Transparent };
        close = SmallButton("×", Close); DockPanel.SetDock(close, Dock.Right); header.Children.Add(close);
        locking = SmallButton("◇", Lock); DockPanel.SetDock(locking, Dock.Right); header.Children.Add(locking);
        options = SmallButton("···", ShowOptions); DockPanel.SetDock(options, Dock.Right); header.Children.Add(options);
        duration.Margin = new Thickness(8, 0, 8, 0); DockPanel.SetDock(duration, Dock.Right); header.Children.Add(duration);
        var brand = new DockPanel { Cursor = Cursors.SizeAll, Background = Brushes.Transparent };
        brand.Children.Add(new BrandMark { Width = 20, Height = 20, Margin = new Thickness(0, 0, 6, 0) }); brand.Children.Add(heading);
        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ChangedButton != MouseButton.Left || locked) return;
            if (e.ClickCount == 2) ChangeAppearance(preferences.OverlayAutoFit, !preferences.OverlayCompact, preferences.OverlayOpacity);
            else
            {
                SetManipulating(true);
                try { DragMove(); SaveLayout(); }
                finally { SetManipulating(false); }
            }
            e.Handled = true;
        };
        header.MouseRightButtonUp += (_, e) => { if (!locked) ShowOptions(); e.Handled = true; };
        header.Children.Add(brand); layout.Children.Add(header);
        var healthArea = new StackPanel { Margin = new Thickness(2, 0, 2, 4) };
        var healthLine = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };
        health.TextTrimming = TextTrimming.CharacterEllipsis;
        DockPanel.SetDock(status, Dock.Right); healthLine.Children.Add(status); healthLine.Children.Add(health);
        healthArea.Children.Add(healthLine); healthArea.Children.Add(hpBar); Grid.SetRow(healthArea, 1); layout.Children.Add(healthArea);
        var controls = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        healing = SmallButton("HPS", () => SetMetric(true)); DockPanel.SetDock(healing, Dock.Right); controls.Children.Add(healing);
        damage = SmallButton("DPS", () => SetMetric(false)); DockPanel.SetDock(damage, Dock.Right); controls.Children.Add(damage);
        scope = SmallButton("", () => { bossOnly = !bossOnly; Render(); }); DockPanel.SetDock(scope, Dock.Right); controls.Children.Add(scope);
        back = SmallButton("←", () => { actor = null; Render(); }); DockPanel.SetDock(back, Dock.Left); controls.Children.Add(back);
        picker = SmallButton("", () => _ = ShowHistory()); picker.HorizontalContentAlignment = HorizontalAlignment.Left; controls.Children.Add(picker);
        Grid.SetRow(controls, 2); layout.Children.Add(controls);
        var columnHead = new DockPanel();
        rateHead.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); DockPanel.SetDock(rateHead, Dock.Right); columnHead.Children.Add(rateHead);
        columns.TextTrimming = TextTrimming.CharacterEllipsis; columnHead.Children.Add(columns);
        Grid.SetRow(columnHead, 3); layout.Children.Add(columnHead);
        scroll = new ScrollViewer { Content = rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        unresolvedToggle = SmallButton("", () => { showUnidentified = !showUnidentified; Render(); });
        unresolvedToggle.HorizontalContentAlignment = HorizontalAlignment.Left;
        Grid.SetRow(scroll, 4); layout.Children.Add(scroll);
        var footer = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        grip = new Thumb { Width = 16, Height = 24, Cursor = Cursors.SizeNWSE, Background = Brushes.Transparent };
        var glyph = new FrameworkElementFactory(typeof(TextBlock)); glyph.SetValue(TextBlock.TextProperty, "◢"); glyph.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
        grip.Template = new ControlTemplate(typeof(Thumb)) { VisualTree = glyph };
        grip.DragDelta += (_, e) => { Width = Math.Clamp(Width + e.HorizontalChange, MinWidth, MaxWidth); Height = Math.Clamp(Height + e.VerticalChange, MinHeight, MaxHeight); };
        grip.DragStarted += (_, _) => { SetManipulating(true); ChangeAppearance(false, preferences.OverlayCompact, preferences.OverlayOpacity); };
        grip.DragCompleted += (_, _) => { SetManipulating(false); placement.EnsureVisible(); SaveLayout(); };
        DockPanel.SetDock(grip, Dock.Right); footer.Children.Add(grip);
        report = SmallButton("", OpenReport); DockPanel.SetDock(report, Dock.Right); footer.Children.Add(report);
        var aggregate = new StackPanel(); aggregate.Children.Add(hint); aggregate.Children.Add(total); footer.Children.Add(aggregate);
        Grid.SetRow(footer, 5); layout.Children.Add(footer);
        frame = new Border { Padding = new Thickness(8), CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(1), Child = layout };
        // Keep opacity on a child visual, including in offscreen render previews.
        var root = new Grid(); root.Children.Add(frame); Content = root;
        MouseEnter += (_, _) => { pointerInside = true; UpdateVisibility(); };
        MouseLeave += (_, _) => { pointerInside = false; UpdateVisibility(); };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && actor is not null) { actor = null; Render(); e.Handled = true; } };
        Closing += (_, _) => SaveLayout(); Apply(preferences);
    }

    private static double Bounded(double value, double fallback, double min, double max) => Math.Clamp(double.IsFinite(value) ? value : fallback, min, Math.Max(min, max));
    private void SaveLayout() => LayoutSaved?.Invoke(Left, Top, Width, Height);
    private Button SmallButton(string text, Action action)
    {
        var button = new Button { Content = text, FontSize = 11, Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(0, 0, 4, 0), MinWidth = 24 };
        button.Click += (_, _) => action(); return button;
    }

    public void Apply(Preferences value)
    {
        preferences = value; var theme = Themes.Get(value.Theme);
        foreach (var (key, color) in new[] { ("Background", theme.Background), ("Surface", theme.Surface), ("Foreground", theme.Foreground), ("Muted", theme.Muted), ("Border", theme.Border), ("Accent", theme.Accent) }) Resources[key] = Themes.Brush(color);
        frame.Background = Themes.Brush(theme.Background); frame.BorderBrush = Themes.Brush(theme.Border); Foreground = Themes.Brush(theme.Foreground);
        frame.Background.Opacity = Bounded(value.OverlayOpacity, .94, .65, 1);
        heading.Foreground = duration.Foreground = total.Foreground = empty.Foreground = Foreground;
        health.Foreground = status.Foreground = hint.Foreground = columns.Foreground = Themes.Brush(theme.Muted);
        hpBar.Background = Themes.Brush(theme.Border); hpBar.Foreground = Themes.Brush(theme.Accent);
        locking.ToolTip = T("lockOverlay"); close.ToolTip = T("closeOverlay"); options.ToolTip = T("overlayOptions"); grip.ToolTip = T("resizeOverlay");
        back.ToolTip = T("back"); picker.ToolTip = T("fightPicker"); report.Content = T("fightDetails");
        damage.ToolTip = T("damage"); healing.ToolTip = T("heals"); Render();
    }

    public void Update(Encounter? encounter, string? state = null)
    {
        if (live?.Id != encounter?.Id && archived is null) actor = null;
        live = encounter; if (state is not null) captureStatus = state;
        if (archived is null) Render();
        else UpdateVisibility();
    }
    private void SetMetric(bool value) { heals = value; Render(); }
    private void SelectFight(Encounter? value) { archived = value; actor = null; Render(); }
    private void OpenReport() { if (Selected is { } fight) DetailsRequested?.Invoke(fight, actor, heals, Target); }
    private void ChangeAppearance(bool autoFit, bool compact, double opacity)
    {
        preferences = preferences with { OverlayAutoFit = autoFit, OverlayCompact = compact, OverlayOpacity = opacity };
        AppearanceChanged?.Invoke(autoFit, compact, opacity); Apply(preferences);
    }
    private void SetManipulating(bool value) { manipulating = value; UpdateVisibility(); }

    private void UpdateVisibility()
    {
        // A retained last encounter is not proof that a fight is still active.
        var reading = !locked && (pointerInside || manipulating || openMenus > 0);
        var opacity = !preferences.OverlayFadeWhenIdle || captureStatus == "capturing" || archived is not null || reading ? 1 : IdleOpacity;
        if (visibilityOpacity == opacity) return;
        visibilityOpacity = opacity;
        var previous = frame.Opacity;
        frame.BeginAnimation(OpacityProperty, null);
        frame.Opacity = opacity;
        // Restore immediately; only fading out is animated. Hidden verification windows stay deterministic.
        if (opacity < previous && IsVisible && SystemParameters.ClientAreaAnimation)
            frame.BeginAnimation(OpacityProperty, new DoubleAnimation(previous, opacity, TimeSpan.FromMilliseconds(300)) { FillBehavior = FillBehavior.Stop });
    }

    private ContextMenu Menu()
    {
        var menu = new ContextMenu { Background = (Brush)Resources["Surface"], Foreground = Foreground, MaxHeight = 460, MaxWidth = 540 };
        menu.Opened += (_, _) => { openMenus++; UpdateVisibility(); };
        menu.Closed += (_, _) => { openMenus = Math.Max(0, openMenus - 1); UpdateVisibility(); };
        return menu;
    }
    private void ShowOptions()
    {
        var menu = BuildOptionsMenu(); menu.PlacementTarget = options; menu.IsOpen = true;
    }
    private ContextMenu BuildOptionsMenu()
    {
        var menu = Menu();
        void Item(string label, Action action, bool? check = null)
        {
            var item = new MenuItem { Header = label, IsCheckable = check is not null, IsChecked = check == true }; item.Click += (_, _) => action(); menu.Items.Add(item);
        }
        Item(T("autoFit"), () => ChangeAppearance(!preferences.OverlayAutoFit, preferences.OverlayCompact, preferences.OverlayOpacity), preferences.OverlayAutoFit);
        Item(T("compactRows"), () => ChangeAppearance(preferences.OverlayAutoFit, !preferences.OverlayCompact, preferences.OverlayOpacity), preferences.OverlayCompact);
        Item(T("fadeWhenIdle"), ToggleIdleFade, preferences.OverlayFadeWhenIdle);
        Item(T("snapEdges"), ToggleSnapping, preferences.OverlaySnapToEdges);
        var position = new MenuItem { Header = T("overlayPosition") };
        foreach (var corner in new[] { "topLeft", "topRight", "bottomLeft", "bottomRight", "centerOverlay" })
        {
            var item = new MenuItem { Header = T(corner) }; item.Click += (_, _) => placement.Place(corner); position.Items.Add(item);
        }
        menu.Items.Add(position);
        foreach (var opacity in new[] { .75, .9, 1 })
        {
            var level = opacity;
            Item($"{T("opacity")} {level:P0}", () => ChangeAppearance(preferences.OverlayAutoFit, preferences.OverlayCompact, level), Math.Abs(level - preferences.OverlayOpacity) < .01);
        }
        menu.Items.Add(new Separator()); Item(T("finish"), () => NewFightRequested?.Invoke());
        return menu;
    }
    private void ToggleSnapping()
    {
        preferences = preferences with { OverlaySnapToEdges = !preferences.OverlaySnapToEdges };
        SnappingChanged?.Invoke(preferences.OverlaySnapToEdges);
    }
    private void ToggleIdleFade()
    {
        preferences = preferences with { OverlayFadeWhenIdle = !preferences.OverlayFadeWhenIdle };
        IdleFadeChanged?.Invoke(preferences.OverlayFadeWhenIdle);
        UpdateVisibility();
    }
    public void RecoverPosition(Window? reference = null)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (locked && handle != 0) SetWindowLongPtr(handle, -20, GetWindowLongPtr(handle, -20) & ~(nint)(0x20 | 0x08000000));
        locked = false; locking.Visibility = close.Visibility = grip.Visibility = options.Visibility = Visibility.Visible;
        Render(); placement.Place("centerOverlay", reference);
    }
    private async Task ShowHistory()
    {
        var menu = Menu(); menu.PlacementTarget = picker;
        var current = new MenuItem { Header = T("currentFight"), IsCheckable = true, IsChecked = archived is null };
        current.Click += (_, _) => SelectFight(null); menu.Items.Add(current); menu.Items.Add(new Separator());
        var loading = new MenuItem { Header = T("loading"), IsEnabled = false }; menu.Items.Add(loading); menu.IsOpen = true;
        try
        {
            var history = store is null ? [] : await Task.Run(store.List); menu.Items.Remove(loading);
            foreach (var entry in history)
            {
                var item = new MenuItem { Header = $"{entry.StartedAt.ToLocalTime().ToString("g", Culture)} · {(entry.Title == "—" ? T("local") : entry.Title)} · {CombatPresentation.Duration(entry.DurationMs)}", IsCheckable = true, IsChecked = archived?.Id == entry.Id };
                item.Click += async (_, _) => { try { SelectFight(await Task.Run(() => store!.Load(entry.Id))); } catch (Exception error) when (IsFileError(error)) { hint.Text = T("loadError"); } };
                menu.Items.Add(item);
            }
            if (history.Count == 0) menu.Items.Add(new MenuItem { Header = T("emptyHistory"), IsEnabled = false });
        }
        catch (Exception error) when (IsFileError(error)) { loading.Header = T("loadError"); }
    }
    private static bool IsFileError(Exception error) => error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException;
    private void Lock()
    {
        var handle = new WindowInteropHelper(this).Handle; if (handle == IntPtr.Zero) return;
        SetWindowLongPtr(handle, -20, GetWindowLongPtr(handle, -20) | 0x20 | 0x08000000);
        locked = true; locking.Visibility = close.Visibility = grip.Visibility = options.Visibility = Visibility.Collapsed; Render();
    }

    private void Render()
    {
        UpdateVisibility();
        var fight = Selected; var target = Target;
        var boss = fight is null ? null : EncounterMath.PrimaryBoss(fight);
        heading.Text = actor is null ? (boss?.Name ?? (fight?.Title is { } title && title != "—" ? title : "DPSMeter")) : fight?.Participants.FirstOrDefault(p => p.Id == actor)?.Name ?? "DPSMeter";
        heading.ToolTip = heading.Text + "\n" + T("dragOverlayHint");
        if (actor is { } selected && fight?.Participants.FirstOrDefault(p => p.Id == selected)?.IsUnidentifiedSource == true)
            heading.Text = $"{T("sourceLabel")} #{selected}";
        duration.Text = CombatPresentation.Duration(fight is null ? 0 : EncounterMath.Window(fight, target).DurationMs);
        duration.ToolTip = T(target is null ? "durationAllHint" : "durationBossHint");
        status.Text = fight?.Origin == "demo" ? T("demoLabel") : fight?.Origin.Contains("unverified") == true ? T("importLabel")
            : archived is not null ? T("saved") : T(captureStatus);
        health.Text = boss?.CurrentHp is { } hp ? $"{T("hpObserved")} {N(hp)}" + (boss.HighestHp is { } max ? $" / {N(max)}" : "") : T("hpUnavailable");
        health.ToolTip = T("hpHint"); hpBar.Value = boss is { CurrentHp: { } current, HighestHp: > 0 } ? Math.Clamp(current * 100d / boss.HighestHp.Value, 0, 100) : 0;
        back.Visibility = actor is null ? Visibility.Collapsed : Visibility.Visible;
        picker.Content = T(archived is null ? "live" : "history") + " ▾";
        scope.Content = T(target is null ? "scopeAll" : "scopeBoss"); scope.ToolTip = T("scopeHint"); scope.IsEnabled = boss is not null && !heals;
        report.IsEnabled = fight is not null;
        damage.Background = (Brush)Resources[heals ? "Surface" : "Accent"]; damage.Foreground = (Brush)Resources[heals ? "Foreground" : "Background"];
        healing.Background = (Brush)Resources[heals ? "Accent" : "Surface"]; healing.Foreground = (Brush)Resources[heals ? "Background" : "Foreground"];
        columns.Text = actor is null ? T("overlayColumns") : T("skillColumns");
        rateHead.Text = (heals ? "HPS" : "DPS") + " / %";
        var wanted = new List<string>();
        void Entry(string key, Action<CombatantRow> update)
        {
            wanted.Add(key);
            if (!entries.TryGetValue(key, out var control)) { control = new CombatantRow { Style = (Style)FindResource(typeof(Button)) }; entries.Add(key, control); }
            update(control);
            var index = wanted.Count - 1;
            if (rows.Children.IndexOf(control) != index) { rows.Children.Remove(control); rows.Children.Insert(Math.Min(index, rows.Children.Count), control); }
        }
        var people = fight is null ? [] : EncounterMath.Players(fight, heals, target);
        if (fight is not null && actor is null)
        {
            foreach (var (row, rank) in people.Select((row, index) => (row, index + 1)))
            {
                var person = fight.Participants.First(p => p.Id == row.Id);
                if (person.IsUnidentifiedSource && !showUnidentified) continue;
                Entry("p" + row.Id, control =>
                {
                    var subtitle = $"{T(row.ClassName.Length == 0 ? "unknown" : row.ClassName)} · {row.CriticalRate.ToString("N1", Culture)} % {T("criticalShort")}";
                    if (person.CombatPower is { } power) subtitle += $" · {CombatPresentation.Short(power, preferences.Language)} {T("powerShort")}";
                    if (person.IsUnidentifiedSource) subtitle = T("unidentifiedSource");
                    var title = person.IsUnidentifiedSource ? $"{T("sourceLabel")} #{row.Id}" : $"{rank}. {row.Name}" + (person.IsSelf ? $" · {T("you")}" : "");
                    control.Update(title, subtitle, N(row.PerSecond), row.Share.ToString("N1", Culture) + "%",
                        $"{CombatPresentation.Short(row.Total, preferences.Language)} {T(heals ? "heals" : "damage")}", row.MaximumShare, person.IsUnidentifiedSource ? "" : row.ClassName, preferences, person.IsSelf);
                    control.Selected = () => { actor = row.Id; Render(); };
                    control.HoverContent = () => CombatHoverCard.Create(Selected!, row.Id, heals, Target, preferences);
                });
            }
        }
        else if (fight is not null && actor is { } id)
        {
            var spells = EncounterMath.Spells(fight, id, heals, target); var maximum = Math.Max(1, spells.FirstOrDefault()?.Total ?? 0);
            var className = fight.Participants.FirstOrDefault(p => p.Id == id)?.ClassName ?? "";
            foreach (var spell in spells)
                Entry("s" + spell.Id + ":" + spell.Name, control =>
                {
                    control.Update(Aion2SkillNames.Display(spell.Name), $"{N(spell.Hits)} {T("hits")} · {spell.CriticalRate.ToString("N1", Culture)} % {T("criticalShort")}",
                        N(spell.PerSecond), spell.Share.ToString("N1", Culture) + "%", CombatPresentation.Short(spell.Total, preferences.Language), spell.Total * 100d / maximum, className, preferences, skillId: fight.Origin == "demo" ? 0 : spell.Id);
                    control.Selected = OpenReport; control.HoverContent = () => CombatHoverCard.Create(Selected!, id, heals, Target, preferences);
                });
        }
        foreach (var key in entries.Keys.Except(wanted).ToArray()) { rows.Children.Remove(entries[key]); entries.Remove(key); }
        var unidentified = fight is null ? [] : people.Where(row => fight.Participants.First(p => p.Id == row.Id).IsUnidentifiedSource).ToArray();
        var showSources = actor is null && unidentified.Length > 0;
        if (showSources)
        {
            unresolvedToggle.Content = $"{(showUnidentified ? "▴" : "▾")} {unidentified.Length} {T("unidentifiedSources")} · {N(unidentified.Sum(row => row.PerSecond))} {(heals ? "HPS" : "DPS")}";
            unresolvedToggle.ToolTip = T("sourceHint");
            if (rows.Children.IndexOf(unresolvedToggle) != wanted.Count)
            {
                rows.Children.Remove(unresolvedToggle); rows.Children.Insert(wanted.Count, unresolvedToggle);
            }
        }
        else rows.Children.Remove(unresolvedToggle);
        rows.Children.Remove(empty);
        if (wanted.Count == 0 && !showSources) { empty.Text = T(fight is null ? "overlayEmpty" : "noMetric"); rows.Children.Add(empty); }
        var unresolvedCount = unidentified.Length;
        hint.Text = locked ? T("overlayLocked") : $"{T("observedGroup")} · {people.Count - unresolvedCount} {T("players").ToLower(Culture)}";
        hint.ToolTip = unresolvedCount == 0 ? null : T("sourceHint");
        total.Text = $"{N(people.Sum(p => p.PerSecond))} {(heals ? "HPS" : "DPS")}";
        total.ToolTip = fight is null ? "" : $"{N(people.Sum(p => p.Total))} {T(heals ? "heals" : "damage")} · {T(target is null ? "allTargets" : "scopeBoss")}";
        if (preferences.OverlayAutoFit)
        {
            var desired = 144 + Math.Max(1, Math.Min(8, wanted.Count)) * (preferences.OverlayCompact ? 30 : 44) + (showSources ? 24 : 0);
            placement.SetHeight(desired);
        }
    }

    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(IntPtr hwnd, int index, nint value);
}
