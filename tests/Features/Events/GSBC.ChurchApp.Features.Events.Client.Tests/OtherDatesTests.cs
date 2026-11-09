using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class OtherDatesTests
{
    private readonly EventOccurrence _eighth = Date("2026-11-08", OccurrenceKind.Regular, "Morning service");
    private readonly EventOccurrence _anniversary = Date("2026-11-15", OccurrenceKind.Special, "Anniversary Sunday Service");
    private readonly EventOccurrence _laterStart = Date("2026-11-22", OccurrenceKind.Special, "Morning service");
    private readonly EventOccurrence _twentyNinth = Date("2026-11-29", OccurrenceKind.Regular, "Morning service");

    [Fact]
    public void ADateWithItsOwnTitleHasNoOtherDatesButOneChangedOnlyInTimeOrPlaceKeepsThem()
    {
        List<EventOccurrence> dates = [_eighth, _anniversary, _laterStart, _twentyNinth];

        Assert.Empty(OccurrenceListing.OtherDates(_anniversary, dates));
        Assert.Equal([_eighth, _anniversary, _twentyNinth], OccurrenceListing.OtherDates(_laterStart, dates));
        Assert.Equal([_eighth, _anniversary, _laterStart], OccurrenceListing.OtherDates(_twentyNinth, dates));
    }

    private static EventOccurrence Date(string date, OccurrenceKind kind, string title) => new()
    {
        Id = $"morning-{date}", ItemId = "morning", Kind = kind, Date = date, Fields = new EventFields { Title = title },
    };
}
