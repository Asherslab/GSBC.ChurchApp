using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class CalendarAudienceTests : EventsHost
{
    [Fact]
    public async Task NothingAboutACalendarForSignedInPeopleReachesASignedOutPhone()
    {
        CalendarSettings kidsTeam = await CalendarAsync("Kids team", Audience.SignedIn);
        ClaimsPrincipal leader = Person((EventsPermissions.EditNode, kidsTeam.Calendar.Id));
        EventDetail night = await EditingAsync(leader, (editing, context) => editing.SaveEventAsync(new EventSave
        {
            CalendarId = kidsTeam.Calendar.Id, Fields = new EventFields { Title = "Helpers night" },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(3)), StartMinute = 19 * 60 }, Showing = Showing.Shown,
        }, context));

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => WithAsync(provider => provider.GetRequiredService<CalendarFollowing>()
            .ChooseAsync(Guid.CreateVersion7(), new ClaimsPrincipal(), new CalendarChoice { CalendarId = kidsTeam.Calendar.Id, InYourWeek = true }, Cancel)));
        await EditingAsync(leader, (editing, context) => editing.TellPeopleAsync(new TellPeople
        {
            CalendarId = kidsTeam.Calendar.Id, OccurrenceId = EventDates.OccurrenceId(Guid.Parse(night.Id)), Body = "Now in the Fellowship Area",
        }, context));

        Assert.Equal(StatusCode.Unauthenticated, refused.StatusCode);
        Assert.Empty(Roles.Held);
        Assert.Equal(NotificationAudience.SignedIn, Assert.Single(Notifications.Raised).Audience);
    }

    [Fact]
    public async Task AgreeingToShowAnEventElsewhereSaysWhenItWouldReachMorePeopleAndWhom()
    {
        Guid helpers = Guid.CreateVersion7();
        CalendarSettings kidsTeam = await CalendarAsync("Kids team", Audience.HoldingRole(helpers));
        CalendarSettings kidsLeaders = await CalendarAsync("Kids leaders", Audience.HoldingRole(helpers));
        CalendarSettings wholeChurch = await CalendarAsync("Whole church", Audience.Everyone);
        ClaimsPrincipal leader = Person((EventsPermissions.EditNode, kidsTeam.Calendar.Id));
        EventDetail night = await EditingAsync(leader, (editing, context) => editing.SaveEventAsync(new EventSave
        {
            CalendarId = kidsTeam.Calendar.Id, Fields = new EventFields { Title = "Helpers night" },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(3)), StartMinute = 19 * 60 }, Showing = Showing.Shown,
        }, context));
        foreach (CalendarSettings other in new[] { kidsLeaders, wholeChurch })
            await EditingAsync(leader, (editing, context) => editing.ShowOnAsync(new ShowOnRequest
            {
                CalendarId = kidsTeam.Calendar.Id, EventId = night.Id, OtherCalendarId = other.Calendar.Id, Show = true,
            }, context));

        CalendarDesk office = await EditingAsync(Person((EventsPermissions.EditNode, null)), (editing, context) => editing.DeskAsync(new Done(), context));

        ShowingRequest toEveryone = office.AskedToShow.Single(request => request.CalendarId == wholeChurch.Calendar.Id);
        ShowingRequest toLeaders = office.AskedToShow.Single(request => request.CalendarId == kidsLeaders.Calendar.Id);
        Assert.Equal((true, CalendarAudiences.EveryoneWords), (toEveryone.Widens, toEveryone.NewlyReaches));
        Assert.Equal((false, "", 0), (toLeaders.Widens, toLeaders.NewlyReaches, toLeaders.NewlyReachesRoleIds.Count));
    }

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
