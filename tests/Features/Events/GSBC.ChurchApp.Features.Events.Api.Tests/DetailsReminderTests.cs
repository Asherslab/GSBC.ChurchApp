using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class DetailsReminderTests : EventsHost
{
    [Fact]
    public async Task OrganisersAreRemindedOnceAtSevenAndTwoDaysAndTheReminderSettlesWhenThePlaceArrives()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        var editor = Person((EventsPermissions.EditNode, outreach.Calendar.Id));
        RegularEventSaved saved = await EditingAsync(editor, (editing, context) => editing.SaveRegularEventAsync(new RegularEventSave
        {
            CalendarId = outreach.Calendar.Id, Fields = new EventFields { Title = "Outreach Saturday" },
            Pattern = Weekly(DayOfWeek.Saturday), StartMinute = 9 * 60, Showing = Showing.Shown,
            Needs = [EventFieldKeys.Place], RemindDaysBefore = [7, 2],
        }, context));
        DateOnly saturday = Today.AddDays(6);
        string subject = DetailsReminders.Subject(EventDates.OccurrenceId(Guid.Parse(saved.Detail!.Id), saturday));

        await RemindAsync();
        await RemindAsync();

        Notification first = Assert.Single(Notifications.Raised);
        Assert.Equal(subject, first.Subject);
        Assert.Equal(NotificationAudience.Holding(EventsPermissions.Edit, outreach.Calendar.Id), first.Audience);
        Assert.Equal("Outreach Saturday on Saturday 14 November needs a place", first.Title);

        Clock.UtcNow = Clock.UtcNow.AddDays(4);
        await RemindAsync();
        await RemindAsync();

        Assert.Equal([subject, subject], Notifications.Raised.Select(raised => raised.Subject));
        Assert.Empty(Notifications.Settled);

        await EditingAsync(editor, (editing, context) => editing.SaveDateAsync(new DateSave
        {
            CalendarId = outreach.Calendar.Id, RegularEventId = saved.Detail.Id, Date = EventDates.Format(saturday),
            Fields = new EventFields { Title = "Outreach Saturday", Place = new EventPlace { Address = "Teralba Park" } }, StartMinute = 9 * 60,
        }, context));
        await RemindAsync();

        Assert.Equal([subject], Notifications.Settled);
        Assert.Equal(2, Notifications.Raised.Count);
    }

    private Task RemindAsync() => WithAsync(provider => provider.GetRequiredService<DetailsReminders>().RemindAsync(Cancel));
}
