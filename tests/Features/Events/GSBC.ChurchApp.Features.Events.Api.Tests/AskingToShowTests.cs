using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("A calendar asking for another calendar's event is kept in Postgres as waiting, and reaches the feed only once the home calendar's editors agree")]
public sealed class AskingToShowTests : EventsHost
{
    [Fact]
    public async Task AnEventAskedForFromAnotherCalendarWaitsForItsHomeCalendarsEditors()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        ClaimsPrincipal outreachLeader = Person((EventsPermissions.EditNode, outreach.Calendar.Id));
        ClaimsPrincipal kidsLeader = Person((EventsPermissions.EditNode, kids.Calendar.Id));
        string picnic = await ShownEventAsync(outreachLeader, outreach, "Park picnic");
        ShowHereRequest ask = new() { CalendarId = kids.Calendar.Id, HomeCalendarId = outreach.Calendar.Id, EventId = picnic, Show = true };

        ShowHereResult asked = await EditingAsync(kidsLeader, (editing, context) => editing.ShowHereAsync(ask, context));
        List<string> whileAsked = Assert.Single(await OccurrencesAsync(EventsFeeds.Public)).AlsoOn;
        RpcException agreeingToItself = await Assert.ThrowsAsync<RpcException>(() => EditingAsync(kidsLeader, (editing, context) =>
            editing.AnswerShowingAsync(new ShowingAnswer { CalendarId = kids.Calendar.Id, EventId = picnic, Agree = true }, context)));
        ShowingRequest waiting = Assert.Single((await EditingAsync(outreachLeader, (editing, context) => editing.DeskAsync(new Done(), context))).AskedToShow);
        await EditingAsync(outreachLeader, (editing, context) => editing.AnswerAskToShowAsync(new AskToShowAnswer
        {
            CalendarId = outreach.Calendar.Id, OtherCalendarId = kids.Calendar.Id, EventId = picnic, Agree = true,
        }, context));

        Assert.False(asked.Agreed);
        Assert.Empty(whileAsked);
        Assert.Equal(StatusCode.PermissionDenied, agreeingToItself.StatusCode);
        Assert.Equal((true, "Kids", "Outreach"), (waiting.AskedByOther, waiting.CalendarName, waiting.HomeCalendarName));
        Assert.Equal([kids.Calendar.Id], Assert.Single(await OccurrencesAsync(EventsFeeds.Public)).AlsoOn);
    }

    [Fact]
    public async Task AnEditorOfBothCalendarsShowsItAtOnce()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        CalendarSettings wholeChurch = await CalendarAsync("Whole church", Audience.Everyone);
        ClaimsPrincipal office = Person((EventsPermissions.EditNode, null));
        string picnic = await ShownEventAsync(office, outreach, "Park picnic");

        ShowHereResult shown = await EditingAsync(office, (editing, context) => editing.ShowHereAsync(new ShowHereRequest
        {
            CalendarId = wholeChurch.Calendar.Id, HomeCalendarId = outreach.Calendar.Id, EventId = picnic, Show = true,
        }, context));

        Assert.True(shown.Agreed);
        Assert.Equal([wholeChurch.Calendar.Id], Assert.Single(await OccurrencesAsync(EventsFeeds.Public)).AlsoOn);
    }

    private async Task<string> ShownEventAsync(ClaimsPrincipal editor, CalendarSettings calendar, string title) =>
        (await EditingAsync(editor, (editing, context) => editing.SaveEventAsync(new EventSave
        {
            CalendarId = calendar.Calendar.Id, Fields = new EventFields { Title = title },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(6)) }, Showing = Showing.Shown,
        }, context))).Id;
}
