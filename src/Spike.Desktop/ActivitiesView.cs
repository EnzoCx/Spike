using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Spike.Core;

namespace Spike.Desktop;

/// <summary>The dashboard and mini-meter share the same controller, never a second timer or file writer.</summary>
internal sealed class ActivitiesView : ScrollViewer
{
    private readonly ActivityController controller;
    private readonly bool compact;
    private readonly Action testNotification;
    private readonly StackPanel root = new();
    private readonly List<Action<DateTimeOffset>> clocks = [];
    private string language;
    private string section = "checklist";
    private ActivityPeriod period;
    private readonly List<Popup> popups = [];
    private string T(string key) => Text.Get(key, language);
    private CultureInfo Culture => CultureInfo.GetCultureInfo(language);
    private string ActivityName(ScheduledActivity e) => e.CustomName.Length > 0 ? e.CustomName : T(e.NameKey);
    private string ActivityName(ChecklistActivity t) => t.CustomName.Length > 0 ? t.CustomName : T(t.NameKey);

    public ActivitiesView(ActivityController controller, string language, bool compact, Action testNotification)
    {
        this.controller = controller; this.language = language; this.compact = compact; this.testNotification = testNotification;
        Content = root; VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Padding = new Thickness(0, 0, compact ? 4 : 12, 0);
        controller.Changed += Refresh;
        Refresh();
    }

    public void Detach() { controller.Changed -= Refresh; ClosePopups(); }
    private void ClosePopups() { foreach (var popup in popups) popup.IsOpen = false; popups.Clear(); }
    public void Translate(string value) { language = value; Refresh(); }
    public void Tick() { foreach (var update in clocks) update(controller.Clock()); }

