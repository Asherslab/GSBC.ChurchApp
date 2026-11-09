namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class OccurrenceListing
{
    public static bool IsOneOffOrSpecial(EventOccurrence occurrence) => occurrence.Kind != OccurrenceKind.Regular;

    public static bool IsListedOnTheWebsite(EventOccurrence occurrence) => IsOneOffOrSpecial(occurrence) || occurrence.ListEveryDate;

    public static IEnumerable<EventOccurrence> OtherDates(EventOccurrence occurrence, IEnumerable<EventOccurrence> dates)
    {
        if (occurrence.Kind == OccurrenceKind.OneOff)
            return [];
        List<EventOccurrence> siblings = dates.Where(other => other.ItemId == occurrence.ItemId && other.Kind != OccurrenceKind.OneOff && other.Id != occurrence.Id).ToList();
        return HasItsOwnTitle(occurrence, siblings) ? [] : siblings;
    }

    private static bool HasItsOwnTitle(EventOccurrence occurrence, IEnumerable<EventOccurrence> siblings) =>
        occurrence.Kind == OccurrenceKind.Special
        && siblings.Any(other => other.Kind == OccurrenceKind.Regular && other.Fields.Title != occurrence.Fields.Title);
}
