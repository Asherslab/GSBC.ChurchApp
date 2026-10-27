using Gsbc.AppPoc.Data;

namespace Gsbc.AppPoc.Layouts.WeekFirst;

public sealed record WeekEntry(DateTime Start, DateTime End, string Title, string? Detail, string? Href, ServiceTime? Service)
{
    public IReadOnlyList<Sermon> Sermons { get; init; } = [];
}

public sealed record WeekDay(DateOnly Date, IReadOnlyList<WeekEntry> Entries, IReadOnlyList<Sermon> LooseSermons, Bulletin? Bulletin)
{
    public bool IsQuiet => Entries.Count == 0 && LooseSermons.Count == 0 && Bulletin is null;
}

public sealed class WeekHomePlan(SampleContent content, DateTime now)
{
    private const int DaysToNextSunday = 7;
    private const int Noon = 12;
    private const string Weekly = "Every";
    private const string MorningSermon = "Sunday AM";
    private const string EveningSermon = "Sunday PM";

    public DateOnly Today { get; } = DateOnly.FromDateTime(now);

    public DateOnly First => Today.AddDays(-(int)Today.DayOfWeek);

    public DateOnly Last => First.AddDays(DaysToNextSunday);

    public IReadOnlyList<WeekDay> Days(IReadOnlyList<Sermon> sermons) =>
        Enumerable.Range(0, DaysToNextSunday + 1).Select(offset => Plan(First.AddDays(offset), sermons)).ToList();

    public IReadOnlyList<ChurchEvent> ComingUp() =>
        content.Events.Where(e => !IsWeekly(e) && DateOnly.FromDateTime(e.Start) > Last).OrderBy(e => e.Start).ToList();

    private WeekDay Plan(DateOnly date, IReadOnlyList<Sermon> sermons)
    {
        var entries = Gatherings(date);
        var hosted = sermons.Where(s => s.Date == date).ToLookup(s => Host(s, entries));
        var placed = entries.Select(e => e with { Sermons = hosted[e].ToList() }).ToList();
        var bulletin = content.Bulletin.Date == date ? content.Bulletin : null;
        return new(date, placed, hosted[null].ToList(), bulletin);
    }

    private List<WeekEntry> Gatherings(DateOnly date)
    {
        var day = date.ToDateTime(TimeOnly.MinValue);
        var events = content.Events
            .Where(e => IsWeekly(e) ? e.Start.DayOfWeek == date.DayOfWeek && e.Start.Date <= day : e.Start.Date == day)
            .Select(e => new WeekEntry(day + e.Start.TimeOfDay, day + e.Start.TimeOfDay + e.Length, e.Title, e.Repeats, $"events/{e.Id}",
                content.Services.FirstOrDefault(s => s.Day == date.DayOfWeek && s.Name == e.Title)));
        var services = content.Services
            .Where(s => s.Day == date.DayOfWeek && content.Events.All(e => e.Title != s.Name))
            .Select(s => new WeekEntry(day + s.Start.ToTimeSpan(), day + s.Start.ToTimeSpan() + s.Length, s.Name, null, s.Href, s));
        return services.Concat(events).OrderBy(e => e.Start).ToList();
    }

    private static WeekEntry? Host(Sermon sermon, List<WeekEntry> entries)
    {
        var services = entries.Where(e => e.Service is not null).ToList();
        return sermon.Service switch
        {
            MorningSermon => services.FirstOrDefault(e => e.Service!.Streamed && e.Start.Hour < Noon),
            EveningSermon => services.LastOrDefault(e => e.Service!.Streamed && e.Start.Hour >= Noon),
            _ => services.LastOrDefault(e => e.Service!.Streamed) ?? services.LastOrDefault(),
        };
    }

    private static bool IsWeekly(ChurchEvent e) => e.Repeats?.StartsWith(Weekly, StringComparison.Ordinal) == true;
}
