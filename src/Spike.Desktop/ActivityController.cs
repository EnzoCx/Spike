using System.IO;
using System.Text.Json;
using Spike.Core;

namespace Spike.Desktop;

internal sealed class ActivityController
{
    private readonly ActivityStore? store;
    private readonly ActivityReminders reminders = new();
    private readonly Action<string> error;
    private bool readable = true;
    private string resetPeriod = "";
    public ActivityData Data { get; private set; } = ActivityCatalog.Create();
    public Func<DateTimeOffset> Clock { get; set; } = () => DateTimeOffset.UtcNow;
    public event Action? Changed;
    public event Action<ActivityOccurrence[]>? Notify;
    public string? ErrorKey { get; private set; }

    public ActivityController(bool verification, Action<string> error)
    {
        this.error = error;
        if (verification) return;
        store = new(Path.Combine(ApplicationData.Root, "activities.json"));
        try { Data = store.Load(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        { readable = false; ErrorKey = "activitiesLoadError"; }
    }

    public bool Change(ActivityData value, bool notifyViews = true)
    {
        if (!readable) { error("activitiesLoadError"); Changed?.Invoke(); return false; }
        try
        {
            ActivityStore.Validate(value);
            store?.Save(value);
            Data = value;
            ErrorKey = null;
            if (notifyViews) Changed?.Invoke();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        { ErrorKey = "saveError"; error("saveError"); Changed?.Invoke(); return false; }
    }

    public bool SetCount(ChecklistActivity task, int count)
    {
        var profile = Data.Profiles.Single(p => p.Id == Data.ActiveProfile);
        var completed = new Dictionary<string, ActivityCompletion>(profile.Completed)
        { [task.Id] = new(Math.Clamp(count, 0, task.Goal), Clock()) };
        return Change(Data with { Profiles = Data.Profiles.Select(p => p.Id == profile.Id ? p with { Completed = completed } : p).ToArray() });
    }

    public void Tick()
    {
        var now = Clock();
        var period = ActivitySchedule.Boundary(now, Data.Settings, ActivityPeriod.Daily).ToString("O");
        if (period != resetPeriod) { resetPeriod = period; Changed?.Invoke(); }
        var due = reminders.Poll(Data, now);
        if (due.Length == 0) return;
        var delivered = Data.Delivered.Where(d => d.Value > now.AddDays(-2)).ToDictionary(d => d.Key, d => d.Value);
        foreach (var occurrence in due) delivered[occurrence.Key] = now;
        // Persist before displaying: opening another view or restarting cannot notify twice.
        if (Change(Data with { Delivered = delivered }, notifyViews: false)) Notify?.Invoke(due);
    }
}
