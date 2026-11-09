using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class DigestTests
{
    private static readonly DateOnly Start = new(2026, 11, 9);
    private static readonly DateTime Now = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 12, 0, 0));

    private readonly CalendarRecord _services = Calendar("Sunday services", Audience.Everyone);
    private readonly CalendarRecord _kids = Calendar("Kids", Audience.Everyone);
    private readonly CalendarRecord _members = Calendar("Church members", Audience.HoldingRole(Guid.CreateVersion7()));

    [Fact]
    public void ADigestMadeOnAMondayFromTodayHasThisWeeksThingsAndAMonthReachesFurther()
    {
        DateOnly monday = new(2026, 11, 9);
        RegularEventRecord morning = Regular(_services, "Morning service", DayOfWeek.Sunday);
        SpecialRecord anniversary = new()
        {
            RegularEventId = morning.Id, Date = new DateOnly(2026, 11, 15), Changed = [EventFieldKeys.Title],
            Fields = new EventFields { Title = "Anniversary Sunday Service" },
        };
        EventRecord agm = OneOff(_members, "Annual general meeting", new DateOnly(2026, 11, 12), 19 * 60 + 30);
        EventRecord outreach = OneOff(_kids, "Outreach Saturday", new DateOnly(2026, 11, 28), 9 * 60);
        EventsBook book = new([_services, _kids, _members], [agm, outreach], [morning], [anniversary], [], [], []);
        IReadOnlySet<Guid> calendars = book.Calendars.Keys.ToHashSet();

        List<DigestItem> twoWeeks = Digests.Candidates(book, monday, Digests.EndOf(monday, DigestRange.TwoWeeks), calendars, Now);
        List<DigestItem> month = Digests.Candidates(book, monday, Digests.EndOf(monday, DigestRange.Month), calendars, Now);

        Assert.Equal(["Annual general meeting", "Anniversary Sunday Service"], twoWeeks.Select(item => item.CalendarWords.Title));
        Assert.Equal("This Sunday 15 November", twoWeeks[1].CalendarWords.When);
        Assert.Equal(["Annual general meeting", "Anniversary Sunday Service", "Outreach Saturday"], month.Select(item => item.CalendarWords.Title));
    }

    [Fact]
    public void EachRangeEndsWhereItSaysAndACustomOneStaysInBounds()
    {
        DateOnly thursday = new(2026, 11, 12);

        Assert.Equal(new DateOnly(2026, 11, 18), Digests.EndOf(thursday, DigestRange.OneWeek));
        Assert.Equal(new DateOnly(2026, 11, 25), Digests.EndOf(thursday, DigestRange.TwoWeeks));
        Assert.Equal(new DateOnly(2026, 12, 11), Digests.EndOf(thursday, DigestRange.Month));
        Assert.Equal(new DateOnly(2026, 11, 30), Digests.EndOf(thursday, DigestRange.Custom, new DateOnly(2026, 11, 30)));
        Assert.Equal(thursday, Digests.EndOf(thursday, DigestRange.Custom, new DateOnly(2026, 11, 1)));
        Assert.Equal(thursday.AddDays(Digests.LongestDays - 1), Digests.EndOf(thursday, DigestRange.Custom, thursday.AddYears(1)));
    }

    [Fact]
    public void TheDeskReopensTheDigestWorkedOnThisWeekUntilItEndsAndStartsAfreshEachWeek()
    {
        DateOnly monday = new(2026, 11, 16);
        DateTime mondayMorning = ChurchTime.ToUtc(new DateTime(2026, 11, 16, 9, 0, 0));
        DateTime lastSunday = ChurchTime.ToUtc(new DateTime(2026, 11, 15, 20, 0, 0));
        Func<DigestRecord, bool> current = Digests.Current(monday).Compile();

        Assert.True(current(new DigestRecord { Start = monday, End = monday.AddDays(13), UpdatedAt = mondayMorning, SentAt = mondayMorning }));
        Assert.False(current(new DigestRecord { Start = monday.AddDays(7), End = monday.AddDays(27), UpdatedAt = lastSunday }));
        Assert.False(current(new DigestRecord { Start = monday.AddDays(-14), End = monday.AddDays(-1), UpdatedAt = mondayMorning }));
    }

    [Fact]
    public void ItIsMadeOfOneOffEventsSpecialsBreaksAndDatesStillNeedingDetailsNeverOrdinaryRegularDates()
    {
        RegularEventRecord morning = Regular(_services, "Morning service", DayOfWeek.Sunday);
        RegularEventRecord evening = Regular(_services, "Evening service", DayOfWeek.Sunday);
        RegularEventRecord impactKids = Regular(_kids, "Impact Kids", DayOfWeek.Friday);
        RegularEventRecord outreach = Regular(_kids, "Outreach Saturday", DayOfWeek.Saturday);
        outreach.Needs = [EventFieldKeys.Place];
        SpecialRecord anniversary = new()
        {
            RegularEventId = morning.Id, Date = new DateOnly(2026, 11, 15), Changed = [EventFieldKeys.Title],
            Fields = new EventFields { Title = "Anniversary Sunday Service" },
        };
        BreakRecord camp = new() { Id = Guid.CreateVersion7(), Name = "Kids camp weekend", StartDate = new DateOnly(2026, 11, 13), EndDate = new DateOnly(2026, 11, 14), RegularEventId = impactKids.Id };
        EventRecord agm = OneOff(_members, "Annual general meeting", new DateOnly(2026, 11, 12), 19 * 60 + 30);
        EventRecord notYet = OneOff(_kids, "Break-up night", new DateOnly(2026, 11, 20), 18 * 60);
        notYet.Showing = Showing.SavedForLater;

        List<DigestItem> items = Candidates(new EventsBook([_services, _kids, _members], [agm, notYet], [morning, evening, impactKids, outreach], [anniversary], [camp], [], []));

        Assert.Equal(
            ["Annual general meeting", "No Impact Kids", "Outreach Saturday", "Anniversary Sunday Service"],
            items.Select(item => item.CalendarWords.Title));
        Assert.Equal("This Thursday 12 November", items[0].CalendarWords.When);
        Assert.Equal("7:30pm", items[0].CalendarWords.Time);
        Assert.Equal("Kids on break", items[1].CalendarWords.Label);
        Assert.Equal("Impact Kids is off for the Kids camp weekend, and back on Friday 20 November.", items[1].CalendarWords.Description);
        Assert.Equal(("The next two Saturdays", "Place to be announced."), (items[2].CalendarWords.When, items[2].CalendarWords.Description));
        Assert.False(items[0].ForEveryone);
    }

    [Fact]
    public void TheWhenLineSaysThisForTheFirstWeekAndNamesARunOfWeeks()
    {
        Assert.Equal("This Thursday 12 November", Digests.When([new DateOnly(2026, 11, 12)], Start));
        Assert.Equal("Friday 20 November", Digests.When([new DateOnly(2026, 11, 20)], Start));
        Assert.Equal("The next two Sundays", Digests.When([new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 22)], Start));
        Assert.Equal("Sundays 22 and 29 November", Digests.When([new DateOnly(2026, 11, 22), new DateOnly(2026, 11, 29)], Start));
        Assert.Equal("Sundays 29 November and 6 December", Digests.When([new DateOnly(2026, 11, 29), new DateOnly(2026, 12, 6)], new DateOnly(2026, 11, 16)));
        Assert.Equal("This Friday 13 to Sunday 15 November", Digests.Span(new DateOnly(2026, 11, 13), new DateOnly(2026, 11, 15), Start));
        Assert.Equal("This Sunday 15 November", Digests.When([new DateOnly(2026, 11, 15)], new DateOnly(2026, 11, 13)));
        Assert.Equal("Thursday 19 November", Digests.When([new DateOnly(2026, 11, 19)], new DateOnly(2026, 11, 13)));
    }

    [Fact]
    public void StaffChoicesOutliveTheCalendarAndSomethingNewSlotsInByDate()
    {
        DigestItem agm = Item("event:agm", "2026-11-12");
        DigestItem outreach = Item("date:outreach", "2026-11-14");
        DigestItem highTea = Item("event:high-tea", "2026-11-21");
        DigestItem games = Item("event:games", "2026-11-20");
        List<DigestEntry> entries =
        [
            new() { Key = "event:high-tea", Included = true },
            new() { Key = "date:outreach", Included = true, Changed = [DigestFields.Description], Words = new DigestWords { Description = "Fernleigh Park this time." } },
            new() { Key = "event:agm", Included = false },
            new() { Key = "event:gone", Included = true },
            new() { Key = "own:notice", Own = true, Included = true, Words = new DigestWords { Title = "Church cleaning bee" } },
        ];

        List<DigestItem> merged = Digests.Merge([agm, outreach, games, highTea], entries);

        Assert.Equal(["event:high-tea", "date:outreach", "event:agm", "event:games", "own:notice"], merged.Select(item => item.Key));
        Assert.Equal("Fernleigh Park this time.", merged[1].Shown.Description);
        Assert.Equal("Calendar words", merged[1].Shown.Title);
        Assert.False(merged[2].Included);
        Assert.True(merged[3].IsNew);
        Assert.Equal("Church cleaning bee", merged[4].Shown.Title);
        Assert.False(merged[0].IsNew);
    }

    [Fact]
    public void ChoicesForSomethingAChipOrTheWeeksHideComeBackWithIt()
    {
        DigestEntry reworded = new() { Key = "event:high-tea", Included = true, Changed = [DigestFields.Title], Words = new DigestWords { Title = "High tea" } };
        DigestEntry removedByStaff = new() { Key = "own:bee", Own = true, Included = true, Words = new DigestWords { Title = "Cleaning bee" } };
        DigestEntry agm = new() { Key = "event:agm", Included = true };

        List<DigestEntry> kept = Digests.Kept([agm, reworded, removedByStaff], [agm], new HashSet<string> { "event:agm" });

        Assert.Equal(["event:agm", "event:high-tea"], kept.Select(entry => entry.Key));
    }

    [Fact]
    public void ThingsFromAPrivateCalendarReachTheAppOnlyThroughThePrivateFeedWithTheCalendarsAudience()
    {
        DigestItem agm = Item("event:agm", "2026-11-12");
        agm.CalendarIds = [_members.Id.ToString()];
        DigestItem anniversary = Item("date:anniversary", "2026-11-15");
        anniversary.CalendarIds = [_services.Id.ToString()];
        DigestItem own = Digests.Merge([], [new DigestEntry { Key = "own:bee", Own = true, Included = true, Words = new DigestWords { Title = "Cleaning bee" } }])[0];
        DigestRecord digest = new() { Start = Start, End = Digests.EndOf(Start, DigestRange.TwoWeeks), SentAt = Now, Sent = Digests.ForApp([agm, anniversary, own], Digests.EndOf(Start, DigestRange.TwoWeeks)) };

        List<EventsPublisher.Row> rows = EventsPublisher.DigestRows(new EventsBook([_services, _members], [], [], [], [], [], []), digest).ToList();

        EventsPublisher.Row membersOnly = Assert.Single(rows, row => row.Feed == EventsFeeds.PrivateDigest);
        Assert.Equal(_members.Audience, membersOnly.Audience);
        Assert.Equal("event:agm", EventsPayloads.Decode<DigestShownItem>(membersOnly.Payload).Key);
        Assert.Equal(3, rows.Count(row => row.Feed == EventsFeeds.Digest));
        Assert.Equal("2 things coming up: Calendar words and Cleaning bee.",
            Digests.Notification(new EventsBook([_services, _members], [], [], [], [], [], []), digest.Sent, NotificationAudience.SignedIn).Body);
    }

    [Fact]
    public void SendingNotifiesEachPersonOnceWithWhatTheyWillSeeAndNobodyForWhatTheyCannot()
    {
        Guid membersRole = CalendarAudiences.Of(_members.Audience).RoleIds.Single();
        CalendarRecord kidsTeam = Calendar("Kids team", Audience.SignedIn);
        DigestItem agm = Item("event:agm", "2026-11-12");
        agm.CalendarIds = [_members.Id.ToString()];
        DigestItem helpers = Item("event:helpers", "2026-11-11");
        helpers.CalendarIds = [kidsTeam.Id.ToString()];
        DigestItem camp = Item("event:camp", "2026-11-13");
        camp.CalendarIds = [_kids.Id.ToString()];
        EventsBook book = new([_kids, _members, kidsTeam], [], [], [], [], [], []);

        List<Notification> everyone = Digests.ForEveryoneWhoSeesIt(book, Digests.ForApp([agm, helpers, camp], Digests.EndOf(Start, DigestRange.TwoWeeks)), Start);
        List<Notification> membersOnly = Digests.ForEveryoneWhoSeesIt(book, Digests.ForApp([agm], Digests.EndOf(Start, DigestRange.TwoWeeks)), Start);

        Assert.Equal([NotificationAudience.Holding(RoleMembership.Of(membersRole)), NotificationAudience.SignedIn, NotificationAudience.Everyone], everyone.Select(notification => notification.Audience));
        Assert.Equal(["3 things coming up: Calendar words, Calendar words and more.", "2 things coming up: Calendar words and Calendar words.", "One thing coming up: Calendar words."],
            everyone.Select(notification => notification.Body));
        Assert.Single(everyone.Select(notification => notification.Subject).Distinct());
        Assert.Equal(NotificationAudience.Holding(RoleMembership.Of(membersRole)), Assert.Single(membersOnly).Audience);
    }

    [Fact]
    public void ADigestNotifiesOnceUnlessStaffChooseToNotifyAgain()
    {
        DigestRecord digest = new() { Start = Start };
        Assert.True(Digests.Notifies(digest, again: false));

        digest.NotifiedAt = Now;

        Assert.False(Digests.Notifies(digest, again: false));
        Assert.True(Digests.Notifies(digest, again: true));
    }

    [Fact]
    public void ThingsFromARemovedCalendarLeaveTheSentDigestInsteadOfReachingAnyoneSignedIn()
    {
        CalendarRecord kidsTeam = Calendar("Kids team", Audience.HoldingRole(Guid.CreateVersion7()));
        DigestItem rosterNight = Item("event:roster-night", "2026-11-12");
        rosterNight.CalendarIds = [kidsTeam.Id.ToString(), _services.Id.ToString()];
        DigestItem anniversary = Item("date:anniversary", "2026-11-15");
        anniversary.CalendarIds = [_services.Id.ToString(), kidsTeam.Id.ToString()];
        DigestRecord digest = new() { Start = Start, End = Digests.EndOf(Start, DigestRange.TwoWeeks), SentAt = Now, Sent = Digests.ForApp([rosterNight, anniversary], Digests.EndOf(Start, DigestRange.TwoWeeks)) };
        EventsBook afterRemoving = new([_services], [], [], [], [], [], []);

        List<EventsPublisher.Row> rows = EventsPublisher.DigestRows(afterRemoving, digest).ToList();

        Assert.DoesNotContain(rows, row => row.Feed == EventsFeeds.PrivateDigest);
        Assert.Equal(["digest", "2026-11-09/date:anniversary"], rows.Select(row => row.RecordId));
        Assert.Equal("One thing coming up: Calendar words.", Digests.Notification(afterRemoving, digest.Sent, NotificationAudience.SignedIn).Body);
    }

    private static List<DigestItem> Candidates(EventsBook book) =>
        Digests.Candidates(book, Start, Digests.EndOf(Start, DigestRange.TwoWeeks), book.Calendars.Keys.ToHashSet(), Now);

    private static DigestItem Item(string key, string firstDate) => new()
    {
        Key = key, Kind = DigestItemKind.Event, Included = true, FirstDate = firstDate, LastDate = firstDate,
        CalendarWords = new DigestWords { Title = "Calendar words" },
    };

    private static CalendarRecord Calendar(string name, Audience audience) =>
        new() { Id = Guid.CreateVersion7(), Name = name, Slug = CalendarLinks.SlugOf(name), Audience = audience.Text };

    private static RegularEventRecord Regular(CalendarRecord calendar, string title, DayOfWeek day) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = new EventFields { Title = title },
        Pattern = new RegularPattern { Repeats = Repeats.Weekly, Days = [day], StartDate = "2026-01-01" },
        StartMinute = 10 * 60, Showing = Showing.Shown,
    };

    private static EventRecord OneOff(CalendarRecord calendar, string title, DateOnly date, int start) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = new EventFields { Title = title }, StartDate = date, StartMinute = start,
        Showing = Showing.Shown,
    };
}
