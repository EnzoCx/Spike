using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AionDPS.Aion2.Protocol;
using Spike.Core;

namespace Spike.Desktop;

internal static class CombatHoverCard
{
    public static Border Create(Encounter fight, int actor, bool heals, int? target, Preferences preferences)
    {
        var palette = Themes.Get(preferences.Theme); var language = preferences.Language; var culture = CultureInfo.GetCultureInfo(language);
        string T(string key) => Text.Get(key, language);
        string N(double value) => value.ToString("N0", culture);
        var person = fight.Participants.First(p => p.Id == actor);
        var row = EncounterMath.Players(fight, heals, target).FirstOrDefault(p => p.Id == actor);
        var stack = new StackPanel();
        TextBlock Label(string text, double size = 12, bool muted = false) => new()
        {
            Text = text,
            FontSize = size,
            Foreground = Themes.Brush(muted ? palette.Muted : palette.Foreground),
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Geist")
        };
        var head = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var icon = CombatPresentation.Emblem(person.IsUnidentifiedSource ? "" : person.ClassName, 32); icon.Margin = new Thickness(0, 0, 10, 0); head.Children.Add(icon);
        var identity = new StackPanel(); identity.Children.Add(Label(person.IsUnidentifiedSource ? $"{T("sourceLabel")} #{actor}" : person.Name, 20));
        identity.Children.Add(Label($"{T(person.ClassName.Length == 0 ? "unknown" : person.ClassName)} · {(target is null ? T("allTargets") : fight.Participants.First(p => p.Id == target).Name)}", 12, true));
        head.Children.Add(identity); stack.Children.Add(head);
        var metrics = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 14) };
        foreach (var (caption, value) in new[] { (T(heals ? "heals" : "damage"), CombatPresentation.Short(row?.Total ?? 0, language)),
            (heals ? "HPS" : "DPS", N(row?.PerSecond ?? 0)), (T("share"), (row?.Share ?? 0).ToString("N1", culture) + " %"),
            (T("duration"), CombatPresentation.Duration(EncounterMath.Window(fight, target).DurationMs)) })
        {
            var block = new StackPanel(); block.Children.Add(Label(caption, 10, true)); block.Children.Add(Label(value, 18)); metrics.Children.Add(block);
        }
        stack.Children.Add(metrics);
        stack.Children.Add(Label($"{N(row?.Hits ?? 0)} {T("hits")}   ·   {T("criticalObserved")} {(row?.CriticalRate ?? 0).ToString("N1", culture)} %", 12));
        if (person.CombatPower is { } power) stack.Children.Add(Label($"{T("combatPower")} : {N(power)}", 12, true));
        var heading = Label(T("topSkills"), 12, true); heading.Margin = new Thickness(0, 16, 0, 8); stack.Children.Add(heading);
        foreach (var spell in EncounterMath.Spells(fight, actor, heals, target).Take(8))
        {
            var entry = new DockPanel();
            var artwork = GameArtwork.SkillIcon(fight.Origin == "demo" ? 0 : spell.Id, Aion2SkillNames.Display(spell.Name), 32);
            artwork.Margin = new Thickness(0, 2, 10, 0); artwork.VerticalAlignment = VerticalAlignment.Top;
            DockPanel.SetDock(artwork, Dock.Left); entry.Children.Add(artwork);
            var body = new StackPanel(); entry.Children.Add(body); stack.Children.Add(entry);
            var line = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };
            var amount = Label($"{CombatPresentation.Short(spell.Total, language)} · {spell.Share.ToString("N1", culture)} %", 12);
            amount.Foreground = Themes.Brush(palette.Accent); amount.Margin = new Thickness(12, 0, 0, 0); DockPanel.SetDock(amount, Dock.Right); line.Children.Add(amount);
            var title = Label(Aion2SkillNames.Display(spell.Name), 13); title.TextWrapping = TextWrapping.NoWrap; title.TextTrimming = TextTrimming.CharacterEllipsis; line.Children.Add(title); body.Children.Add(line);
            body.Children.Add(Label($"{N(spell.Hits)} {T("hits")} · {N(spell.Ticks)} {T("ticks")} · {spell.CriticalRate.ToString("N1", culture)} % {T("criticalShort")}", 10, true));
            var track = new Grid { Height = 2, Margin = new Thickness(0, 5, 0, 10), Background = Themes.Brush(palette.Border) };
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(spell.Share, GridUnitType.Star) });
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(.01, 100 - spell.Share), GridUnitType.Star) });
            track.Children.Add(new Border { Background = Dashboard.ClassColor(person.ClassName) }); body.Children.Add(track);
        }
        var note = Label(T(person.IsUnidentifiedSource ? "sourceHint" : "hoverHint"), 11, true); note.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(note);
        return new Border
        {
            Width = 460,
            Background = Themes.Brush(palette.Background),
            BorderBrush = Themes.Brush(palette.Border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20),
            Child = stack
        };
    }
}
