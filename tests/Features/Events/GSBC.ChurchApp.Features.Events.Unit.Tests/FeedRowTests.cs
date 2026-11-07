using GSBC.ChurchApp.Features.Events.Api;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Unit.Tests;

public sealed class FeedRowTests
{
    private static readonly DateOnly Today = new(2026, 11, 8);
    private static readonly DateTime Now = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 20, 0, 0));

    [Fact]
    public void PhonesHoldThirtyDaysBackAndTwelveMonthsAhead()
    {
        CalendarRecord services = Calendar("Sunday services", Audience.Everyone);
        RegularEventRecord morning = Regular(services, "Morning service", Today.AddYears(-1));

        List<string> dates = Occurrences(Rows(Book([services], [morning]), EventsFeeds.Public)).Select(occurrence => occurrence.Date).Order().ToList();

        Assert.Equal(EventDates.Format(Today.AddDays(-28)), dates[0]);
        Assert.Equal("2027-11-07", dates[^1]);
    }

    [Fact]
    public void APrivateCalendarsRowsNeverGoInThePublicFeedsAndCarryTheirAudience()
    {
        Audience churchMembers = Audience.HoldingRole(Guid.CreateVersion7());
        CalendarRecord members = Calendar("Church members", churchMembers);
        CalendarRecord kids = Calendar("Kids", Audience.Everyone);
        List<EventsPublisher.Row> rows = Rows(Book([members, kids], [Regular(members, "Members' prayer", Today), Regular(kids, "Impact Kids", Today)])).ToList();

        Assert.All(rows.Where(row => row.Feed is EventsFeeds.Public or EventsFeeds.Calendars), row => Assert.Contains(Title(row), new[] { "Kids", "Impact Kids" }));
        Assert.All(rows.Where(row => row.Feed is EventsFeeds.Private or EventsFeeds.PrivateCalendars), row =>
        {
            Assert.Contains(Title(row), new[] { "Church members", "Members' prayer" });
            Assert.Equal(churchMembers.Text, row.Audience);
        });
        Assert.Contains(rows, row => row.Feed == EventsFeeds.Private);
    }

    [Fact]
    public void ContactDetailsTravelOnlyInTheContactsFeedAndNeverWiderThanTheEventsAudience()
    {
        Audience kidsTeam = Audience.HoldingRole(Guid.CreateVersion7());
        CalendarRecord outreach = Calendar("Outreach", Audience.Everyone);
        CalendarRecord helpers = Calendar("Kids team", kidsTeam);
        EventContact sam = new() { Name = "Sam Okafor", Phone = "0400 000 002" };
        RegularEventRecord saturdays = Regular(outreach, "Outreach Saturday", Today);
        RegularEventRecord helpersNight = Regular(helpers, "Helpers night", Today);
        saturdays.Fields.Contacts = [sam];
        helpersNight.Fields.Contacts = [sam];
        List<EventsPublisher.Row> rows = Rows(Book([outreach, helpers], [saturdays, helpersNight])).ToList();

        Assert.All(Occurrences(rows.Where(row => row.Feed is EventsFeeds.Public or EventsFeeds.Private)), occurrence =>
            Assert.Equal(("Sam Okafor", "", ""), (occurrence.Fields.Contacts[0].Name, occurrence.Fields.Contacts[0].Phone, occurrence.Fields.Contacts[0].Email)));
        Assert.Equivalent(new[] { Audience.SignedIn.Text, kidsTeam.Text }, rows.Where(row => row.Feed == EventsFeeds.Contacts).Select(row => row.Audience).Distinct().ToList(), strict: true);
    }

    private static string Title(EventsPublisher.Row row) =>
        row.Feed is EventsFeeds.Calendars or EventsFeeds.PrivateCalendars
            ? EventsPayloads.Decode<CalendarInfo>(row.Payload).Name
            : EventsPayloads.Decode<EventOccurrence>(row.Payload).Fields.Title;

    private static IEnumerable<EventsPublisher.Row> Rows(EventsBook book, string? feed = null) =>
        EventsPublisher.Rows(book, Now).Where(row => feed is null || row.Feed == feed);

    private static List<EventOccurrence> Occurrences(IEnumerable<EventsPublisher.Row> rows) =>
        rows.Select(row => EventsPayloads.Decode<EventOccurrence>(row.Payload)).ToList();

    private static EventsBook Book(IReadOnlyList<CalendarRecord> calendars, IReadOnlyList<RegularEventRecord> regularEvents) =>
        new(calendars, [], regularEvents, [], [], [], []);

    private static CalendarRecord Calendar(string name, Audience audience) =>
        new() { Id = Guid.CreateVersion7(), Name = name, Slug = CalendarLinks.SlugOf(name), Audience = audience.Text };

    private static RegularEventRecord Regular(CalendarRecord calendar, string title, DateOnly from) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = new EventFields { Title = title },
        Pattern = new RegularPattern { Repeats = Repeats.Weekly, Days = [DayOfWeek.Sunday], StartDate = EventDates.Format(from) },
        StartMinute = 10 * 60, Showing = Showing.Shown,
    };
}