    private TextBlock Label(string text, bool muted = false, double size = 13)
    {
        var label = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = size, Margin = new Thickness(0, 0, 0, 8) };
        label.SetResourceReference(TextBlock.ForegroundProperty, muted ? "Muted" : "Foreground");
        return label;
    }
    private Button Button(string text, Action action, string? key = null)
    {
        var button = new Button { Content = text, Padding = new Thickness(compact ? 8 : 12, 7, compact ? 8 : 12, 7), Tag = key };
        button.Click += (_, _) => action();
        AutomationProperties.SetName(button, text);
        return button;
    }
    private TextBox Input(string label, string text, int max = 120)
    {
        var box = new TextBox { Text = text, MaxLength = max, Margin = new Thickness(0, 0, 0, 10), MinWidth = 80 };
        AutomationProperties.SetName(box, label); box.ToolTip = label;
        return box;
    }
    private Border Card(UIElement child)
    {
        var card = new Border { Child = child, Padding = new Thickness(compact ? 10 : 12),
            CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, 0, 6) };
        card.SetResourceReference(Border.BackgroundProperty, "Surface"); return card;
    }
    private void Section(StackPanel parent, string text) { var label = Label(text, size: 15); label.FontWeight = FontWeights.SemiBold; parent.Children.Add(label); }
    private static string Countdown(TimeSpan time)
    {
        var seconds = Math.Max(0, (long)Math.Ceiling(time.TotalSeconds));
        return seconds >= 86400 ? $"{seconds / 86400}d {seconds / 3600 % 24:00}h"
            : seconds >= 3600 ? $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void Refresh()
    {
        // Restore keyboard focus after an immediately persisted checkbox/counter change.
        var focusKey = (System.Windows.Input.Keyboard.FocusedElement as FrameworkElement)?.Tag as string;
        var scrollOffset = VerticalOffset;
        var expanded = Descendants<Expander>(root).Where(e => e.IsExpanded).Select(e => e.Tag as string ?? e.Header?.ToString()).ToHashSet();
        ClosePopups();
        root.Children.Clear(); clocks.Clear();
        if (controller.ErrorKey is { } error) root.Children.Add(Label(T(error)));
        if (!compact)
        {
            root.Children.Add(Label(T("activitiesIntro"), true));
            root.Children.Add(ActivitySegments.Create(new[] { "checklist", "schedule" }.Select(id =>
                (T(id), id == section, (Action)(() => { section = id; Refresh(); }))).ToArray()));
        }
        if (compact || section == "checklist") BuildChecklist(); else BuildSchedule();
        foreach (var expander in Descendants<Expander>(root)) expander.IsExpanded = expanded.Contains(expander.Tag as string ?? expander.Header?.ToString());
        Tick(); ScrollToVerticalOffset(scrollOffset);
        if (focusKey is not null) RestoreFocus(root, focusKey);
    }

    private static IEnumerable<TControl> Descendants<TControl>(DependencyObject parent) where TControl : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is TControl control) yield return control;
            foreach (var nested in Descendants<TControl>(child)) yield return nested;
        }
    }

    private static bool RestoreFocus(DependencyObject parent, string key)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is FrameworkElement element && Equals(element.Tag, key)) { element.Focus(); return true; }
            if (RestoreFocus(child, key)) return true;
        }
        return false;
    }

    private string PeriodName(ActivityPeriod value) => T(value switch
    { ActivityPeriod.Daily => "daily", ActivityPeriod.Weekly => "weekly", _ => "reserves" });

    private string HelpText(ChecklistActivity task)
    {
        if (task.Notes.Length > 0) return task.Notes;
        var key = task.NameKey + "Help";
        var text = T(key);
        return text == key ? T(task.NameKey.Length == 0 ? "customHelp" : "legacyHelp") : text;
    }

    private void ShowPopup(Button anchor, FrameworkElement content)
    {
        ClosePopups();
        var body = new StackPanel();
        var close = Button(T("closeDetails") + " ×", ClosePopups);
        close.HorizontalAlignment = HorizontalAlignment.Right; body.Children.Add(close); body.Children.Add(content);
        var frame = Card(body); frame.Width = compact ? 270 : 330; frame.Margin = new Thickness(0);
        frame.BorderThickness = new Thickness(1); frame.SetResourceReference(Border.BorderBrushProperty, "Border");
        var popup = new Popup { Child = frame, PlacementTarget = anchor, Placement = PlacementMode.Bottom,
            StaysOpen = false, AllowsTransparency = true };
        popup.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { ClosePopups(); anchor.Focus(); e.Handled = true; } };
        popups.Add(popup); popup.IsOpen = true;
        close.Focus();
    }

    private Button Info(ChecklistActivity task)
    {
        var button = Button("i", () => { }, task.Id + "-info"); button.Padding = new Thickness(7, 2, 7, 2);
        button.Margin = new Thickness(4, 0, 0, 0); button.VerticalAlignment = VerticalAlignment.Center;
        var text = HelpText(task);
        button.ToolTip = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 300 };
        ToolTipService.SetShowDuration(button, 30000);
        AutomationProperties.SetName(button, T("activityInfo") + " · " + ActivityName(task));
        AutomationProperties.SetHelpText(button, text);
        button.Click += (_, _) =>
        {
            var body = new StackPanel(); Section(body, ActivityName(task)); body.Children.Add(Label(text));
            body.Children.Add(Label(T(task.Shared ? "sharedScope" : "characterScope"), true, 11));
            if (task.NameKey.Length > 0)
            {
                body.Children.Add(SourceLink("https://corpus.gg/blog/aion-2/daily-weekly-checklist"));
                body.Children.Add(SourceLink("https://metabot.gg/en/aion-2/guides/daily-weekly-checklist"));
                body.Children.Add(SourceLink("https://aion2maps.com/guides/daily-and-weekly/"));
            }
            ShowPopup(button, body);
        };
        return button;
    }

    private void BuildChecklist()
    {
        var data = controller.Data;
        var profiles = new ComboBox { DisplayMemberPath = "Name", SelectedValuePath = "Id", Margin = new Thickness(0, 0, 0, 10), ToolTip = T("serverProfiles") };
        profiles.ItemsSource = data.Profiles.Select(p => new { p.Id, Name = p.Name.Length == 0 ? T("mainCharacter") : p.Name });
        profiles.SelectedValue = data.ActiveProfile;
        AutomationProperties.SetName(profiles, T("progressPlayer"));
        profiles.SelectionChanged += (_, _) => { if (profiles.SelectedValue is string id && id != controller.Data.ActiveProfile) controller.Change(controller.Data with { ActiveProfile = id }); };
        root.Children.Add(profiles);
        root.Children.Add(ActivitySegments.Create(Enum.GetValues<ActivityPeriod>().Select(value =>
            (PeriodName(value), value == period, (Action)(() => { period = value; Refresh(); }))).ToArray()));
        var tasks = data.Tasks.Where(t => t.Visible && t.Period == period).ToArray();
        if (period == ActivityPeriod.Reserve)
        {
            root.Children.Add(Label(T("reserveIntro"), size: compact ? 13 : 20));
            root.Children.Add(Label(T("reserveHint"), true, 11));
        }
        else
        {
            var done = tasks.Count(t => ActivitySchedule.Count(data, t, controller.Clock()) == t.Goal);
            var summary = Label(string.Format(Culture, T("checklistProgress"), done, tasks.Length), size: compact ? 13 : 20);
            summary.FontWeight = FontWeights.SemiBold; root.Children.Add(summary);
            var progress = new ProgressBar { Minimum = 0, Maximum = Math.Max(1, tasks.Length), Value = done, Height = 3, Margin = new Thickness(0, 0, 0, 10) };
            progress.SetResourceReference(Control.StyleProperty, "ActivityProgress");
            progress.SetResourceReference(Control.ForegroundProperty, "Accent"); root.Children.Add(progress);
            var reset = Label("", true, 11); root.Children.Add(reset);
            clocks.Add(now => reset.Text = string.Format(Culture, T("nextReset"),
                ActivitySchedule.NextReset(now, controller.Data.Settings, period).ToLocalTime().ToString("ddd HH:mm", Culture),
                Countdown(ActivitySchedule.NextReset(now, controller.Data.Settings, period) - now)));
            if (done == tasks.Length && done > 0) root.Children.Add(Label(T("allDone"), true, 12));
        }
        if (tasks.Length == 0) root.Children.Add(Label(T("emptyChecklist"), true));
        foreach (var task in tasks)
        {
            if (task.Period == ActivityPeriod.Reserve) { BuildReserve(task); continue; }
            var count = ActivitySchedule.Count(data, task, controller.Clock());
            var row = new DockPanel { LastChildFill = true };
            var info = Info(task); DockPanel.SetDock(info, Dock.Right); row.Children.Add(info);
            if (task.Goal > 1)
            {
                var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var minus = Button("−", () => controller.SetCount(task, ActivitySchedule.Count(controller.Data, task, controller.Clock()) - 1), task.Id + "-");
                minus.IsEnabled = count > 0; minus.ToolTip = T("decrease") + " · " + ActivityName(task); AutomationProperties.SetName(minus, (string)minus.ToolTip);
                var plus = Button("+", () => controller.SetCount(task, ActivitySchedule.Count(controller.Data, task, controller.Clock()) + 1), task.Id + "+");
                plus.IsEnabled = count < task.Goal; plus.ToolTip = T("increase") + " · " + ActivityName(task); AutomationProperties.SetName(plus, (string)plus.ToolTip);
                foreach (var button in new[] { minus, plus }) { button.Padding = new Thickness(6, 3, 6, 3); button.Margin = new Thickness(0); }
                var value = Label($"{count}/{task.Goal}", true, 12); value.Margin = new Thickness(5, 0, 5, 0); value.VerticalAlignment = VerticalAlignment.Center;
                actions.Children.Add(minus); actions.Children.Add(value); actions.Children.Add(plus);
                DockPanel.SetDock(actions, Dock.Right); row.Children.Add(actions);
            }
            var label = Label(ActivityName(task), size: compact ? 12 : 13); label.Margin = new Thickness(0); if (count == task.Goal) label.Opacity = .6;
            var title = new StackPanel(); title.Children.Add(label);
            var scope = Label(T(task.Shared ? "sharedScope" : "characterScope"), true, 10); scope.Margin = new Thickness(0, 3, 0, 0); title.Children.Add(scope);
            var check = new CheckBox { Content = title, IsChecked = count == task.Goal, Tag = task.Id, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 8, 2) };
            check.ToolTip = new TextBlock { Text = HelpText(task), MaxWidth = 300, TextWrapping = TextWrapping.Wrap };
            AutomationProperties.SetName(check, ActivityName(task));
            check.Click += (_, _) => { if (!controller.SetCount(task, check.IsChecked == true ? task.Goal : 0)) check.IsChecked = count == task.Goal; };
            row.Children.Add(check); root.Children.Add(Card(row));
        }
        if (compact) return;
        root.Children.Add(Label(T("manualChecklist"), true, 11));
        BuildChecklistEditor();
    }

    private void BuildReserve(ChecklistActivity task)
    {
        var recorded = ActivitySchedule.Recorded(controller.Data, task, controller.Clock());
        var body = new StackPanel(); var row = new DockPanel();
        var info = Info(task); DockPanel.SetDock(info, Dock.Right); row.Children.Add(info);
        var edit = Button(recorded is null ? "— / " + task.Goal : $"{recorded.Count} / {task.Goal}", () => { }, task.Id + "-stock");
        edit.Margin = new Thickness(8, 0, 0, 0); edit.FontWeight = FontWeights.SemiBold;
        edit.ToolTip = T("reserveEdit"); AutomationProperties.SetName(edit, T("reserveEdit") + " · " + ActivityName(task));
        edit.Click += (_, _) =>
        {
            var content = new StackPanel(); Section(content, ActivityName(task));
            content.Children.Add(Label(T("reserveHelp"), true, 12));
            var value = Input(T("reserveValue"), recorded?.Count.ToString(CultureInfo.InvariantCulture) ?? "", 3);
            content.Children.Add(value); var error = Label("", true, 12); content.Children.Add(error);
            void Save()
            {
                if (!int.TryParse(value.Text, out var number) || number < 0 || number > task.Goal)
                { error.Text = string.Format(Culture, T("reserveError"), task.Goal); value.Focus(); return; }
                controller.SetCount(task, number);
            }
            content.Children.Add(Button(T("apply"), Save));
            value.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Save(); e.Handled = true; } };
            ShowPopup(edit, content); value.Focus(); value.SelectAll();
        };
        DockPanel.SetDock(edit, Dock.Right); row.Children.Add(edit);
        var name = Label(ActivityName(task), size: compact ? 12 : 14); name.FontWeight = FontWeights.SemiBold; name.Margin = new Thickness(0, 3, 0, 3); row.Children.Add(name);
        body.Children.Add(row);
        body.Children.Add(Label(T(task.Shared ? "sharedScope" : "characterScope") + " · " + (recorded is null ? T("reserveUnknown")
            : string.Format(Culture, T("reserveUpdated"), recorded.UpdatedAt.ToLocalTime().ToString("g", Culture))), true, 10));
        var bar = new ProgressBar { Maximum = task.Goal, Value = recorded?.Count ?? 0, Height = 3 };
        bar.SetResourceReference(Control.StyleProperty, "ActivityProgress");
        bar.SetResourceReference(Control.ForegroundProperty, "Accent"); body.Children.Add(bar);
        if (recorded?.Count == task.Goal) { var cap = Label(T("reserveCapacity"), true, 10); cap.Margin = new Thickness(0, 5, 0, 0); body.Children.Add(cap); }
        root.Children.Add(Card(body));
    }

    private void BuildChecklistEditor()
    {
        var editor = new StackPanel();
        editor.Children.Add(Label(T("customizeHint"), true, 12));
        var error = Label("", true, 12); editor.Children.Add(error);
        foreach (var task in controller.Data.Tasks.Where(t => t.Period == period))
        {
            var row = new DockPanel();
            var description = task.NameKey.Length == 0 ? Input(T("activityNotes"), task.Notes, 1000) : null;
            if (description is not null) { description.AcceptsReturn = true; description.TextWrapping = TextWrapping.Wrap; description.MinHeight = 48; }
            var goal = Input(T(task.Period == ActivityPeriod.Reserve ? "reserveCap" : "goal"), task.Goal.ToString(CultureInfo.InvariantCulture), 3); goal.MinWidth = 54; goal.Width = 54;
            var save = Button(T("apply"), () =>
            {
                if (!int.TryParse(goal.Text, out var value) || value is < 1 or > 999) { error.Text = T("goalError"); goal.Focus(); return; }
                controller.Change(controller.Data with { Tasks = controller.Data.Tasks.Select(t => t.Id == task.Id ? t with { Goal = value, Notes = description?.Text.Trim() ?? t.Notes } : t).ToArray() });
            });
            DockPanel.SetDock(save, Dock.Right); row.Children.Add(save); DockPanel.SetDock(goal, Dock.Right); row.Children.Add(goal);
            var visible = new CheckBox { Content = new TextBlock { Text = ActivityName(task), TextWrapping = TextWrapping.Wrap }, IsChecked = task.Visible, Margin = new Thickness(0, 6, 8, 8) };
            visible.Click += (_, _) => controller.Change(controller.Data with { Tasks = controller.Data.Tasks.Select(t => t.Id == task.Id ? t with { Visible = visible.IsChecked == true } : t).ToArray() });
            row.Children.Add(visible); editor.Children.Add(row);
            if (description is not null) editor.Children.Add(description);
        }
        editor.Children.Add(Label(T("addTask")));
        var name = Input(T("taskName"), ""); editor.Children.Add(name);
        var notes = Input(T("activityNotes"), "", 1000); notes.AcceptsReturn = true; notes.TextWrapping = TextWrapping.Wrap; notes.MinHeight = 48; editor.Children.Add(notes);
        var add = Button(T("addTask") + " · " + PeriodName(period), () =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) { name.Focus(); return; }
            if (controller.Data.Tasks.Length >= 100) { error.Text = T("checklistLimit"); return; }
            controller.Change(controller.Data with { Tasks = [.. controller.Data.Tasks, new(Guid.NewGuid().ToString("N"), "", name.Text.Trim(), period, Notes: notes.Text.Trim())] });
        }); editor.Children.Add(add);
        editor.Children.Add(Label(T("serverProfiles"), true, 11));
        editor.Children.Add(Label(T("addCharacter")));
        var character = Input(T("progressPlayer"), ""); editor.Children.Add(character);
        editor.Children.Add(Button(T("addCharacter"), () =>
        {
            if (string.IsNullOrWhiteSpace(character.Text)) { character.Focus(); return; }
            if (controller.Data.Profiles.Length >= 20) { error.Text = T("characterLimit"); return; }
            var id = Guid.NewGuid().ToString("N");
            controller.Change(controller.Data with { ActiveProfile = id, Profiles = [.. controller.Data.Profiles, new(id, character.Text.Trim(), new())] });
        }));
        root.Children.Add(new Expander { Header = T("customizeChecklist"), Content = editor, Margin = new Thickness(0, 12, 0, 20) });
    }

    private void BuildSchedule()
    {
        var data = controller.Data;
        root.Children.Add(Label(T("localSchedule"), size: 18));
        var evidence = Label(T("scheduleEvidence"), true, 11); root.Children.Add(evidence);
        BuildSettings();
        var eventEditor = new StackPanel();
        root.Children.Add(Button(T("addEvent"), () => EditEvent(null, eventEditor)));
        root.Children.Add(eventEditor);
        var eventList = new StackPanel(); root.Children.Add(eventList);
        var eventCards = new List<(ScheduledActivity Activity, Border Card)>();
        foreach (var occurrence in data.Events.Select(e => ActivitySchedule.Next(e, data.Settings, controller.Clock())).OrderBy(o => o.StartsAt))
        {
            var activity = occurrence.Activity;
            var card = new StackPanel();
            var row = new DockPanel();
            var edit = Button("…", () => EditEvent(activity, eventEditor)); edit.ToolTip = T("editEvent"); AutomationProperties.SetName(edit, T("editEvent") + " · " + ActivityName(activity)); DockPanel.SetDock(edit, Dock.Right); row.Children.Add(edit);
            var notify = new CheckBox { Content = T("notifyMe"), IsChecked = activity.Notify, Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            AutomationProperties.SetName(notify, T("notifyMe") + " · " + ActivityName(activity));
            notify.Click += (_, _) => controller.Change(controller.Data with { Events = controller.Data.Events.Select(e => e.Id == activity.Id ? e with { Notify = notify.IsChecked == true } : e).ToArray() });
            DockPanel.SetDock(notify, Dock.Right); row.Children.Add(notify);
            var name = Label(ActivityName(activity), size: 14); name.FontWeight = FontWeights.SemiBold; row.Children.Add(name); card.Children.Add(row);
            var next = Label("", true, 12); card.Children.Add(next);
            clocks.Add(now =>
            {
                var starts = ActivitySchedule.Next(activity, controller.Data.Settings, now).StartsAt;
                next.Text = starts.ToLocalTime().ToString("ddd HH:mm", Culture) + "   ·   " + Countdown(starts - now);
            });
            var detail = new StackPanel();
            detail.Children.Add(Label(T(activity.Reliability) + " · " + T("referenceClock") + " " + OffsetLabel(data.Settings.UtcOffsetMinutes), true, 11));
            var times = string.Join(", ", activity.Minutes.Select(ActivitySchedule.FormatMinute));
            var days = string.Join(" · ", activity.Days.OrderBy(d => ((int)d + 6) % 7).Select(d => Culture.DateTimeFormat.AbbreviatedDayNames[(int)d]));
            detail.Children.Add(Label(days + " · " + times, true, 11));
            if (activity.Source.Length > 0) detail.Children.Add(SourceLink(activity.Source));
            card.Children.Add(new Expander { Header = T(activity.Reliability == "uncertain" ? "uncertain" : "eventDetails"), Tag = "event-" + activity.Id, Content = detail, FontSize = 11 });
            var border = Card(card); eventList.Children.Add(border); eventCards.Add((activity, border));
        }
        clocks.Add(now =>
        {
            var ordered = eventCards.OrderBy(c => ActivitySchedule.Next(c.Activity, controller.Data.Settings, now).StartsAt).ToArray();
            for (var index = 0; index < ordered.Length; index++)
                if (!ReferenceEquals(eventList.Children[index], ordered[index].Card))
                { eventList.Children.Remove(ordered[index].Card); eventList.Children.Insert(index, ordered[index].Card); }
        });
    }

    private static string OffsetLabel(int minutes) => "UTC" + (minutes >= 0 ? "+" : "−") + ActivitySchedule.FormatMinute(Math.Abs(minutes));

    private FrameworkElement SourceLink(string url)
    {
        var text = Label(T("source") + " ↗", true, 11);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https") return text;
        var link = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(uri.Host)) { NavigateUri = uri };
        link.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Muted");
        link.RequestNavigate += (_, e) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException) { text.Text = url; }
            e.Handled = true;
        };
        text.Inlines.Add(" "); text.Inlines.Add(link); return text;
    }

    private void BuildSettings()
    {
        var settings = controller.Data.Settings;
        var panel = new StackPanel();
        var row = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        var enabled = new CheckBox { Content = T("enableReminders"), IsChecked = settings.Notifications, ToolTip = T("reminderHint"), Margin = new Thickness(0, 8, 18, 8) };
        enabled.Click += (_, _) => controller.Change(controller.Data with { Settings = controller.Data.Settings with { Notifications = enabled.IsChecked == true } });
        row.Children.Add(enabled);
        var sound = new CheckBox { Content = T("notificationSound"), IsChecked = settings.Sound, Margin = new Thickness(0, 8, 18, 8) };
        sound.Click += (_, _) => controller.Change(controller.Data with { Settings = controller.Data.Settings with { Sound = sound.IsChecked == true } }); row.Children.Add(sound);
        var lead = new ComboBox { Width = 145, Margin = new Thickness(0, 0, 8, 0), DisplayMemberPath = "Name", SelectedValuePath = "Minutes", ToolTip = T("leadMinutes") };
        lead.ItemsSource = Enumerable.Range(0, 61).Select(i => new { Minutes = i, Name = i == 0 ? T("leadNow") : string.Format(Culture, T("leadOption"), i) });
        lead.SelectedValue = settings.LeadMinutes; AutomationProperties.SetName(lead, T("leadMinutes"));
        lead.SelectionChanged += (_, _) => { if (lead.SelectedValue is int minutes) controller.Change(controller.Data with { Settings = controller.Data.Settings with { LeadMinutes = minutes } }); };
        row.Children.Add(lead); row.Children.Add(Button(T("testNotification"), testNotification)); panel.Children.Add(row);
        var details = new StackPanel();
        var fields = new WrapPanel();
        void Field(string label, FrameworkElement input)
        {
            var field = new StackPanel { Width = 190, Margin = new Thickness(0, 8, 12, 0) };
            field.Children.Add(Label(T(label), true, 11)); field.Children.Add(input); fields.Children.Add(field);
        }
        var offset = new ComboBox { DisplayMemberPath = "Name", SelectedValuePath = "Minutes", Margin = new Thickness(0, 0, 0, 10) };
        offset.ItemsSource = Enumerable.Range(-48, 105).Select(i => new { Minutes = i * 15, Name = OffsetLabel(i * 15) }).ToArray();
        offset.SelectedValue = settings.UtcOffsetMinutes; AutomationProperties.SetName(offset, T("utcOffset")); Field("configClock", offset);
        var reset = Input(T("resetTime"), ActivitySchedule.FormatMinute(settings.ResetMinute), 5); Field("configReset", reset);
        var day = new ComboBox { ItemsSource = Enum.GetValues<DayOfWeek>().Select(d => new { Day = d, Name = Culture.DateTimeFormat.DayNames[(int)d] }), DisplayMemberPath = "Name", SelectedValuePath = "Day", SelectedValue = settings.WeeklyResetDay, Margin = new Thickness(0, 0, 0, 10) };
        AutomationProperties.SetName(day, T("weeklyDay")); Field("configWeek", day); details.Children.Add(fields);
        details.Children.Add(Label(T("resetEvidence"), true, 11));
        var error = Label("", true); error.Visibility = Visibility.Collapsed; details.Children.Add(error);
        details.Children.Add(Button(T("apply"), () =>
        {
            if (!ActivitySchedule.TryMinutes(reset.Text, out var resets) || resets.Length != 1
                || offset.SelectedValue is not int utc || day.SelectedValue is not DayOfWeek weekday)
            { error.Text = T("scheduleError"); error.Visibility = Visibility.Visible; return; }
            controller.Change(controller.Data with { Settings = controller.Data.Settings with { UtcOffsetMinutes = utc, ResetMinute = resets[0], WeeklyResetDay = weekday } });
        }));
        panel.Children.Add(new Expander { Header = T("reminderSettings"), Content = details, Margin = new Thickness(0, 6, 0, 0), FontSize = 12 });
        root.Children.Add(Card(panel));
    }

    private void EditEvent(ScheduledActivity? activity, StackPanel host)
    {
        host.Children.Clear();
        var panel = new StackPanel(); Section(panel, T(activity is null ? "addEvent" : "editEvent"));
        panel.Children.Add(Label(T("eventName"))); var name = Input(T("eventName"), activity is null ? "" : ActivityName(activity)); panel.Children.Add(name);
        panel.Children.Add(Label(T("eventTimes") + " · " + OffsetLabel(controller.Data.Settings.UtcOffsetMinutes)));
        var times = Input(T("eventTimes"), activity is null ? "21:00" : string.Join(", ", activity.Minutes.Select(ActivitySchedule.FormatMinute)), 166); panel.Children.Add(times);
        var days = new WrapPanel(); var choices = new List<(DayOfWeek Day, CheckBox Check)>();
        foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(d => ((int)d + 6) % 7))
        {
            var check = new CheckBox { Content = Culture.DateTimeFormat.AbbreviatedDayNames[(int)day], IsChecked = activity is null || activity.Days.Contains(day), Margin = new Thickness(0, 6, 12, 10) };
            choices.Add((day, check)); days.Children.Add(check);
        }
        panel.Children.Add(days);
        var error = Label("", true); panel.Children.Add(error);
        var actions = new WrapPanel();
        actions.Children.Add(Button(T("apply"), () =>
        {
            var selectedDays = choices.Where(c => c.Check.IsChecked == true).Select(c => c.Day).ToArray();
            if (string.IsNullOrWhiteSpace(name.Text) || !ActivitySchedule.TryMinutes(times.Text, out var minutes) || selectedDays.Length == 0)
            { error.Text = T("eventError"); return; }
            var value = new ScheduledActivity(activity?.Id ?? Guid.NewGuid().ToString("N"), "", name.Text.Trim(), minutes, selectedDays, activity?.Notify ?? true, "custom", "");
            if (activity is null && controller.Data.Events.Length >= 100) { error.Text = T("activityLimit"); return; }
            controller.Change(controller.Data with { Events = activity is null ? [.. controller.Data.Events, value] : controller.Data.Events.Select(e => e.Id == activity.Id ? value : e).ToArray() });
        }));
        actions.Children.Add(Button(T("cancelActivity"), () => host.Children.Clear()));
        if (activity is not null && activity.Reliability == "custom") actions.Children.Add(Button(T("deleteActivity"), () => controller.Change(controller.Data with { Events = controller.Data.Events.Where(e => e.Id != activity.Id).ToArray() })));
        panel.Children.Add(actions); host.Children.Add(Card(panel)); host.BringIntoView(); name.Focus();
    }
}
