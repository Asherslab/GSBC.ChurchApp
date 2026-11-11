using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class RosterableEvents(EventsDbContext db, IAccessEvaluator access, TimeProvider time) : IRosterableEvents
{
    public async ValueTask<IReadOnlyList<RosterableEvent>> VisibleToAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        Viewer viewer = Viewer.Of(await access.GrantsForAsync(personId, cancellationToken));
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        return book.Occurrences(from, to, item => EventsBook.IsShown(item, now))
            .Where(occurrence => !occurrence.NotOn && book.AudienceOf(occurrence).Includes(viewer))
            .OrderBy(occurrence => occurrence.StartsAt)
            .Select(occurrence => Rosterable(book, occurrence))
            .ToList();
    }

    public async ValueTask<IReadOnlyDictionary<string, RosterableEvent>> FindAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        List<(string Key, Guid ItemId, DateOnly? Date)> wanted = [];
        foreach (string key in keys.Distinct())
            if (EventDates.TryReadOccurrenceId(key, out Guid itemId, out DateOnly? date))
                wanted.Add((key, itemId, date));
        if (wanted.Count == 0)
            return new Dictionary<string, RosterableEvent>();

        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        HashSet<Guid> items = wanted.Select(each => each.ItemId).ToHashSet();
        List<DateOnly> days = wanted
            .Select(each => each.Date ?? book.Events.FirstOrDefault(record => record.Id == each.ItemId)?.StartDate)
            .OfType<DateOnly>()
            .ToList();
        if (days.Count == 0)
            return new Dictionary<string, RosterableEvent>();

        HashSet<string> keysWanted = wanted.Select(each => each.Key).ToHashSet();
        return book.Occurrences(days.Min(), days.Max(), item => items.Contains(item.Id) && EventsBook.IsShown(item, now))
            .Where(occurrence => keysWanted.Contains(occurrence.Id))
            .DistinctBy(occurrence => occurrence.Id)
            .ToDictionary(occurrence => occurrence.Id, occurrence => Rosterable(book, occurrence));
    }

    private static RosterableEvent Rosterable(EventsBook book, EventOccurrence occurrence)
    {
        string calendar = Guid.TryParse(occurrence.CalendarId, out Guid id) && book.Calendars.TryGetValue(id, out CalendarRecord? record) ? record.Name : "";
        return new RosterableEvent(occurrence.Id, occurrence.Fields.Title, id, calendar, occurrence.StartsAt, occurrence.EndsAt, occurrence.AllDay, occurrence.NotOn);
    }
}
