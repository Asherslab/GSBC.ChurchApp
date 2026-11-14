using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventDestinationSource(EventsDbContext db, TimeProvider time) : IDestinationSource
{
    public DestinationKind Kind => EventsDestinations.Event;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        (DateOnly from, DateOnly to) = EventsBook.Window(time);
        return book.Occurrences(from, to, item => EventsBook.IsShown(item, now))
            .Where(occurrence => occurrence.EndsAt >= now && !occurrence.NotOn)
            .Where(occurrence => text.Length == 0 || occurrence.Fields.Title.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(occurrence => occurrence.StartsAt)
            .DistinctBy(occurrence => occurrence.Kind == OccurrenceKind.Regular ? occurrence.ItemId : occurrence.Id)
            .Take(take)
            .Select(occurrence => Place(book, occurrence))
            .ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken)
    {
        if (!EventDates.TryReadOccurrenceId(id, out Guid itemId, out DateOnly? date))
            return null;
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateOnly day = date ?? book.Events.FirstOrDefault(record => record.Id == itemId)?.StartDate ?? DateOnly.MinValue;
        return book.Occurrences(day, day, item => item.Id == itemId && EventsBook.IsShown(item, now))
            .FirstOrDefault(occurrence => occurrence.Id == id) is { } found
            ? Place(book, found)
            : null;
    }

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    internal static FoundPlace Place(EventsBook book, EventOccurrence occurrence)
    {
        string calendar = Guid.TryParse(occurrence.CalendarId, out Guid id) && book.Calendars.TryGetValue(id, out CalendarRecord? record) ? record.Name : "Event";
        return new FoundPlace(occurrence.Id, occurrence.Fields.Title, calendar, book.AudienceOf(occurrence), When: occurrence.StartsAt);
    }
}

internal sealed class CalendarDestinationSource(EventsDbContext db) : IDestinationSource
{
    public DestinationKind Kind => EventsDestinations.Calendar;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        List<CalendarRecord> found = await db.Calendars.AsNoTracking()
            .Where(calendar => text.Length == 0 || EF.Functions.ILike(calendar.Name, Like(text)))
            .OrderBy(calendar => calendar.Order).ThenBy(calendar => calendar.Name).Take(take).ToListAsync(cancellationToken);
        return found.Select(Place).ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid calendarId) && await db.Calendars.AsNoTracking().SingleOrDefaultAsync(calendar => calendar.Id == calendarId, cancellationToken) is { } found
            ? Place(found)
            : null;

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    private static FoundPlace Place(CalendarRecord calendar) =>
        new(calendar.Id.ToString(), calendar.Name, "Calendar", CalendarAudiences.Of(calendar.Audience));

    private static string Like(string text) =>
        $"%{text.Replace("%", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal)}%";
}
