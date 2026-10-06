using System.IO;
using System.Text.Json;
using Spike.Core;

internal static class ActivityChecks
{
    public static void Run(Action<bool, string> check)
    {
        var data = ActivityCatalog.Create();
        ActivityStore.Validate(data);
        var settings = data.Settings;
        var tuesday = DateTimeOffset.Parse("2026-10-06T06:59:59Z");
        var daily = data.Tasks.Single(t => t.Id == "shugo");
        var weekly = data.Tasks.Single(t => t.Id == "dungeons");
        check(ActivitySchedule.NextReset(tuesday, settings, ActivityPeriod.Daily) == DateTimeOffset.Parse("2026-10-06T07:00:00Z"), "Global daily reset uses the configured UTC time, not PC midnight");
        var wednesday = DateTimeOffset.Parse("2026-10-07T07:00:00Z");
        check(ActivitySchedule.Boundary(wednesday, settings, ActivityPeriod.Weekly) == wednesday
            && ActivitySchedule.NextReset(wednesday, settings, ActivityPeriod.Weekly) == wednesday.AddDays(7), "Weekly reset changes at the exact Wednesday boundary");
        check(ActivitySchedule.NextReset(wednesday.AddSeconds(-1), settings, ActivityPeriod.Weekly) == wednesday, "Last second before weekly reset stays in the previous week");
        data = data with { Profiles = [new("main", "", new() { [daily.Id] = new(1, tuesday), [weekly.Id] = new(8, tuesday) }), new("alt", "Alt", new())] };
        check(ActivitySchedule.Count(data, daily, tuesday) == 1, "Daily checklist preserves partial progress");
        check(ActivitySchedule.Count(data, daily, tuesday.AddSeconds(1)) == 0 && ActivitySchedule.Count(data, weekly, tuesday.AddSeconds(1)) == 8, "Daily reset does not clear weekly progress");
        check(ActivitySchedule.Count(data, weekly, wednesday) == 0 && ActivitySchedule.Count(data, weekly, wednesday.AddDays(30)) == 0, "Weekly progress expires even when the application is closed for weeks");
        check(ActivitySchedule.Count(data with { ActiveProfile = "alt" }, daily, tuesday) == 0, "Characters have independent counters");
        check(ActivitySchedule.Count(data, daily with { Goal = 1 }, tuesday) == 1, "Reduced goals clamp previously saved counts");
        check(ActivitySchedule.Count(data, daily, tuesday.AddHours(-1)) == 0, "Future-dated completions are not treated as completed after a clock rollback");
        var korea = settings with { UtcOffsetMinutes = 540, ResetMinute = 300 };
        var koreaReset = DateTimeOffset.Parse("2026-10-06T20:00:00Z");
        check(ActivitySchedule.Boundary(koreaReset, korea, ActivityPeriod.Weekly) == koreaReset, "Weekly reset uses the reference weekday, including UTC date rollover");
        var paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var summer = TimeZoneInfo.ConvertTime(ActivitySchedule.NextReset(DateTimeOffset.Parse("2026-10-24T00:00:00Z"), settings, ActivityPeriod.Daily), paris);
        var winter = TimeZoneInfo.ConvertTime(ActivitySchedule.NextReset(DateTimeOffset.Parse("2026-10-25T00:00:00Z"), settings, ActivityPeriod.Daily), paris);
        check(summer.Hour == 9 && winter.Hour == 8, "Local reset display follows daylight saving without moving the UTC schedule");
        var rift = data.Events.Single(e => e.Id == "rift");
        check(ActivitySchedule.Next(rift, settings, DateTimeOffset.Parse("2026-10-06T23:59:59Z")).StartsAt == DateTimeOffset.Parse("2026-10-07T00:00:00Z"), "Rift schedule crosses midnight correctly");
        var siege = data.Events.Single(e => e.Id == "siege");
        check(ActivitySchedule.Next(siege, settings, DateTimeOffset.Parse("2026-10-03T21:00:01Z")).StartsAt == DateTimeOffset.Parse("2026-10-05T21:00:00Z"), "Weekly event skips unselected days and crosses Sunday");
        check(ActivitySchedule.Next(siege with { Minutes = [30], Days = [DayOfWeek.Wednesday] }, settings with { UtcOffsetMinutes = 120 }, tuesday).StartsAt == DateTimeOffset.Parse("2026-10-06T22:30:00Z"), "Custom offset is applied to event weekdays before conversion");
        check(ActivitySchedule.TryMinutes("03:00, 00:00, 03:00", out var parsed) && parsed.SequenceEqual(new[] { 0, 180 }), "Schedule editor deduplicates and sorts strict HH:mm times");
        foreach (var invalid in new[] { "", "24:00", "03:60", "3pm", "3:00", "03:00,", "-1:00" })
            check(!ActivitySchedule.TryMinutes(invalid, out _), "Reject invalid schedule time: " + invalid);
        var clock = DateTimeOffset.Parse("2026-10-06T11:54:59Z");
        data = data with { Settings = settings with { Notifications = true } };
        var reminders = new ActivityReminders();
        check(reminders.Poll(data, clock).Length == 0, "No notification before the configured lead time");
        var due = reminders.Poll(data, clock.AddSeconds(2));
        check(due.Length == 2 && due.Select(o => o.Activity.Id).Order().SequenceEqual(new[] { "rift", "shugo" }), "Simultaneous Rift and Shugo reminders are grouped and tolerate timer drift");
        var delivered = due.ToDictionary(o => o.Key, _ => clock.AddSeconds(2));
        data = data with { Delivered = delivered };
        check(reminders.Poll(data, clock.AddSeconds(3)).Length == 0 && new ActivityReminders().Poll(data, clock.AddSeconds(3)).Length == 0, "Persisted occurrence IDs prevent repeats across ticks and restarts");
        var sleep = new ActivityReminders(); sleep.Poll(data, clock);
        check(sleep.Poll(data, clock.AddMinutes(20)).Length == 0, "Resuming from sleep does not replay stale reminders");
        check(new ActivityReminders().Poll(data with { Settings = settings }, clock.AddSeconds(2)).Length == 0, "Master notification switch wins over per-event selection");
        check(new ActivityReminders().Poll(data with { Delivered = new(), Events = [rift with { Notify = false }] }, clock.AddSeconds(2)).Length == 0, "Disabled event never notifies");
        check(new ActivityReminders().Poll(data with { Settings = data.Settings with { LeadMinutes = 0 }, Delivered = new() }, DateTimeOffset.Parse("2026-10-06T12:00:01Z")).Length == 2, "Zero-minute reminders fire even when a tick is one second late");
        check(data.Events.Where(e => e.Reliability == "uncertain").All(e => !e.Notify), "Unconfirmed boss and invasion presets are opt-in");

        var errors = new List<string>();
        var controller = new Spike.Desktop.ActivityController(true, errors.Add) { Clock = () => clock };
        var firstView = 0; var secondView = 0;
        controller.Changed += () => firstView++;
        controller.Changed += () => secondView++;
        controller.SetCount(daily, 1);
        check(firstView == 1 && secondView == 1 && ActivitySchedule.Count(controller.Data, daily, clock) == 1,
            "One persisted checklist mutation updates both subscribers with the same state");
        var beforeInvalid = controller.Data;
        check(!controller.Change(controller.Data with { Settings = settings with { LeadMinutes = 100 } })
            && ReferenceEquals(beforeInvalid, controller.Data) && errors.Count == 1,
            "Failed activity mutations keep the last good state and notify the views of the error");
        controller.Change(controller.Data with { Settings = settings with { Notifications = true } });
        var batches = 0;
        controller.Notify += values => { batches++; check(values.Length == 2, "Controller groups simultaneous notifications into one batch"); };
        controller.Tick();
        controller.Clock = () => clock.AddSeconds(2);
        var beforeDelivery = firstView;
        controller.Tick(); controller.Tick();
        check(batches == 1 && controller.Data.Delivered.Count == 2, "Controller persists delivery before emitting a single notification");
        check(firstView == beforeDelivery && firstView == secondView, "Reminder delivery does not rebuild views or discard an open editor");
        controller.Clock = () => wednesday;
        controller.Tick();
        check(ActivitySchedule.Count(controller.Data, daily, wednesday) == 0 && firstView == beforeDelivery + 1,
            "Controller refreshes checklist views once when the reset period changes");

        var directory = Path.Combine(Path.GetTempPath(), "spike-activities-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "activities.json");
        var store = new ActivityStore(path);
        try
        {
            check(store.Load().Tasks.Length == 13, "First use creates daily and weekly templates without writing a file");
            store.Save(data);
            var reopened = store.Load();
            check(reopened.Profiles.Length == 2 && reopened.Delivered.Count == 2 && reopened.Profiles[0].Completed[weekly.Id].Count == 8, "Atomic storage retains profiles, partial progress, schedules and reminder history");
            check(Directory.GetFiles(directory).Length == 1, "Atomic saves do not leave temporary files");
            void Reject(ActivityData invalid, string label)
            {
                try { store.Save(invalid); }
                catch (InvalidDataException) { check(store.Load().Tasks.Length == 13, label); return; }
                throw new Exception("Accepted invalid activities: " + label);
            }
            Reject(data with { Version = 2 }, "Unsupported schema cannot overwrite a valid save");
            Reject(data with { Settings = settings with { UtcOffsetMinutes = 900 } }, "Reject out-of-range time zone");
            Reject(data with { Settings = settings with { ResetMinute = 1440 } }, "Reject invalid reset hour");
            Reject(data with { Settings = settings with { LeadMinutes = -1 } }, "Reject negative notification lead");
            Reject(data with { Events = [rift with { Days = [] }] }, "Reject events without weekdays");
            Reject(data with { Events = [rift, rift] }, "Reject duplicate event identifiers");
            Reject(data with { Tasks = [daily with { Goal = 0 }] }, "Reject empty checklist goals");
            Reject(data with { ActiveProfile = "missing" }, "Reject a nonexistent active character");
            File.WriteAllText(path, "{broken");
            try { store.Load(); throw new Exception("Corrupt activities accepted"); }
            catch (JsonException) { check(File.ReadAllText(path) == "{broken", "Loading corrupt data preserves the original file"); }
            File.WriteAllText(path, "{\"Version\":1}");
            try { store.Load(); throw new Exception("Incomplete activities accepted"); }
            catch (InvalidDataException) { check(true, "Incomplete persisted state fails validation cleanly"); }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
