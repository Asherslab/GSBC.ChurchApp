using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Hosts.Website.Tests;

public sealed class EventListingTests
{
    private static readonly CalendarInfo Sundays = new() { Id = "sundays", Slug = "sunday-services", Name = "Sunday services" };
    private static readonly CalendarInfo Kids = new() { Id = "kids", Slug = "kids", Name = "Kids" };
    private static readonly CalendarInfo Youth = new() { Id = "youth", Slug = "youth", Name = "Youth" };
    private static readonly IReadOnlyList<CalendarInfo> Calendars = [Sundays, Kids, Youth];

    private static readonly DateTime SundayMorning = new(2026, 11, 7, 23, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AnEmbedLimitedToACalendarShowsItsEventsAndThoseShownOnIt()
    {
        EventOccurrence holidayClub = Occurrence("holiday-club", "2026-11-14", Kids.Id);
        EventOccurrence familyService = Occurrence("family-service", "2026-11-15", Sundays.Id, alsoOn: Kids.Id);
        EventOccurrence youthNight = Occurrence("youth-night", "2026-11-13", Youth.Id);

        IReadOnlyList<CalendarInfo> limited = EventListing.Limited(Calendars, "kids");

        Assert.Equal([Kids], limited);
        Assert.Equal(["holiday-club", "family-service"], EventListing.On([holidayClub, familyService, youthNight], limited).Select(occurrence => occurrence.Id));
    }

    [Fact]
    public void ALimitNamingNoCalendarShowsEveryPublicCalendar()
    {
        Assert.Equal(Calendars, EventListing.Limited(Calendars, "kids-team"));
        Assert.Equal([Kids, Youth], EventListing.Limited(Calendars, "Kids, youth"));
    }

    [Fact]
    public void WeeksStartOnSundayAndKeepTodaysDateThatIsNotOn()
    {
        EventOccurrence finished = Occurrence("prayer", "2026-11-06", Sundays.Id);
        EventOccurrence notOnToday = Occurrence("evening", "2026-11-08", Sundays.Id);
        notOnToday.NotOn = true;
        notOnToday.EndsAt = SundayMorning.AddHours(-1);
        EventOccurrence saturday = Occurrence("outreach", "2026-11-14", Sundays.Id);
        EventOccurrence nextSunday = Occurrence("anniversary", "2026-11-15", Sundays.Id);
        EventOccurrence december = Occurrence("carols", "2026-12-12", Sundays.Id);

        List<EventOccurrence> ahead = EventListing.Ahead([finished, notOnToday, saturday, nextSunday, december], SundayMorning).ToList();
        IReadOnlyList<EventWeek> weeks = EventListing.Weeks(ahead, new DateOnly(2026, 11, 8), weeks: 2);

        Assert.Equal(["This week", "Next week"], weeks.Select(week => week.Heading));
        Assert.Equal("8 – 14 November", weeks[0].Range);
        Assert.Equal(["evening", "outreach"], weeks[0].Days.SelectMany(day => day.Events).Select(occurrence => occurrence.Id));
        Assert.True(EventListing.HasLater(ahead, new DateOnly(2026, 11, 8), weeks: 2));
        Assert.False(EventListing.HasLater(ahead, new DateOnly(2026, 11, 8), weeks: 6));
    }

    [Fact]
    public void DayToDayDatesAreGatheredOnceAndOnlyProperEventsAreListedByDate()
    {
        EventOccurrence morning = Regular("morning", "2026-11-08", listEveryDate: false);
        EventOccurrence anniversary = Regular("morning", "2026-11-15", listEveryDate: false);
        anniversary.Kind = OccurrenceKind.Special;
        EventOccurrence evening = Regular("evening", "2026-11-08", listEveryDate: false);
        EventOccurrence eveningOff = Regular("evening", "2026-11-15", listEveryDate: false);
        eveningOff.NotOn = true;
        eveningOff.NotOnReason = "Christmas break";
        eveningOff.BackOn = "2027-01-31";
        EventOccurrence outreach = Regular("outreach", "2026-11-14", listEveryDate: true);
        EventOccurrence carols = Occurrence("carols", "2026-12-12", Sundays.Id);
        List<EventOccurrence> ahead = [morning, evening, outreach, anniversary, eveningOff, carols];

        Assert.Equal([outreach, anniversary, carols], EventListing.Listed(ahead));
        IReadOnlyList<RegularGathering> gatherings = EventListing.Gatherings(ahead);
        Assert.Equal(["morning", "evening"], gatherings.Select(gathering => gathering.Next.ItemId));
        Assert.Equal("Not on 15 November, back on 31 January (Christmas break)", gatherings[1].NotOn);
    }

    private static EventOccurrence Regular(string itemId, string date, bool listEveryDate)
    {
        EventOccurrence occurrence = Occurrence($"{itemId}-{date}", date, Sundays.Id);
        occurrence.ItemId = itemId;
        occurrence.Kind = OccurrenceKind.Regular;
        occurrence.Repeats = "Every Sunday";
        occurrence.ListEveryDate = listEveryDate;
        return occurrence;
    }

    private static EventOccurrence Occurrence(string id, string date, string calendarId, string? alsoOn = null)
    {
        DateTime starts = DateOnly.ParseExact(date, "yyyy-MM-dd").ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc);
        return new EventOccurrence
        {
            Id = id,
            Date = date,
            CalendarId = calendarId,
            AlsoOn = alsoOn is null ? [] : [alsoOn],
            Fields = new EventFields { Title = id },
            StartsAt = starts,
            EndsAt = starts.AddHours(2),
        };
    }
}
