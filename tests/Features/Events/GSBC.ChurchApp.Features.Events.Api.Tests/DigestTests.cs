using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class DigestTests
{
    private static readonly DateOnly Start = new(2026, 11, 9);
    private static readonly DateTime Now = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 12, 0, 0));
    private static readonly IReadOnlyDictionary<Guid, HighlightedPost> NoPosts = new Dictionary<Guid, HighlightedPost>();

    private readonly CalendarRecord _services = Calendar("Sunday services", Audience.Everyone);
    private readonly CalendarRecord _kids = Calendar("Kids", Audience.Everyone);
    private readonly CalendarRecord _members = Calendar("Church members", Audience.HoldingRole(Guid.CreateVersion7()));

    [Fact]
    public void TheCalendarPickerListsEverythingInTheDatesMarksTheSpecialThingsAndAMonthReachesFurther()
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

        List<DigestItem> twoWeeks = Digests.Candidates(book, monday, Digests.EndOf(monday, DigestRange.TwoWeeks), Now);
        List<DigestItem> month = Digests.Candidates(book, monday, Digests.EndOf(monday, DigestRange.Month), Now);

        Assert.Equal(["Annual general meeting", "Anniversary Sunday Service", "Morning service"], twoWeeks.Select(item => item.CalendarWords.Title));
        Assert.Equal([true, true, false], twoWeeks.Select(item => item.Special));
        Assert.Equal("This Sunday 15 November", twoWeeks[1].CalendarWords.When);
        Assert.Contains("Outreach Saturday", month.Select(item => item.CalendarWords.Title));
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
    public void ThePickerGroupsBreaksAndDatesNeedingDetailsAndListsEachOrdinaryDateOnItsOwn()
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

        List<DigestItem> listed = Candidates(new EventsBook([_services, _kids, _members], [agm, notYet], [morning, evening, impactKids, outreach], [anniversary], [camp], [], []));
        List<DigestItem> items = listed.Where(item => item.Special).ToList();

        Assert.Equal(
            ["Annual general meeting", "No Impact Kids", "Outreach Saturday", "Anniversary Sunday Service"],
            items.Select(item => item.CalendarWords.Title));
        Assert.Equal("This Thursday 12 November", items[0].CalendarWords.When);
        Assert.Equal("7:30pm", items[0].CalendarWords.Time);
        Assert.Equal("Kids on break", items[1].CalendarWords.Label);
        Assert.Equal("Impact Kids is off for the Kids camp weekend, and back on Friday 20 November.", items[1].CalendarWords.Description);
        Assert.Equal(("The next two Saturdays", "Place to be announced."), (items[2].CalendarWords.When, items[2].CalendarWords.Description));
        Assert.False(items[0].ForEveryone);
        Assert.Equal(2, listed.Count(item => item.CalendarWords.Title == "Evening service"));
        Assert.All(listed.Where(item => !item.Special), item => Assert.Single(item.OccurrenceIds));
        Assert.DoesNotContain("Break-up night", listed.Select(item => item.CalendarWords.Title));
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
    public void OnlyWhatStaffAddedIsInTheDigestInTheirOrderWithTheirWords()
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

        List<DigestItem> merged = Digests.Merge([agm, outreach, games, highTea], NoPosts, entries);

        Assert.Equal(["event:high-tea", "date:outreach", "own:notice"], merged.Select(item => item.Key));
        Assert.Equal("Fernleigh Park this time.", merged[1].Shown.Description);
        Assert.Equal("Calendar words", merged[1].Shown.Title);
        Assert.Equal("Church cleaning bee", merged[2].Shown.Title);
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
        DigestItem own = Digests.Merge([], NoPosts, [new DigestEntry { Key = "own:bee", Own = true, Included = true, Words = new DigestWords { Title = "Cleaning bee" } }])[0];
        DigestRecord digest = new() { Start = Start, End = Digests.EndOf(Start, DigestRange.TwoWeeks), SentAt = Now, Sent = Digests.ForApp([agm, anniversary, own], Digests.EndOf(Start, DigestRange.TwoWeeks)) };

        List<EventsPublisher.Row> rows = EventsPublisher.DigestRows(new EventsBook([_services, _members], [], [], [], [], [], []), NoPosts, [digest]).ToList();

        EventsPublisher.Row membersOnly = Assert.Single(rows, row => row.Feed == EventsFeeds.PrivateDigest);
        Assert.Equal(_members.Audience, membersOnly.Audience);
        Assert.Equal("event:agm", EventsPayloads.Decode<DigestShownItem>(membersOnly.Payload).Key);
        Assert.Equal(3, rows.Count(row => row.Feed == EventsFeeds.Digest));
        Assert.Equal("2 things coming up: Calendar words and Cleaning bee.",
            Digests.Notification(new EventsBook([_services, _members], [], [], [], [], [], []), NoPosts, digest.Sent, NotificationAudience.SignedIn).Body);
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

        List<Notification> everyone = Digests.ForEveryoneWhoSeesIt(book, NoPosts, Digests.ForApp([agm, helpers, camp], Digests.EndOf(Start, DigestRange.TwoWeeks)), Start);
        List<Notification> membersOnly = Digests.ForEveryoneWhoSeesIt(book, NoPosts, Digests.ForApp([agm], Digests.EndOf(Start, DigestRange.TwoWeeks)), Start);

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

        List<EventsPublisher.Row> rows = EventsPublisher.DigestRows(afterRemoving, NoPosts, [digest]).ToList();

        Assert.DoesNotContain(rows, row => row.Feed == EventsFeeds.PrivateDigest);
        Assert.Equal(["digest", "2026-11-09/date:anniversary"], rows.Select(row => row.RecordId));
        Assert.Equal("One thing coming up: Calendar words.", Digests.Notification(afterRemoving, NoPosts, digest.Sent, NotificationAudience.SignedIn).Body);
    }

    [Fact]
    public void TheAppGetsTheLatestDigestAndTheThreeSentBeforeItEachUnderItsOwnHeading()
    {
        EventsBook book = new([_services], [], [], [], [], [], []);
        DigestRecord resent = SentFor(Start.AddDays(-14), "event:changed", Now.AddDays(-13));
        List<DigestRecord> newestFirst =
        [
            SentFor(Start, "event:latest", Now),
            resent,
            SentFor(Start.AddDays(-14), "event:first-try", Now.AddDays(-14)),
            SentFor(Start.AddDays(-28), "event:a", Now.AddDays(-28)),
            SentFor(Start.AddDays(-42), "event:b", Now.AddDays(-42)),
            SentFor(Start.AddDays(-56), "event:too-old", Now.AddDays(-56)),
        ];

        List<EventsPublisher.Row> rows = EventsPublisher.DigestRows(book, NoPosts, newestFirst).ToList();

        Assert.Equal(["digest", "2026-11-09/event:latest", "digest:2026-10-26", "2026-10-26/event:changed", "digest:2026-10-12", "2026-10-12/event:a",
            "digest:2026-09-28", "2026-09-28/event:b"], rows.Select(row => row.RecordId));
    }

    [Fact]
    public void ANewsPostReachesOnlyThePeopleItIsForAndLeavesTheAppOnceItIsNoLongerPublished()
    {
        Guid membersRole = Guid.CreateVersion7();
        HighlightedPost offering = new(Guid.CreateVersion7(), "Christmas offering", "This year's offering goes to the food pantry.", null,
            Now.AddDays(-1), Audience.HoldingRole(membersRole), "/news/offering");
        Dictionary<Guid, HighlightedPost> published = new() { [offering.Id] = offering };
        DigestItem baptisms = Item("event:baptisms", "2026-11-15");
        baptisms.CalendarIds = [_services.Id.ToString()];
        List<DigestItem> items = Digests.Merge([baptisms], published,
            [new DigestEntry { Key = $"news:{offering.Id:D}", Included = true }, new DigestEntry { Key = "event:baptisms", Included = true }]);
        DigestRecord digest = new() { Start = Start, End = Digests.EndOf(Start, DigestRange.TwoWeeks), SentAt = Now, Sent = Digests.ForApp(items, Digests.EndOf(Start, DigestRange.TwoWeeks)) };
        EventsBook book = new([_services], [], [], [], [], [], []);

        List<EventsPublisher.Row> rows = EventsPublisher.DigestRows(book, published, [digest]).ToList();
        List<EventsPublisher.Row> archived = EventsPublisher.DigestRows(book, NoPosts, [digest]).ToList();

        EventsPublisher.Row post = Assert.Single(rows, row => row.Feed == EventsFeeds.PrivateDigest);
        Assert.Equal(offering.Audience.Text, post.Audience);
        Assert.Equal(("/news/offering", "Church news"), (EventsPayloads.Decode<DigestShownItem>(post.Payload).Link, EventsPayloads.Decode<DigestShownItem>(post.Payload).Words.Label));
        Assert.DoesNotContain(archived, row => row.RecordId.Contains("news:", StringComparison.Ordinal));
        Assert.Equal([NotificationAudience.Holding(RoleMembership.Of(membersRole)), NotificationAudience.Everyone],
            Digests.ForEveryoneWhoSeesIt(book, published, digest.Sent, Start).Select(notification => notification.Audience));
    }

    [Fact]
    public void APictureChosenForTheDigestReplacesTheEventsOwnAndGoesWithItToTheApp()
    {
        DigestItem baptisms = Item("event:baptisms", "2026-11-15");
        baptisms.CalendarWords.Picture = Picture("event's");
        DigestEntry entry = new() { Key = "event:baptisms", Included = true, Changed = [DigestFields.Picture], Words = new DigestWords { Picture = Picture("chosen") }, PictureAt = DigestPictureAt.Across };

        DigestItem merged = Assert.Single(Digests.Merge([baptisms], NoPosts, [entry]));
        SentDigest sent = Digests.ForApp([merged], Digests.EndOf(Start, DigestRange.TwoWeeks));

        Assert.Equal("chosen", merged.Shown.Picture?.Id);
        Assert.Equal(DigestPictureAt.Across, merged.PictureAt);
        Assert.Equal("chosen", sent.Items[0].Item.Picture?.Id);
    }

    private static MediaRef Picture(string id) => new() { Id = id };

    private DigestRecord SentFor(DateOnly start, string key, DateTime sentAt)
    {
        DigestItem item = Item(key, EventDates.Format(start.AddDays(2)));
        item.CalendarIds = [_services.Id.ToString()];
        DateOnly end = Digests.EndOf(start, DigestRange.TwoWeeks);
        return new DigestRecord { Start = start, End = end, SentAt = sentAt, Sent = Digests.ForApp([item], end) };
    }

    private static List<DigestItem> Candidates(EventsBook book) =>
        Digests.Candidates(book, Start, Digests.EndOf(Start, DigestRange.TwoWeeks), Now);

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
