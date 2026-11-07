using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class EditingTests : EventsHost
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

    [Fact]
    public async Task OnACalendarThatNeedsAPublisherAnEditorAsksAndAPublisherShowsIt()
    {
        CalendarSettings youth = await CalendarAsync("Youth", Audience.Everyone, needsPublisher: true);
        ClaimsPrincipal leader = Person((EventsPermissions.EditNode, youth.Calendar.Id));
        ClaimsPrincipal publisher = Person((EventsPermissions.EditNode, youth.Calendar.Id), (EventsPermissions.PublishNode, youth.Calendar.Id));

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => EditingAsync(leader, (editing, context) =>
            editing.SaveEventAsync(Event(youth, "Youth camp"), context)));
        EventSave forLater = Event(youth, "Youth camp");
        forLater.Showing = Showing.SavedForLater;
        EventDetail saved = await EditingAsync(leader, (editing, context) => editing.SaveEventAsync(forLater, context));
        ItemRef camp = new() { CalendarId = youth.Calendar.Id, EventId = saved.Id };
        await EditingAsync(leader, (editing, context) => editing.AskToPublishAsync(camp, context));
        AskedList asked = await EditingAsync(publisher, (editing, context) => editing.AskedToPublishAsync(new Done(), context));
        Assert.Empty(await OccurrencesAsync(EventsFeeds.Public));

        await EditingAsync(publisher, (editing, context) => editing.PublishAsync(camp, context));

        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        Assert.Equal("Youth camp", Assert.Single(asked.Items).Title);
        Notification told = Assert.Single(Notifications.Raised);
        Assert.Equal(EventsNotifications.AskedToPublish, told.Kind);
        Assert.Equal(NotificationAudience.Holding(EventsPermissions.Publish, youth.Calendar.Id), told.Audience);
        Assert.Equal("Youth camp", Assert.Single(await OccurrencesAsync(EventsFeeds.Public)).Fields.Title);
        Assert.Empty((await EditingAsync(publisher, (editing, context) => editing.AskedToPublishAsync(new Done(), context))).Items);
    }

    [Fact]
    public async Task ThisDateOnlyChangesOneDateAndEveryDateChangesTheRestButWarnsFirstAboutWhatASpecialKeeps()
    {
        CalendarSettings services = await CalendarAsync("Sunday services", Audience.Everyone);
        ClaimsPrincipal office = Person((EventsPermissions.EditNode, services.Calendar.Id));
        RegularEventSave every = new()
        {
            CalendarId = services.Calendar.Id, Fields = new EventFields { Title = "Morning service", Place = new EventPlace { Name = "Auditorium" } },
            Pattern = Weekly(DayOfWeek.Sunday), StartMinute = 10 * 60 + 30, EndMinute = 12 * 60, Showing = Showing.Shown,
        };
        RegularEventDetail morning = (await EditingAsync(office, (editing, context) => editing.SaveRegularEventAsync(every, context))).Detail!;
        DateOnly anniversary = Today.AddDays(7);

        await EditingAsync(office, (editing, context) => editing.SaveDateAsync(new DateSave
        {
            CalendarId = services.Calendar.Id, RegularEventId = morning.Id, Date = EventDates.Format(anniversary),
            Fields = new EventFields { Title = "Anniversary Sunday Service", Place = new EventPlace { Name = "Auditorium" } }, StartMinute = 9 * 60, EndMinute = 12 * 60,
        }, context));

        every.RegularEventId = morning.Id;
        every.StartMinute = 11 * 60;
        every.Fields.Place = new EventPlace { Name = "Fellowship Area" };
        RegularEventSaved warned = await EditingAsync(office, (editing, context) => editing.SaveRegularEventAsync(every, context));
        every.KeepOwnConfirmed = true;
        await EditingAsync(office, (editing, context) => editing.SaveRegularEventAsync(every, context));

        Assert.False(warned.Saved);
        KeptValue kept = Assert.Single(warned.KeepOwn);
        Assert.Equal((EventFieldKeys.Time, "9am–12pm"), (kept.Field, kept.Value));
        List<EventOccurrence> dates = await OccurrencesAsync(EventsFeeds.Public);
        EventOccurrence special = dates.Single(date => date.Date == EventDates.Format(anniversary));
        EventOccurrence ordinary = dates.Single(date => date.Date == EventDates.Format(anniversary.AddDays(7)));
        Assert.Equal(("Anniversary Sunday Service", 9, "Fellowship Area"), (special.Fields.Title, Hour(special), special.Fields.Place!.Name));
        Assert.Equal(("Morning service", 11, "Fellowship Area"), (ordinary.Fields.Title, Hour(ordinary), ordinary.Fields.Place!.Name));
        Assert.Equal("Now 11am–12pm · Now at Fellowship Area", ordinary.ChangeNote);
    }

    private static int Hour(EventOccurrence occurrence) => Platform.Common.ChurchTime.ToChurch(occurrence.StartsAt).Hour;

    private static EventSave Event(CalendarSettings calendar, string title, string eventId = "") => new()
    {
        CalendarId = calendar.Calendar.Id, EventId = eventId, Fields = new EventFields { Title = title },
        Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(10)), StartMinute = 19 * 60 }, Showing = Showing.Shown,
    };
}
