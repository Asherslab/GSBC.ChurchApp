using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class ComingUpTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComingUpLeavesOrdinaryRegularDatesOutAndKeepsWhatIsDifferent()
    {
        EventOccurrence ordinary = At("ordinary", OccurrenceKind.Regular, 1);
        EventOccurrence special = At("special", OccurrenceKind.Special, 2);
        EventOccurrence oneOff = At("one-off", OccurrenceKind.OneOff, 3);
        EventOccurrence notOn = At("not on", OccurrenceKind.Regular, 4, occurrence => occurrence.NotOn = true);
        EventOccurrence changed = At("changed", OccurrenceKind.Regular, 5, occurrence => occurrence.ChangedAt = Now);
        EventOccurrence past = At("past", OccurrenceKind.OneOff, -3);

        IReadOnlyList<EventOccurrence> coming = CalendarDiary.ComingUp([past, ordinary, special, oneOff, notOn, changed], Now, 10);

        Assert.Equal(["special", "one-off", "not on", "changed"], coming.Select(occurrence => occurrence.Fields.Title));
    }

    private static EventOccurrence At(string title, OccurrenceKind kind, int days, Action<EventOccurrence>? change = null)
    {
        DateTime starts = Now.AddDays(days).AddHours(2);
        EventOccurrence occurrence = new()
        {
            Id = title, Kind = kind, Fields = new EventFields { Title = title }, StartsAt = starts, EndsAt = starts.AddHours(1),
            Date = EventDates.Format(DateOnly.FromDateTime(starts.AddHours(10))),
        };
        change?.Invoke(occurrence);
        return occurrence;
    }
}
