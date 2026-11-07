using System.Security.Claims;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("Publishing writes each calendar's dates into the feeds in Postgres, deleting the rows that left the window or the audience, and showing what a publisher approves")]
public sealed class PublishingTests : EventsHost
{
    [Fact]
    public async Task PublishingSendsADateThatLeftTheWindowAsADelete()
    {
        CalendarSettings services = await CalendarAsync("Sunday services", Audience.Everyone);
        await EditingAsync(Person((EventsPermissions.EditNode, services.Calendar.Id)), (editing, context) => editing.SaveRegularEventAsync(new RegularEventSave
        {
            CalendarId = services.Calendar.Id, Fields = new EventFields { Title = "Morning service" }, Pattern = Weekly(DayOfWeek.Sunday, Today.AddYears(-1)),
            StartMinute = 10 * 60 + 30, Showing = Showing.Shown,
        }, context));
        string oldest = (await OccurrencesAsync(EventsFeeds.Public))[0].Id;

        Clock.Advance(TimeSpan.FromDays(7));
        await PublishAsync();

        Assert.DoesNotContain(await OccurrencesAsync(EventsFeeds.Public), occurrence => occurrence.Id == oldest);
        Assert.Contains(oldest, await DeletedAsync(EventsFeeds.Public));
    }

    [Fact]
    public async Task PublishingDeletesACalendarsRowsFromThePublicFeedsWhenItBecomesPrivate()
    {
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        await EditingAsync(Person((EventsPermissions.EditNode, kids.Calendar.Id)), (editing, context) => editing.SaveEventAsync(new EventSave
        {
            CalendarId = kids.Calendar.Id, Fields = new EventFields { Title = "Break-up night" },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(4)), StartMinute = 19 * 60 }, Showing = Showing.Shown,
        }, context));

        kids.Calendar.Audience = Audience.SignedIn.Text;
        await WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>().SaveCalendarAsync(kids, As(Person())));

        Assert.Empty(await LiveRowsAsync(EventsFeeds.Public));
        Assert.Empty(await LiveRowsAsync(EventsFeeds.Calendars));
        Assert.Equal(["Break-up night"], (await OccurrencesAsync(EventsFeeds.Private)).Select(occurrence => occurrence.Fields.Title));
        Assert.Equal(2, (await DeletedAsync(EventsFeeds.Public)).Count + (await DeletedAsync(EventsFeeds.Calendars)).Count);
    }

    [Fact]
    public async Task AskingToPublishTellsTheCalendarsPublishersAndPublishingShowsItToEveryone()
    {
        CalendarSettings youth = await CalendarAsync("Youth", Audience.Everyone, needsPublisher: true);
        ClaimsPrincipal leader = Person((EventsPermissions.EditNode, youth.Calendar.Id));
        ClaimsPrincipal publisher = Person((EventsPermissions.EditNode, youth.Calendar.Id), (EventsPermissions.PublishNode, youth.Calendar.Id));

        EventSave forLater = Event(youth, "Youth camp");
        forLater.Showing = Showing.SavedForLater;
        EventDetail saved = await EditingAsync(leader, (editing, context) => editing.SaveEventAsync(forLater, context));
        ItemRef camp = new() { CalendarId = youth.Calendar.Id, EventId = saved.Id };
        await EditingAsync(leader, (editing, context) => editing.AskToPublishAsync(camp, context));
        AskedList asked = await EditingAsync(publisher, (editing, context) => editing.AskedToPublishAsync(new Done(), context));
        Assert.Empty(await OccurrencesAsync(EventsFeeds.Public));

        await EditingAsync(publisher, (editing, context) => editing.PublishAsync(camp, context));

        Assert.Equal("Youth camp", Assert.Single(asked.Items).Title);
        Notification told = Assert.Single(Notifications.Raised);
        Assert.Equal(EventsNotifications.AskedToPublish, told.Kind);
        Assert.Equal(NotificationAudience.Holding(EventsPermissions.Publish, youth.Calendar.Id), told.Audience);
        Assert.Equal("Youth camp", Assert.Single(await OccurrencesAsync(EventsFeeds.Public)).Fields.Title);
        Assert.Empty((await EditingAsync(publisher, (editing, context) => editing.AskedToPublishAsync(new Done(), context))).Items);
    }

    private async Task<List<string>> DeletedAsync(string feed) =>
        await WithAsync(provider => provider.GetRequiredService<Data.EventsDbContext>().FeedChanges
            .Where(change => change.Feed == feed && change.Deleted).Select(change => change.RecordId).ToListAsync(Cancel));

    private static EventSave Event(CalendarSettings calendar, string title, string eventId = "") => new()
    {
        CalendarId = calendar.Calendar.Id, EventId = eventId, Fields = new EventFields { Title = title },
        Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(10)), StartMinute = 19 * 60 }, Showing = Showing.Shown,
    };
}
