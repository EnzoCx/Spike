using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionDPS.Aion2.Protocol;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    private IReadOnlyList<ProgressAttempt> progressAttempts = [];
    private IReadOnlyList<ProgressAttempt> progressSeries = [];
    private bool updatingProgress;
    private int progressRequest;
    private Guid? progressPreferredFight;
    private string? progressPreferredPlayer;
    private sealed record ProgressChoice(string Key, string Name);
    private sealed record AttemptChoice(ProgressAttempt Attempt, string Name);

    private string Deaths(int? count) => count is { } value ? N(value) : T("notRecorded");

    private void TranslateProgress()
    {
        foreach (var (button, key) in new[] { (ProgressNav, "progress"), (CompareButton, "compare"),
            (CompareHistoryButton, "compare"), (RefreshProgressButton, "refreshProgress"),
            (PreviousAttemptButton, "previousAttempt"), (BestAttemptButton, "bestAttempt"), (OpenProgressReportButton, "fightDetails") })
            button.Content = T(key);
        foreach (var (label, key) in new[] { (ProgressIntro, "progressIntro"), (ProgressContextLabel, "progressContext"),
            (ProgressPlayerLabel, "progressPlayer"), (ProgressCurrentLabel, "currentAttempt"),
            (ProgressReferenceLabel, "referenceAttempt"), (ProgressTrendTitle, "progressTrend"),
            (ProgressTrendHint, "progressTrendHint"), (ProgressSkillsTitle, "progressSkills") }) label.Text = T(key);
        System.Windows.Automation.AutomationProperties.SetName(ProgressContext, T("progressContext"));
        System.Windows.Automation.AutomationProperties.SetName(ProgressPlayer, T("progressPlayer"));
        System.Windows.Automation.AutomationProperties.SetName(ProgressCurrent, T("currentAttempt"));
        System.Windows.Automation.AutomationProperties.SetName(ProgressReference, T("referenceAttempt"));
        if (page == "progress") PopulateProgress();
    }

    private async void ShowProgress(object sender, RoutedEventArgs e) => await LoadProgress();
    private async void CompareShown(object sender, RoutedEventArgs e) => await LoadProgress(shown, selectedActor);
    private async void CompareHistory(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not HistoryDisplay selected) return;
        try { await LoadProgress(await Task.Run(() => store.Load(selected.Id))); }
        catch (Exception error) when (IsFileError(error)) { SetNotice("loadError"); }
    }

    private async Task LoadProgress(Encounter? preferred = null, int? actor = null)
    {
        var request = ++progressRequest;
        var selected = (ProgressCurrent.SelectedItem as AttemptChoice)?.Attempt;
        SwitchPage("progress");
        ProgressEmpty.Text = T("loadingProgress"); ProgressEmpty.Visibility = Visibility.Visible;
        ProgressContent.Visibility = Visibility.Collapsed;
        RefreshProgressButton.IsEnabled = false;
        try
        {
            await pendingSave;
            var attempts = verifying ? progressAttempts : await Task.Run(store.Progress);
            if (request != progressRequest) return;
            var anchor = preferred ?? lastLive;
            if (anchor is not null)
            {
                var extra = EncounterProgress.Summarize(anchor);
                attempts = attempts.Where(a => a.Id != anchor.Id).Concat(extra).ToArray();
                progressPreferredFight = preferred?.Id ?? selected?.Id ?? anchor.Id;
                progressPreferredPlayer = actor is { } id ? anchor.Participants.FirstOrDefault(p => p.Id == id)?.Name : selected?.PlayerName;
            }
            progressAttempts = attempts;
            PopulateProgress();
            if (!verifying && store.SkippedFiles > 0) SetNotice("progressSkipped");
        }
        catch (Exception error) when (IsFileError(error)) { ProgressEmpty.Text = T("loadError"); }
        finally { if (request == progressRequest) RefreshProgressButton.IsEnabled = true; }
    }

    private void PopulateProgress()
    {
        updatingProgress = true;
        var old = (ProgressContext.SelectedItem as ProgressChoice)?.Key;
        var preferred = progressAttempts.FirstOrDefault(a => a.Id == progressPreferredFight);
        var contexts = progressAttempts.GroupBy(a => a.Context).OrderByDescending(g => g.Max(a => a.At))
            .Select(g => new ProgressChoice(g.Key, ContextLabel(g.First()))).ToArray();
        ProgressContext.ItemsSource = contexts;
        ProgressContext.SelectedItem = contexts.FirstOrDefault(c => c.Key == preferred?.Context)
            ?? contexts.FirstOrDefault(c => c.Key == old) ?? contexts.FirstOrDefault();
        updatingProgress = false;
        PopulateProgressPlayers();
    }

    private string ContextLabel(ProgressAttempt attempt)
    {
        var parts = System.Text.Json.JsonSerializer.Deserialize<string[]>(attempt.Context)!;
        var zone = string.IsNullOrEmpty(parts[3]) || parts[3] == attempt.Title ? "" : $" · {parts[3]}";
        var origin = parts[2] == "demo" ? $" · {T("demoLabel")}" : parts[2] == "live" ? "" : $" · {T("importLabel")}";
        var legacy = parts[4].StartsWith("name:", StringComparison.Ordinal) ? $" · {T("legacyAttempts")}" : "";
        return $"{attempt.Title}{zone}{legacy} · {parts[1]}{origin}";
    }

    private void ProgressContextChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingProgress) { progressPreferredFight = null; PopulateProgressPlayers(); }
    }

    private void PopulateProgressPlayers()
    {
        updatingProgress = true;
        var old = (ProgressPlayer.SelectedItem as ProgressChoice)?.Key;
        var context = (ProgressContext.SelectedItem as ProgressChoice)?.Key;
        var candidates = progressAttempts.Where(a => a.Context == context).ToArray();
        var preferred = candidates.FirstOrDefault(a => a.PlayerName == progressPreferredPlayer && a.Id == progressPreferredFight)
            ?? candidates.FirstOrDefault(a => a.IsSelf && (progressPreferredFight is null || a.Id == progressPreferredFight));
        var players = candidates.GroupBy(a => a.PlayerKey).OrderByDescending(g => g.Any(a => a.IsSelf)).ThenBy(g => g.First().PlayerName)
            .Select(g => new ProgressChoice(g.Key, PlayerLabel(g.First()))).ToArray();
        ProgressPlayer.ItemsSource = players;
        ProgressPlayer.SelectedItem = players.FirstOrDefault(p => p.Key == preferred?.PlayerKey)
            ?? players.FirstOrDefault(p => p.Key == old) ?? players.FirstOrDefault();
        updatingProgress = false;
        PopulateProgressSeries();
    }

    private string PlayerLabel(ProgressAttempt attempt)
    {
        var parts = System.Text.Json.JsonSerializer.Deserialize<string[]>(attempt.PlayerKey)!;
        return $"{parts[0]} · {T(parts[1].Length == 0 ? "unknown" : parts[1])}"
            + (parts[2].Length > 0 ? $" · {T("serverLabel")} {parts[2]}" : $" · {T("serverUnknown")}");
    }

    private void ProgressPlayerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingProgress) PopulateProgressSeries();
    }

    private void PopulateProgressSeries()
    {
        updatingProgress = true;
        var context = (ProgressContext.SelectedItem as ProgressChoice)?.Key ?? "";
        var player = (ProgressPlayer.SelectedItem as ProgressChoice)?.Key ?? "";
        progressSeries = EncounterProgress.Series(progressAttempts, context, player);
        var attempts = progressSeries.Reverse().Select(AttemptLabel).ToArray();
        ProgressCurrent.ItemsSource = attempts;
        ProgressCurrent.SelectedItem = attempts.FirstOrDefault(a => a.Attempt.Id == progressPreferredFight) ?? attempts.FirstOrDefault();
        updatingProgress = false;
        PopulateReferences();
    }

    private AttemptChoice AttemptLabel(ProgressAttempt a) => new(a,
        $"{a.At.ToLocalTime().ToString("g", Culture)} · {N(a.Dps)} DPS · {T(Outcome(a.EndReason))}");

    private static string Outcome(string reason) => reason switch
    {
        "boss-defeated" => "bossDefeated", "boss-reset" => "bossReset", "active" => "attemptActive", _ => "attemptRecorded"
    };

    private void ProgressCurrentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingProgress) PopulateReferences();
    }

    private void PopulateReferences()
    {
        updatingProgress = true;
        var current = (ProgressCurrent.SelectedItem as AttemptChoice)?.Attempt;
        var choices = progressSeries.Where(a => a.Id != current?.Id).Reverse().Select(AttemptLabel).ToArray();
        ProgressReference.ItemsSource = choices;
        ProgressReference.SelectedItem = choices.FirstOrDefault(a => a.Attempt.At < current?.At) ?? choices.FirstOrDefault();
        updatingProgress = false;
        RenderProgressComparison();
    }

    private void ProgressReferenceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingProgress) RenderProgressComparison();
    }

    private void ChoosePreviousAttempt(object sender, RoutedEventArgs e) => ChooseReference(false);
    private void ChooseBestAttempt(object sender, RoutedEventArgs e) => ChooseReference(true);
    private void ChooseReference(bool best)
    {
        var current = (ProgressCurrent.SelectedItem as AttemptChoice)?.Attempt;
        var earlier = ProgressReference.Items.Cast<AttemptChoice>().Where(a => a.Attempt.At < current?.At);
        ProgressReference.SelectedItem = best ? earlier.OrderByDescending(a => a.Attempt.Dps).FirstOrDefault() : earlier.OrderByDescending(a => a.Attempt.At).FirstOrDefault();
    }

    private void RenderProgressComparison()
    {
        var current = (ProgressCurrent.SelectedItem as AttemptChoice)?.Attempt;
        var reference = (ProgressReference.SelectedItem as AttemptChoice)?.Attempt;
        ProgressEmpty.Visibility = current is null ? Visibility.Visible : Visibility.Collapsed;
        ProgressEmpty.Text = T("emptyProgress");
        ProgressContent.Visibility = current is null ? Visibility.Collapsed : Visibility.Visible;
        if (current is null) return;
        var earlier = progressSeries.Any(a => a.At < current.At);
        PreviousAttemptButton.IsEnabled = BestAttemptButton.IsEnabled = earlier;
        var change = reference is null ? null : EncounterProgress.Change(current.Dps, reference.Dps);
        ProgressDelta.Text = change is { } delta ? $"{delta.ToString("+0.0;-0.0;0.0", Culture)} % DPS" : T(reference is null ? "firstAttempt" : "noBaseline");
        ProgressNumbers.Text = reference is null ? $"{N(current.Dps)} DPS" : $"{N(reference.Dps)} → {N(current.Dps)} DPS";
        ProgressDetail.Text = reference is null
            ? $"{T("duration")} : {Duration(current.DurationMs)} · {T("observedDeaths")} : {Deaths(current.Deaths)}\n{N(current.Hps)} HPS"
            : $"{T("duration")} : {Duration(reference.DurationMs)} → {Duration(current.DurationMs)}\n{T("observedDeaths")} : {Deaths(reference.Deaths)} → {Deaths(current.Deaths)} · {N(reference.Hps)} → {N(current.Hps)} HPS";
        ProgressScope.Text = $"{progressSeries.Count} {T("fightCount")} · {T("progressScopeHint")}";
        var origin = System.Text.Json.JsonSerializer.Deserialize<string[]>(current.Context)![2];
        if (origin != "live") ProgressScope.Text = T(origin == "demo" ? "demoLabel" : "importLabel") + "\n" + ProgressScope.Text;
        ProgressCaution.Text = T("progressCaution") + (current.Uncertain || reference?.Uncertain == true ? "\n" + T("progressUncertain") : "")
            + (reference is not null && current.EndReason != reference.EndReason ? "\n" + T("differentOutcome") : "");
        var previousSkills = (reference?.Skills ?? []).ToDictionary(s => (s.Id, s.Name));
        var currentSkills = current.Skills.ToDictionary(s => (s.Id, s.Name));
        ProgressSkills.ItemsSource = currentSkills.Keys.Union(previousSkills.Keys).Select(key =>
        {
            var now = currentSkills.GetValueOrDefault(key)?.PerSecond ?? 0;
            var before = previousSkills.GetValueOrDefault(key)?.PerSecond ?? 0;
            return new { Name = Aion2SkillNames.Display(key.Name), Magnitude = Math.Abs(now - before),
                Delta = reference is null ? $"{N(now)} DPS" : $"{(now - before).ToString("+0;-0;0", Culture)} DPS",
                Values = reference is null ? "" : $"{N(before)} → {N(now)}" };
        }).OrderByDescending(s => s.Magnitude).ToArray();
        DrawProgressTrend(current.Id);
    }

    private void DrawProgressTrend(Guid current)
    {
        ProgressChart.Children.Clear(); ProgressChart.ColumnDefinitions.Clear();
        var points = progressSeries.TakeLast(20).ToArray();
        ProgressTrendRange.Text = $"{Math.Min(20, progressSeries.Count)} / {progressSeries.Count}";
        var max = Math.Max(1, points.Max(p => p.Dps));
        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            ProgressChart.ColumnDefinitions.Add(new ColumnDefinition());
            var button = new Button { Padding = new Thickness(2), Margin = new Thickness(2, 0, 2, 0),
                VerticalContentAlignment = VerticalAlignment.Bottom, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent, BorderThickness = new Thickness(point.Id == current ? 1 : 0),
                ToolTip = AttemptLabel(point).Name };
            System.Windows.Automation.AutomationProperties.SetName(button, AttemptLabel(point).Name);
            var stack = new StackPanel();
            var bar = new Border { Height = Math.Max(3, point.Dps / max * 95), CornerRadius = new CornerRadius(3, 3, 0, 0) };
            bar.SetResourceReference(Border.BackgroundProperty, point.Id == current ? "Accent" : "Muted");
            var plot = new Grid { Height = 95 };
            bar.VerticalAlignment = VerticalAlignment.Bottom; plot.Children.Add(bar); stack.Children.Add(plot);
            var label = new TextBlock { Text = point.At.ToLocalTime().ToString("dd/MM", Culture), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
            label.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
            if (points.Length <= 10 || i == 0 || i == points.Length - 1) stack.Children.Add(label);
            else stack.Children.Add(new Border { Height = 18 });
            button.Content = stack;
            button.Click += (_, _) => ProgressCurrent.SelectedItem = ProgressCurrent.Items.Cast<AttemptChoice>().First(a => a.Attempt.Id == point.Id);
            Grid.SetColumn(button, i); ProgressChart.Children.Add(button);
        }
    }

    private async void OpenProgressReport(object sender, RoutedEventArgs e)
    {
        if (ProgressCurrent.SelectedItem is not AttemptChoice choice) return;
        try
        {
            var fight = lastLive?.Id == choice.Attempt.Id ? lastLive : await Task.Run(() => store.Load(choice.Attempt.Id));
            var actor = fight.Participants.FirstOrDefault(p => p.Name == choice.Attempt.PlayerName && p.IsPlayer)?.Id;
            DisplayEncounter(fight, actor, false);
        }
        catch (Exception error) when (IsFileError(error)) { SetNotice("loadError"); }
    }
}
