using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsBook
{
    private readonly ILookup<Guid, SpecialRecord> _specials;
    private readonly ILookup<Guid, BreakRecord> _breaks;
    private readonly ILookup<Guid, ShowingRecord> _showings;

    public EventsBook(
        IReadOnlyList<CalendarRecord> calendars, IReadOnlyList<EventRecord> events, IReadOnlyList<RegularEventRecord> regularEvents,
        IReadOnlyList<SpecialRecord> specials, IReadOnlyList<BreakRecord> breaks, IReadOnlyList<RegularEventBreak> links, IReadOnlyList<ShowingRecord> showings)
    {
        Calendars = calendars.ToDictionary(calendar => calendar.Id);
        Events = events;
        RegularEvents = regularEvents;
        _specials = specials.ToLookup(special => special.RegularEventId);
        Dictionary<Guid, BreakRecord> named = breaks.Where(stop => stop.RegularEventId is null).ToDictionary(stop => stop.Id);
        _breaks = breaks.Where(stop => stop.RegularEventId is not null).Select(stop => (stop.RegularEventId!.Value, stop))
            .Concat(links.Where(link => named.ContainsKey(link.BreakId)).Select(link => (link.RegularEventId, named[link.BreakId])))
            .ToLookup(pair => pair.Item1, pair => pair.Item2);
        _showings = showings.ToLookup(showing => showing.EventId ?? showing.RegularEventId ?? Guid.Empty);
    }

    public IReadOnlyDictionary<Guid, CalendarRecord> Calendars { get; }

    public IReadOnlyList<EventRecord> Events { get; }

    public IReadOnlyList<RegularEventRecord> RegularEvents { get; }

    public static async Task<EventsBook> LoadAsync(EventsDbContext db, CancellationToken cancellationToken) =>
        new(await db.Calendars.AsNoTracking().ToListAsync(cancellationToken),
            await db.Events.AsNoTracking().ToListAsync(cancellationToken),
            await db.RegularEvents.AsNoTracking().ToListAsync(cancellationToken),
            await db.Specials.AsNoTracking().ToListAsync(cancellationToken),
            await db.Breaks.AsNoTracking().ToListAsync(cancellationToken),
            await db.RegularEventBreaks.AsNoTracking().ToListAsync(cancellationToken),
            await db.Showings.AsNoTracking().ToListAsync(cancellationToken));

    public static (DateOnly From, DateOnly To) Window(TimeProvider time)
    {
        DateOnly today = ChurchTime.Today(time);
        return (today.AddDays(-EventsFeeds.DaysBack), today.AddMonths(EventsFeeds.MonthsAhead));
    }

    public static bool IsShown(IShowable item, DateTime now) =>
        item.Showing == Showing.Shown || (item.Showing == Showing.ShownFrom && item.ShowFrom is { } from && from <= now);

    public IReadOnlyList<SpecialRecord> SpecialsOf(Guid regularEventId) => _specials[regularEventId].ToList();

    public IReadOnlyList<BreakRecord> BreaksOf(Guid regularEventId) => _breaks[regularEventId].ToList();

    public IReadOnlyList<Guid> AlsoOn(Guid itemId) =>
        _showings[itemId].Where(showing => showing.AgreedAt is not null).Select(showing => showing.CalendarId).ToList();

    public IReadOnlyList<ShowingRecord> ShowingsOf(Guid itemId) => _showings[itemId].ToList();

    public Audience AudienceOf(EventOccurrence occurrence) =>
        CalendarAudiences.Union(new[] { occurrence.CalendarId }.Concat(occurrence.AlsoOn)
            .Select(id => Guid.TryParse(id, out Guid calendarId) ? Calendars.GetValueOrDefault(calendarId) : null)
            .OfType<CalendarRecord>()
            .Select(calendar => CalendarAudiences.Of(calendar.Audience)));

    public IEnumerable<EventOccurrence> Occurrences(DateOnly from, DateOnly to, Func<IShowable, bool> include)
    {
        foreach (RegularEventRecord regular in RegularEvents.Where(regular => Calendars.ContainsKey(regular.CalendarId) && include(regular)))
        {
            Dictionary<DateOnly, SpecialRecord> specials = _specials[regular.Id].ToDictionary(special => special.Date);
            foreach (EventOccurrence occurrence in Expansion.OccurrencesOf(regular, specials, BreaksOf(regular.Id), from, to))
                yield return Stamped(occurrence, regular);
        }

        foreach (EventRecord record in Events.Where(record => Calendars.ContainsKey(record.CalendarId) && include(record)
                     && Expansion.LastDayOf(record) >= from && record.StartDate <= to))
            yield return Stamped(Expansion.OccurrenceOf(record), record);
    }

    private EventOccurrence Stamped(EventOccurrence occurrence, IShowable item)
    {
        occurrence.AlsoOn = AlsoOn(item.Id).Where(Calendars.ContainsKey).Select(id => id.ToString()).ToList();
        occurrence.Showing = item.Showing;
        occurrence.ShowFrom = item.ShowFrom;
        occurrence.Asked = item.AskedAt is not null;
        return occurrence;
    }
}
