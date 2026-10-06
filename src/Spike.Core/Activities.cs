using System.Globalization;
using System.Text.Json;

namespace Spike.Core;

public enum ActivityPeriod { Daily, Weekly }

public sealed record ScheduledActivity(string Id, string NameKey, string CustomName, int[] Minutes,
    DayOfWeek[] Days, bool Notify, string Reliability = "community", string Source = "https://shugo.gg/timers");
public sealed record ChecklistActivity(string Id, string NameKey, string CustomName, ActivityPeriod Period,
    int Goal = 1, bool Visible = true);
public sealed record ActivityCompletion(int Count, DateTimeOffset UpdatedAt);
public sealed record ActivityProfile(string Id, string Name, Dictionary<string, ActivityCompletion> Completed);
public sealed record ActivitySettings(bool Notifications = false, bool Sound = false, int LeadMinutes = 5,
    int UtcOffsetMinutes = 0, int ResetMinute = 420, DayOfWeek WeeklyResetDay = DayOfWeek.Wednesday);
public sealed record ActivityData(int Version, ActivitySettings Settings, ScheduledActivity[] Events,
    ChecklistActivity[] Tasks, ActivityProfile[] Profiles, string ActiveProfile,
    Dictionary<string, DateTimeOffset> Delivered);
public sealed record ActivityOccurrence(ScheduledActivity Activity, DateTimeOffset StartsAt)
{
    public string Key => Activity.Id + ":" + StartsAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
}

/// <summary>Offline Global presets, reviewed 2026-10-06. See docs/ACTIVITIES.md for evidence and conflicts.</summary>
public static class ActivityCatalog
{
    public static ActivityData Create()
    {
        DayOfWeek[] daily = Enum.GetValues<DayOfWeek>();
        return new(1, new(),
        [
            new("shugo", "eventShugo", "", Enumerable.Range(0, 24).Select(h => h * 60).ToArray(), daily, true),
            new("rift", "eventRift", "", Enumerable.Range(0, 8).Select(h => h * 180).ToArray(), daily, true,
                Source: "https://aion2hub.com/tools/event-timer"),
            new("siege", "eventSiege", "", [1260], [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Saturday], false),
            new("siege-bosses", "eventSiegeBosses", "", [1290], [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Saturday], false),
            new("nahma", "eventNahma", "", [1260], [DayOfWeek.Friday, DayOfWeek.Sunday], false, "uncertain"),
            new("kaira", "eventKaira", "", Enumerable.Range(0, 8).Select(h => 60 + h * 180).ToArray(), daily, false, "uncertain"),
            new("invasion", "eventInvasion", "", Enumerable.Range(0, 24).Select(h => 30 + h * 60).ToArray(), daily, false, "uncertain")
        ],
        [
            new("quests", "taskQuests", "", ActivityPeriod.Daily),
            new("shugo", "taskShugo", "", ActivityPeriod.Daily, 2),
            new("nightmare", "taskNightmare", "", ActivityPeriod.Daily, 2),
            new("odyle", "taskOdyle", "", ActivityPeriod.Daily),
            new("abyss", "taskAbyss", "", ActivityPeriod.Weekly),
            new("altgard", "taskAltgard", "", ActivityPeriod.Weekly),
            new("craft", "taskCraft", "", ActivityPeriod.Weekly, 20),
            new("buy", "taskBuy", "", ActivityPeriod.Weekly, 20),
            new("dungeons", "taskDungeons", "", ActivityPeriod.Weekly, 14),
            new("tickets", "taskTickets", "", ActivityPeriod.Weekly, 7),
            new("season", "taskSeason", "", ActivityPeriod.Weekly),
            new("trophies", "taskTrophies", "", ActivityPeriod.Weekly),
            new("ascension", "taskAscension", "", ActivityPeriod.Weekly)
        ], [new("main", "", new())], "main", new());
    }
}

public static class ActivitySchedule
{
    public static TimeSpan Offset(ActivitySettings settings) => TimeSpan.FromMinutes(settings.UtcOffsetMinutes);

    public static DateTimeOffset Boundary(DateTimeOffset now, ActivitySettings settings, ActivityPeriod period)
    {
        var local = now.ToOffset(Offset(settings));
        var reset = new DateTimeOffset(local.Date.AddMinutes(settings.ResetMinute), local.Offset);
        if (reset > now) reset = reset.AddDays(-1);
        if (period == ActivityPeriod.Weekly)
            reset = reset.AddDays(-((7 + (int)reset.DayOfWeek - (int)settings.WeeklyResetDay) % 7));
        return reset;
    }

    public static DateTimeOffset NextReset(DateTimeOffset now, ActivitySettings settings, ActivityPeriod period) =>
        Boundary(now, settings, period).AddDays(period == ActivityPeriod.Daily ? 1 : 7);

    public static ActivityOccurrence Next(ScheduledActivity activity, ActivitySettings settings, DateTimeOffset now)
    {
        var local = now.ToOffset(Offset(settings));
        for (var day = 0; day <= 7; day++)
        {
            var date = local.Date.AddDays(day);
            if (!activity.Days.Contains(date.DayOfWeek)) continue;
            foreach (var minute in activity.Minutes.Order())
            {
                var time = new DateTimeOffset(date.AddMinutes(minute), local.Offset);
                if (time >= now) return new(activity, time);
            }
        }
        throw new InvalidDataException("Event has no valid occurrence.");
    }

