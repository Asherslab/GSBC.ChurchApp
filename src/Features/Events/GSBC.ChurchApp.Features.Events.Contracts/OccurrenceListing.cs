namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class OccurrenceListing
{
    public static bool IsOneOffOrSpecial(EventOccurrence occurrence) => occurrence.Kind != OccurrenceKind.Regular;

    public static bool IsListedOnTheWebsite(EventOccurrence occurrence) => IsOneOffOrSpecial(occurrence) || occurrence.ListEveryDate;
}
