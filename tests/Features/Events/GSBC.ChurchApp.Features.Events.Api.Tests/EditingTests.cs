using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class EditingTests
{
    private static readonly DateOnly Today = new(2026, 11, 8);
    private static readonly DateTime Now = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 20, 0, 0));

    [Fact]
    public void OnACalendarThatNeedsAPublisherOnlyAPublisherShowsAnEventOrChangesOneOnceShown()
    {
        CalendarRecord youth = Calendar("Youth", needsPublisher: true);
        CalendarRecord kids = Calendar("Kids", needsPublisher: false);
        EventRecord camp = new() { Id = Guid.CreateVersion7(), CalendarId = youth.Id, Showing = Showing.SavedForLater };
        EventRecord breakUp = new() { Id = Guid.CreateVersion7(), CalendarId = kids.Id, Showing = Showing.Shown };

        Assert.True(EventWriter.ShowingNeedsAPublisher(youth, null, Showing.Shown, Now));
        Assert.True(EventWriter.ShowingNeedsAPublisher(youth, camp, Showing.ShownFrom, Now));
        Assert.False(EventWriter.ShowingNeedsAPublisher(youth, camp, Showing.SavedForLater, Now));
        Assert.False(EventWriter.ChangeNeedsAPublisher(youth, camp, Now));
        Assert.True(EventWriter.ShowingNeedsAPublisher(youth, breakUp, Showing.Shown, Now));
        Assert.False(EventWriter.ShowingNeedsAPublisher(kids, null, Showing.Shown, Now));

        camp.Showing = Showing.Shown;

        Assert.True(EventWriter.ChangeNeedsAPublisher(youth, camp, Now));
        Assert.False(EventWriter.ChangeNeedsAPublisher(kids, breakUp, Now));
    }

    [Fact]
    public void ThisDateOnlyKeepsWhatItChangedAndEveryDateChangesTheRestButWarnsFirstAboutWhatADateKeeps()
    {
        CalendarRecord services = Calendar("Sunday services", needsPublisher: false);
        RegularEventRecord morning = new()
        {
            Id = Guid.CreateVersion7(), CalendarId = services.Id, Fields = Fields("Morning service", "Auditorium"),
            Pattern = new RegularPattern { Repeats = Repeats.Weekly, Days = [DayOfWeek.Sunday], StartDate = EventDates.Format(Today.AddDays(-60)) },
            StartMinute = 10 * 60 + 30, EndMinute = 12 * 60, Showing = Showing.Shown,
        };
        DateOnly anniversary = Today.AddDays(7);
        Timed every = new(morning.Fields, morning.StartMinute, morning.EndMinute);
        Timed thatDay = new(Fields("Anniversary Sunday Service", "Auditorium"), 9 * 60, 12 * 60);
        SpecialRecord special = new()
        {
            RegularEventId = morning.Id, Date = anniversary, Changed = FieldValues.Differences(every, thatDay),
            Fields = thatDay.Fields, StartMinute = thatDay.StartMinute, EndMinute = thatDay.EndMinute,
        };
        Timed everyAfter = new(Fields("Morning service", "Fellowship Area"), 11 * 60, 12 * 60);

        List<KeptValue> kept = EventWriter.KeptValuesOf(morning, FieldValues.Differences(every, everyAfter), [special]);
        (morning.Fields, morning.StartMinute) = (everyAfter.Fields, everyAfter.StartMinute);
        List<EventOccurrence> dates = new EventsBook([services], [], [morning], [special], [], [], [])
            .Occurrences(anniversary, anniversary.AddDays(7), _ => true).OrderBy(occurrence => occurrence.StartsAt).ToList();

        KeptValue time = Assert.Single(kept);
        Assert.Equal((EventDates.Format(anniversary), EventFieldKeys.Time, "9am–12pm"), (time.Date, time.Field, time.Value));
        Assert.Equal([("Anniversary Sunday Service", 9, "Fellowship Area"), ("Morning service", 11, "Fellowship Area")], dates.Select(Seen));
        Assert.Equal("Now 11am–12pm · Now at Fellowship Area", FieldValues.ChangeNote(every, everyAfter));
    }

    private static (string, int, string) Seen(EventOccurrence occurrence) =>
        (occurrence.Fields.Title, ChurchTime.ToChurch(occurrence.StartsAt).Hour, occurrence.Fields.Place!.Name);

    private static EventFields Fields(string title, string place) => new() { Title = title, Place = new EventPlace { Name = place } };

    private static CalendarRecord Calendar(string name, bool needsPublisher) => new()
    {
        Id = Guid.CreateVersion7(), Name = name, Slug = CalendarLinks.SlugOf(name), Audience = Audience.Everyone.Text, NeedsPublisher = needsPublisher,
    };
}
