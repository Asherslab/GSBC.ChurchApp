using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

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

        Clock.UtcNow = Clock.UtcNow.AddDays(7);
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

    private async Task<List<string>> DeletedAsync(string feed) =>
        await WithAsync(provider => provider.GetRequiredService<Data.EventsDbContext>().FeedChanges
            .Where(change => change.Feed == feed && change.Deleted).Select(change => change.RecordId).ToListAsync(Cancel));
}
