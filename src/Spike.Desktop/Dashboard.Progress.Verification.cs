using System.IO;
using System.Windows;
using System.Windows.Controls;
using Spike.Core;

namespace Spike.Desktop;

public partial class Dashboard
{
    private void VerifyProgress(string directory)
    {
        var demo = PreviewEncounter();
        demo = demo with { StartedAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Participants = demo.Participants.Select(p => p with { IsSelf = p.Id == 1,
                ObservedDeaths = p.IsPlayer ? 1 : null, IdentityEvidence = p.IsPlayer ? "direct" : "unknown" }).ToArray() };
        var improved = demo with { Id = Guid.NewGuid(), StartedAt = demo.StartedAt.AddDays(1),
            Events = demo.Events.Select(e => e with { Amount = e.Amount * 2 }).ToArray(),
            Participants = demo.Participants.Select(p => p with { ObservedDeaths = p.IsPlayer ? 0 : null }).ToArray() };
        var best = demo with { Id = Guid.NewGuid(), StartedAt = demo.StartedAt.AddDays(-1),
            Events = demo.Events.Select(e => e with { Amount = e.Amount * 3 }).ToArray() };
        progressAttempts = new[] { best, demo, improved }.SelectMany(EncounterProgress.Summarize).ToArray();
        progressPreferredFight = improved.Id; progressPreferredPlayer = null;
        SwitchPage("progress"); PopulateProgress();
        if (progressSeries.Count != 3 || (ProgressPlayer.SelectedItem as ProgressChoice)?.Name.StartsWith(demo.Participants[0].Name) != true
            || (ProgressReference.SelectedItem as AttemptChoice)?.Attempt.Id != demo.Id || !ProgressDelta.Text.Contains("100"))
            throw new InvalidOperationException("Progress did not select self, latest attempt and previous reference.");
        ChooseBestAttempt(this, new RoutedEventArgs());
        if ((ProgressReference.SelectedItem as AttemptChoice)?.Attempt.Id != best.Id || !ProgressDelta.Text.Contains('-'))
            throw new InvalidOperationException("Best previous attempt comparison failed.");
        ChoosePreviousAttempt(this, new RoutedEventArgs());
        lastLive = demo with { Id = Guid.NewGuid() }; Tick();
        if ((ProgressCurrent.SelectedItem as AttemptChoice)?.Attempt.Id != improved.Id || ProgressSkills.Items.Count == 0)
            throw new InvalidOperationException("Live capture disturbed progression selection or skills.");
        ((Button)ProgressChart.Children[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if ((ProgressCurrent.SelectedItem as AttemptChoice)?.Attempt.Id != best.Id || BestAttemptButton.IsEnabled)
            throw new InvalidOperationException("Trend navigation or earliest-attempt actions failed.");
        foreach (var language in Text.Languages)
            foreach (var theme in Themes.Ids)
            {
                preferences = new(language, theme, false, AutoStart: false);
                progressPreferredFight = improved.Id; ApplyTheme(); Translate();
                SaveDashboard(directory, $"{language}-{theme}-progress.png", 1100, 780);
                SaveDashboard(directory, $"{language}-{theme}-progress-minimum.png", 884, 600);
                foreach (var combo in new[] { ProgressContext, ProgressPlayer, ProgressCurrent, ProgressReference })
                {
                    var name = combo.SelectedItem is ProgressChoice context ? context.Name : ((AttemptChoice)combo.SelectedItem).Name;
                    if (!ProgressLabels(combo).Any(t => t.Text == name))
                        throw new InvalidOperationException("Selected comparison value is not rendered.");
                }
                if (!ProgressCaution.Text.Contains(Text.Get("progressCaution", language)) || ProgressDelta.Text.Contains("NaN"))
                    throw new InvalidOperationException("Progress localization or numbers failed.");
                if (!ProgressScope.Text.Contains(T("demoLabel"))) throw new InvalidOperationException("Synthetic progression is not visibly labelled.");
            }
        progressAttempts = Enumerable.Range(0, 25).SelectMany(i => EncounterProgress.Summarize(demo with
            { Id = Guid.NewGuid(), StartedAt = demo.StartedAt.AddDays(i) })).ToArray();
        progressPreferredFight = null; PopulateProgress();
        if (ProgressCurrent.Items.Count != 25 || ProgressChart.Children.Count != 20)
            throw new InvalidOperationException("Trend limit hides older attempts from selection.");
        progressAttempts = EncounterProgress.Summarize(demo); PopulateProgress();
        if (ProgressReference.Items.Count != 0 || ProgressDelta.Text != T("firstAttempt") || BestAttemptButton.IsEnabled)
            throw new InvalidOperationException("Single-attempt onboarding failed.");
        progressAttempts = []; PopulateProgress();
        if (ProgressContent.Visibility != Visibility.Collapsed || ProgressEmpty.Visibility != Visibility.Visible)
            throw new InvalidOperationException("Empty progression is not explained.");
        shown = demo; selectedActor = 1; RenderFight();
        if (!DetailsSummary.Text.Contains(T("observedDeaths"))) throw new InvalidOperationException("Observed deaths missing from player report.");
        File.WriteAllText(Path.Combine(directory, "progress-result.txt"),
            "PASS: self selection, previous/best reference, delta, skills, trend navigation, stable live refresh, 25-attempt selection, single/empty states, deaths, FR/EN/ES and three themes at normal/minimum sizes. Synthetic data only; no visible window or capture.");
    }

    private static IEnumerable<TextBlock> ProgressLabels(DependencyObject root)
    {
        if (root is TextBlock text) yield return text;
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in ProgressLabels(System.Windows.Media.VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