    public static int Count(ActivityData data, ChecklistActivity task, DateTimeOffset now)
    {
        var profile = data.Profiles.Single(p => p.Id == data.ActiveProfile);
        return profile.Completed.TryGetValue(task.Id, out var value)
            && value.UpdatedAt >= Boundary(now, data.Settings, task.Period) && value.UpdatedAt <= now
                ? Math.Clamp(value.Count, 0, task.Goal) : 0;
    }

    public static bool TryMinutes(string input, out int[] minutes)
    {
        var result = new List<int>();
        foreach (var part in input.Split(',', StringSplitOptions.TrimEntries))
        {
            if (!TimeOnly.TryParseExact(part, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            { minutes = []; return false; }
            result.Add(value.Hour * 60 + value.Minute);
        }
        minutes = result.Distinct().Order().ToArray();
        return minutes.Length is > 0 and <= 24;
    }

    public static string FormatMinute(int value) => $"{value / 60:00}:{value % 60:00}";
}

/// <summary>One scheduler per application, independent of capture and overlay visibility.</summary>
public sealed class ActivityReminders
{
    private DateTimeOffset? lastPoll;
    public ActivityOccurrence[] Poll(ActivityData data, DateTimeOffset now)
    {
        // First launch, sleep and clock jumps never replay a backlog. Permit a one-minute scheduling delay.
        var since = lastPoll is { } previous && previous <= now && now - previous < TimeSpan.FromMinutes(1)
            ? previous : now.AddMinutes(-1);
        lastPoll = now;
        if (!data.Settings.Notifications) return [];
        return data.Events.Where(e => e.Notify).Select(e => ActivitySchedule.Next(e, data.Settings, since.AddMinutes(data.Settings.LeadMinutes)))
            .Where(o => o.StartsAt.AddMinutes(-data.Settings.LeadMinutes) > since
                && o.StartsAt.AddMinutes(-data.Settings.LeadMinutes) <= now
                && !data.Delivered.ContainsKey(o.Key)).OrderBy(o => o.StartsAt).ToArray();
    }
}

public sealed class ActivityStore(string path)
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public ActivityData Load()
    {
        if (!File.Exists(path)) return ActivityCatalog.Create();
        if (new FileInfo(path).Length > MaximumBytes) throw new InvalidDataException("Activities file too large.");
        var data = JsonSerializer.Deserialize<ActivityData>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException("Missing activities.");
        Validate(data);
        return data;
    }

    public void Save(ActivityData data)
    {
        Validate(data);
        var json = JsonSerializer.Serialize(data, Json);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new InvalidDataException("Activities file too large.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, json); File.Move(temporary, path, overwrite: true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static void Validate(ActivityData data)
    {
        static bool Id(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 80;
        static bool Label(string? value) => value is not null && value.Length <= 120 && !value.Any(char.IsControl);
        var s = data.Settings;
        if (data.Version != 1 || s is null || s.UtcOffsetMinutes is < -720 or > 840 || s.UtcOffsetMinutes % 15 != 0
            || s.ResetMinute is < 0 or >= 1440 || s.LeadMinutes is < 0 or > 60 || !Enum.IsDefined(s.WeeklyResetDay)
            || data.Events is null || data.Tasks is null || data.Profiles is null || data.Delivered is null
            || data.Events.Length > 100 || data.Tasks.Length > 100 || data.Profiles.Length is < 1 or > 20)
            throw new InvalidDataException("Invalid activities settings.");
        foreach (var e in data.Events)
            if (e is null || !Id(e.Id) || !Label(e.NameKey) || !Label(e.CustomName) || (e.NameKey == "" && string.IsNullOrWhiteSpace(e.CustomName))
                || e.Minutes is null || e.Minutes.Length is < 1 or > 24 || e.Minutes.Any(m => m is < 0 or >= 1440)
                || e.Days is null || e.Days.Length is < 1 or > 7 || e.Days.Any(d => !Enum.IsDefined(d))
                || e.Reliability is not ("community" or "uncertain" or "custom") || e.Source is null || e.Source.Length > 500)
                throw new InvalidDataException("Invalid event.");
        foreach (var t in data.Tasks)
            if (t is null || !Id(t.Id) || !Label(t.NameKey) || !Label(t.CustomName) || (t.NameKey == "" && string.IsNullOrWhiteSpace(t.CustomName))
                || !Enum.IsDefined(t.Period) || t.Goal is < 1 or > 999)
                throw new InvalidDataException("Invalid checklist item.");
        foreach (var p in data.Profiles)
            if (p is null || !Id(p.Id) || !Label(p.Name) || p.Completed is null || p.Completed.Count > 100
                || p.Completed.Any(c => !Id(c.Key) || c.Value is null || c.Value.Count is < 0 or > 999))
                throw new InvalidDataException("Invalid character checklist.");
        if (data.Events.Select(e => e.Id).Distinct().Count() != data.Events.Length
            || data.Tasks.Select(t => t.Id).Distinct().Count() != data.Tasks.Length
            || data.Profiles.Select(p => p.Id).Distinct().Count() != data.Profiles.Length
            || !data.Profiles.Any(p => p.Id == data.ActiveProfile) || data.Delivered.Count > 10000)
            throw new InvalidDataException("Invalid or duplicate activities identifiers.");
    }
}
