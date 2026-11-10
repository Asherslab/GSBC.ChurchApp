using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed record KeptShape(RegularPattern Pattern, int? StartMinute, int? EndMinute, IReadOnlyList<SpecialRecord> Specials, IReadOnlyList<BreakRecord> Breaks)
{
    private const int DaysInAWeek = 7;

    public static KeptShape Of(Guid regularEventId, KeptRegularEvent wanted, DateTime now)
    {
        Dictionary<DateOnly, KeptDate> on = wanted.On.GroupBy(date => date.Date).ToDictionary(group => group.Key, group => group.First());
        Dictionary<DateOnly, string> notOn = wanted.NotOn.Where(gap => !on.ContainsKey(gap.Date)).GroupBy(gap => gap.Date)
            .ToDictionary(group => group.Key, group => group.First().Why);
        List<DateOnly> dates = on.Keys.Concat(notOn.Keys).Order().ToList();

        (int Start, int End)? usual = on.Values.GroupBy(Minutes).OrderByDescending(group => group.Count()).ThenBy(group => group.Min(date => date.Date))
            .Select(group => ((int, int)?)group.Key).FirstOrDefault();
        List<SpecialRecord> specials = on.Values.Where(date => Minutes(date) != usual).OrderBy(date => date.Date).Select(date => new SpecialRecord
        {
            RegularEventId = regularEventId, Date = date.Date, Changed = [EventFieldKeys.Time], StartMinute = Minutes(date).Start, EndMinute = Minutes(date).End, UpdatedAt = now,
        }).ToList();

        return new KeptShape(PatternOf(dates), usual?.Start, usual?.End, specials, BreaksOf(regularEventId, dates, notOn, now));
    }

    private static (int Start, int End) Minutes(KeptDate date) => (date.Starts.Hour * 60 + date.Starts.Minute, date.Ends.Hour * 60 + date.Ends.Minute);

    private static RegularPattern PatternOf(List<DateOnly> dates) =>
        dates.Count > 0 && dates.Zip(dates.Skip(1)).All(pair => pair.Second.DayNumber - pair.First.DayNumber == DaysInAWeek)
            ? new RegularPattern { Repeats = Repeats.Weekly, Days = [dates[0].DayOfWeek], StartDate = EventDates.Format(dates[0]), EndDate = EventDates.Format(dates[^1]) }
            : new RegularPattern
            {
                Repeats = Repeats.OnDates, Dates = dates.Select(EventDates.Format).ToList(),
                StartDate = dates.Count > 0 ? EventDates.Format(dates[0]) : "", EndDate = dates.Count > 0 ? EventDates.Format(dates[^1]) : "",
            };

    private static List<BreakRecord> BreaksOf(Guid regularEventId, List<DateOnly> dates, Dictionary<DateOnly, string> notOn, DateTime now)
    {
        List<BreakRecord> breaks = [];
        BreakRecord? open = null;
        foreach (DateOnly date in dates)
        {
            if (!notOn.TryGetValue(date, out string? why))
            {
                open = null;
                continue;
            }
            if (open is not null && open.Name == why)
            {
                open.EndDate = date;
                continue;
            }
            open = new BreakRecord { Id = Guid.CreateVersion7(), Name = why, StartDate = date, EndDate = date, RegularEventId = regularEventId, CreatedAt = now };
            breaks.Add(open);
        }
        return breaks;
    }
}
