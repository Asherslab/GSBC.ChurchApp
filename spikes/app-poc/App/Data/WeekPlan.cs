namespace Gsbc.AppPoc.Data;

public sealed record AgendaEntry(DateTime Start, string Title, string? Detail, string? Href, bool Streamed);

public sealed record DayPlan(DateOnly Date, IReadOnlyList<AgendaEntry> Entries);

public sealed class WeekPlan(SampleContent content, ChurchClock clock)
{
    private const int DaysShown = 7;
    private const string Weekly = "Every";

    public IReadOnlyList<DayPlan> Days()
    {
        var today = DateOnly.FromDateTime(clock.Now);
        return Enumerable.Range(0, DaysShown).Select(offset => Plan(today.AddDays(offset))).ToList();
    }

    public IReadOnlyList<ChurchEvent> ComingUp()
    {
        var windowEnd = clock.Now.Date.AddDays(DaysShown);
        return content.Events.Where(e => !IsWeekly(e) && e.Start >= windowEnd).OrderBy(e => e.Start).ToList();
    }

    private DayPlan Plan(DateOnly date)
    {
        var day = date.ToDateTime(TimeOnly.MinValue);
        var events = content.Events
            .Where(e => IsWeekly(e) ? e.Start.DayOfWeek == date.DayOfWeek && e.Start.Date <= day : e.Start.Date == day)
            .Select(e => new AgendaEntry(day + e.Start.TimeOfDay, e.Title, e.Repeats, $"events/{e.Id}", false));
        var services = content.Services
            .Where(s => s.Day == date.DayOfWeek && content.Events.All(e => e.Title != s.Name))
            .Select(s => new AgendaEntry(day + s.Start.ToTimeSpan(), s.Name, s.Streamed ? "Streamed on YouTube" : null, s.Href, s.Streamed));
        return new(date, services.Concat(events).OrderBy(e => e.Start).ToList());
    }

    private static bool IsWeekly(ChurchEvent e) => e.Repeats?.StartsWith(Weekly, StringComparison.Ordinal) == true;
}
