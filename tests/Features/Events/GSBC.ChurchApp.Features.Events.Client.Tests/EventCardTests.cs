using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Content;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class EventCardTests
{
    private static readonly Guid Worship = Guid.CreateVersion7();
    private static readonly DateTime Now = new(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);

    private readonly Diary _diary = new([], [Sunday(15), Sunday(22, cancelled: true), Sunday(29)], new Dictionary<string, List<EventContact>>());

    [Fact]
    public void TheNextDateRollsOnAndSaysWhenItsCancelled()
    {
        EventCardShown next = Assert.IsType<EventCardShown>(EventCardFacts.Find(_diary, Card(EventShows.NextDate), Now));

        Assert.Equal(EventDates.OccurrenceId(Worship, new DateOnly(2026, 11, 22)), next.Occurrence.Id);
        Assert.True(next.Occurrence.NotOn);
    }

    [Fact]
    public void ThePatternSkipsACancelledDateAndShowsNoDate()
    {
        EventCardShown pattern = Assert.IsType<EventCardShown>(EventCardFacts.Find(_diary, Card(EventShows.Pattern), Now));

        Assert.Equal(EventDates.OccurrenceId(Worship, new DateOnly(2026, 11, 29)), pattern.Occurrence.Id);
        Assert.True(pattern.AsPattern);
    }

    [Fact]
    public void ADateThePhoneDoesntHoldShowsNothingUnlessItsOver()
    {
        AppCard past = new(AppCardKind.Event, EventDates.OccurrenceId(Worship, new DateOnly(2026, 9, 6)));
        AppCard future = new(AppCardKind.Event, EventDates.OccurrenceId(Worship, new DateOnly(2027, 2, 7)));

        Assert.Null(EventCardFacts.Find(_diary, past, Now));
        Assert.True(EventCardFacts.IsOver(past, Now));
        Assert.Null(EventCardFacts.Find(_diary, future, Now));
        Assert.False(EventCardFacts.IsOver(future, Now));
    }

    [Fact]
    public void ARegularEventIsOfferedOnceWithItsNextDateAndWhatItRepeats()
    {
        AppCardChoice choice = Assert.Single(EventCardFacts.Choices(_diary, "", Now));

        Assert.Equal((Worship.ToString("N"), "Every Sunday"), (choice.RegularId, choice.Repeats));
        Assert.Equal(EventDates.OccurrenceId(Worship, new DateOnly(2026, 11, 29)), choice.Card.Id);
    }

    private static AppCard Card(EventShows shows) => new(AppCardKind.Event, Worship.ToString("N"), shows);

    private static EventOccurrence Sunday(int day, bool cancelled = false)
    {
        DateOnly date = new(2026, 11, day);
        DateTime starts = date.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Utc);
        return new EventOccurrence
        {
            Id = EventDates.OccurrenceId(Worship, date), Kind = OccurrenceKind.Regular, ItemId = Worship.ToString(), Date = EventDates.Format(date),
            StartsAt = starts, EndsAt = starts.AddHours(1), Repeats = "Every Sunday", NotOn = cancelled, Fields = new EventFields { Title = "Sunday worship" },
        };
    }
}
