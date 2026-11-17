using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsForEveryone(EventsDbContext db, TimeProvider time)
{
    public async Task<PublicEvents> ReadAsync(CancellationToken cancellationToken)
    {
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        PublicEvents events = new();
        foreach (EventsPublisher.Row row in EventsPublisher.Rows(book, time.GetUtcNow().UtcDateTime))
        {
            if (row.Feed == EventsFeeds.Calendars)
                events.Calendars.Add(EventsPayloads.Decode<CalendarInfo>(row.Payload));
            else if (row.Feed == EventsFeeds.Public)
                events.Occurrences.Add(EventsPayloads.Decode<EventOccurrence>(row.Payload));
        }
        events.Occurrences.Sort((first, second) => first.StartsAt.CompareTo(second.StartsAt));
        return events;
    }
}
