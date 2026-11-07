using System.Security.Claims;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("A copy on another calendar is written to the public feed in Postgres only while its home event is published")]
public sealed class ShowingElsewhereTests : EventsHost
{
    [Fact]
    public async Task AnEventShownOnAnotherCalendarAppearsThereOnlyWhileItsHomeEventIsPublished()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        CalendarSettings wholeChurch = await CalendarAsync("Whole church", Audience.Everyone);
        ClaimsPrincipal office = Person((EventsPermissions.EditNode, null));
        EventSave picnic = new()
        {
            CalendarId = outreach.Calendar.Id, Fields = new EventFields { Title = "Park picnic" },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(6)) }, Showing = Showing.SavedForLater,
        };
        picnic.EventId = (await EditingAsync(office, (editing, context) => editing.SaveEventAsync(picnic, context))).Id;
        await EditingAsync(office, (editing, context) => editing.ShowOnAsync(new ShowOnRequest
        {
            CalendarId = outreach.Calendar.Id, EventId = picnic.EventId, OtherCalendarId = wholeChurch.Calendar.Id, Show = true,
        }, context));
        List<EventOccurrence> beforePublishing = await OccurrencesAsync(EventsFeeds.Public);

        picnic.Showing = Showing.Shown;
        await EditingAsync(office, (editing, context) => editing.SaveEventAsync(picnic, context));
        List<string> published = Assert.Single(await OccurrencesAsync(EventsFeeds.Public)).AlsoOn;
        picnic.Showing = Showing.SavedForLater;
        await EditingAsync(office, (editing, context) => editing.SaveEventAsync(picnic, context));
        List<EventOccurrence> unpublished = await OccurrencesAsync(EventsFeeds.Public);
        picnic.Showing = Showing.Shown;
        await EditingAsync(office, (editing, context) => editing.SaveEventAsync(picnic, context));
        await EditingAsync(office, (editing, context) => editing.DeleteEventAsync(new EventRef { CalendarId = outreach.Calendar.Id, EventId = picnic.EventId }, context));

        Assert.Empty(beforePublishing);
        Assert.Equal([wholeChurch.Calendar.Id], published);
        Assert.Empty(unpublished);
        Assert.Empty(await OccurrencesAsync(EventsFeeds.Public));
    }
}
