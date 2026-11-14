using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Content;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventCardSource(EventsDbContext db, TimeProvider time, IEnumerable<IDestinationSource> destinations) : IAppCardSource
{
    public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Event];

    public async Task<FoundPlace?> FindAsync(AppCard card, CancellationToken cancellationToken)
    {
        if (card.Shows == EventShows.OneDate)
            return await destinations.Single(source => source.Kind == EventsDestinations.Event).FindAsync(card.Id, cancellationToken);
        if (!EventDates.TryReadOccurrenceId(card.Id, out Guid itemId, out DateOnly? date) || date is not null)
            return null;
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        if (book.RegularEvents.All(regular => regular.Id != itemId))
            return null;
        DateTime now = time.GetUtcNow().UtcDateTime;
        (DateOnly from, DateOnly to) = EventsBook.Window(time);
        List<EventOccurrence> dates = book.Occurrences(from, to, item => item.Id == itemId && EventsBook.IsShown(item, now)).OrderBy(occurrence => occurrence.StartsAt).ToList();
        return (dates.FirstOrDefault(occurrence => occurrence.EndsAt >= now) ?? dates.LastOrDefault()) is { } shown ? EventDestinationSource.Place(book, shown) : null;
    }
}
