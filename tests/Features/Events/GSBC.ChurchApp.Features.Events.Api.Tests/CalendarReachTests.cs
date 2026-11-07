using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("An event is looked up in Postgres only through its own calendar, so an editor of another calendar can't read, move or list it")]
public sealed class CalendarReachTests : EventsHost
{
    [Fact]
    public async Task AnEditorOfOneCalendarCantReachAnotherCalendarsEventsThroughTheirOwn()
    {
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        CalendarSettings members = await CalendarAsync("Church members", Audience.SignedIn);
        ClaimsPrincipal office = Person((EventsPermissions.EditNode, null));
        EventDetail agm = await EditingAsync(office, (editing, context) => editing.SaveEventAsync(Event(members, "Annual general meeting"), context));
        ClaimsPrincipal kidsLeader = Person((EventsPermissions.EditNode, kids.Calendar.Id));

        RpcException throughKids = await Assert.ThrowsAsync<RpcException>(() => EditingAsync(kidsLeader, (editing, context) =>
            editing.SaveEventAsync(Event(kids, "Moved into Kids", agm.Id), context)));
        RpcException readThroughKids = await Assert.ThrowsAsync<RpcException>(() => EditingAsync(kidsLeader, (editing, context) =>
            editing.EventAsync(new EventRef { CalendarId = kids.Calendar.Id, EventId = agm.Id }, context)));
        CalendarDesk desk = await EditingAsync(kidsLeader, (editing, context) => editing.DeskAsync(new Done(), context));
        Agenda agenda = await EditingAsync(kidsLeader, (editing, context) => editing.AgendaAsync(new AgendaQuery { CalendarIds = [members.Calendar.Id] }, context));

        Assert.Equal(StatusCode.PermissionDenied, throughKids.StatusCode);
        Assert.Equal(StatusCode.NotFound, readThroughKids.StatusCode);
        Assert.Equal(["Kids"], desk.Calendars.Select(calendar => calendar.Settings.Calendar.Name));
        Assert.Empty(agenda.Occurrences);
    }

    private static EventSave Event(CalendarSettings calendar, string title, string eventId = "") => new()
    {
        CalendarId = calendar.Calendar.Id, EventId = eventId, Fields = new EventFields { Title = title },
        Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(10)), StartMinute = 19 * 60 }, Showing = Showing.Shown,
    };
}
