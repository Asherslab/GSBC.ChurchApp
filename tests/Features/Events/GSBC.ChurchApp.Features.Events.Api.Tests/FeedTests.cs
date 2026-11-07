using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class FeedTests : EventsHost
{
    [Fact]
    public async Task PhonesHoldThirtyDaysBackAndTwelveMonthsAheadAndADateLeavingTheWindowIsDeleted()
    {
        CalendarSettings services = await CalendarAsync("Sunday services", Audience.Everyone);
        await SaveRegularAsync(services, "Morning service", Weekly(DayOfWeek.Sunday, Today.AddYears(-1)));

        List<EventOccurrence> held = await OccurrencesAsync(EventsFeeds.Public);
        Assert.Equal(Today.AddDays(-28), EventDates.Parse(held[0].Date));
        Assert.Equal(new DateOnly(2027, 11, 7), EventDates.Parse(held[^1].Date));

        Clock.UtcNow = Clock.UtcNow.AddDays(7);
        await PublishAsync();

        List<EventOccurrence> later = await OccurrencesAsync(EventsFeeds.Public);
        Assert.Equal(Today.AddDays(-21), EventDates.Parse(later[0].Date));
        Assert.Contains(await DeletedAsync(EventsFeeds.Public), id => id == held[0].Id);
    }

    [Fact]
    public async Task APrivateCalendarsEventsNeverTravelInThePublicFeedAndMoveOutWhenACalendarBecomesPrivate()
    {
        Guid churchMember = Guid.CreateVersion7();
        CalendarSettings members = await CalendarAsync("Church members", Audience.HoldingRole(churchMember));
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        await SaveEventAsync(members, "Annual general meeting");
        await SaveEventAsync(kids, "Break-up night");

        Assert.Equal(["Break-up night"], (await OccurrencesAsync(EventsFeeds.Public)).Select(occurrence => occurrence.Fields.Title));
        FeedChangeRecord agm = Assert.Single(await LiveRowsAsync(EventsFeeds.Private));
        Assert.Equal(Audience.HoldingRole(churchMember).Text, agm.ScopeId);
        Assert.Equal(["Kids"], (await LiveRowsAsync(EventsFeeds.Calendars)).Select(row => EventsPayloads.Decode<CalendarInfo>(row.Payload).Name));

        kids.Calendar.Audience = Audience.SignedIn.Text;
        await WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>().SaveCalendarAsync(kids, As(Person())));

        Assert.Empty(await LiveRowsAsync(EventsFeeds.Public));
        Assert.Empty(await LiveRowsAsync(EventsFeeds.Calendars));
        Assert.Equal(2, (await LiveRowsAsync(EventsFeeds.Private)).Count);
    }

    [Fact]
    public async Task ContactDetailsTravelOnlyToPeopleSignedIn()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        await SaveEventAsync(outreach, "Outreach Saturday", new EventContact { Name = "Sam Okafor", Phone = "0400 000 002" });

        EventOccurrence shown = Assert.Single(await OccurrencesAsync(EventsFeeds.Public));
        Assert.Equal("Sam Okafor", Assert.Single(shown.Fields.Contacts).Name);
        Assert.Equal("", shown.Fields.Contacts[0].Phone);
        FeedChangeRecord contacts = Assert.Single(await LiveRowsAsync(EventsFeeds.Contacts));
        Assert.Equal(Audience.SignedIn.Text, contacts.ScopeId);
    }

    private async Task<List<string>> DeletedAsync(string feed) =>
        await WithAsync(provider => provider.GetRequiredService<Api.Data.EventsDbContext>().FeedChanges
            .Where(change => change.Feed == feed && change.Deleted).Select(change => change.RecordId).ToListAsync(Cancel));

    private Task SaveRegularAsync(CalendarSettings calendar, string title, RegularPattern pattern) =>
        EditingAsync(Person((EventsPermissions.EditNode, calendar.Calendar.Id)), (editing, context) => editing.SaveRegularEventAsync(new RegularEventSave
        {
            CalendarId = calendar.Calendar.Id, Fields = new EventFields { Title = title }, Pattern = pattern, StartMinute = 10 * 60 + 30, Showing = Showing.Shown,
        }, context));

    private Task SaveEventAsync(CalendarSettings calendar, string title, params EventContact[] contacts) =>
        EditingAsync(Person((EventsPermissions.EditNode, calendar.Calendar.Id)), (editing, context) => editing.SaveEventAsync(new EventSave
        {
            CalendarId = calendar.Calendar.Id, Fields = new EventFields { Title = title, Contacts = [.. contacts] },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(4)), StartMinute = 19 * 60 + 30 }, Showing = Showing.Shown,
        }, context));
}
