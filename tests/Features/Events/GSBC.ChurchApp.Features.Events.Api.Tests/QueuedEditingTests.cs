using System.Security.Claims;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("A phone's queued edits are written to Postgres once per request id, merged with whatever changed meanwhile, and its editor rows are filtered by who edits each calendar")]
public sealed class QueuedEditingTests : EventsHost
{
    [Fact]
    public async Task AQueuedSaveSentAgainAfterItsReplyWasLostIsAppliedOnce()
    {
        CalendarSettings outreach = await CalendarAsync("Outreach", Audience.Everyone);
        ClaimsPrincipal leader = Person((EventsPermissions.EditNode, outreach.Calendar.Id));
        string newId = Guid.CreateVersion7().ToString();
        EventSave picnic = new()
        {
            CalendarId = outreach.Calendar.Id, NewId = newId, Fields = new EventFields { Title = "Park picnic" },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(6)) }, Showing = Showing.Shown,
        };
        TellPeople tell = new() { CalendarId = outreach.Calendar.Id, OccurrenceId = EventDates.OccurrenceId(Guid.Parse(newId)), Title = "Picnic on Saturday" };

        for (int sent = 0; sent < 2; sent++)
        {
            await QueuedAsync(leader, "save-1", (editing, context) => editing.SaveEventAsync(picnic, context));
            await QueuedAsync(leader, "tell-1", (editing, context) => editing.TellPeopleAsync(tell, context));
        }

        List<EventRecord> saved = await WithAsync(provider => provider.GetRequiredService<EventsDbContext>().Events.AsNoTracking().ToListAsync(Cancel));
        Assert.Equal(newId, Assert.Single(saved).Id.ToString());
        Assert.Single(Notifications.Raised, notification => notification.Title == "Picnic on Saturday");
    }

    [Fact]
    public async Task TwoEditorsChangingDifferentThingsBothKeepTheirsAndTheLaterWinsOnTheSameThing()
    {
        CalendarSettings services = await CalendarAsync("Sunday services", Audience.Everyone);
        ClaimsPrincipal ann = Person((EventsPermissions.EditNode, services.Calendar.Id));
        ClaimsPrincipal ben = Person((EventsPermissions.EditNode, services.Calendar.Id));
        EventSave started = new()
        {
            CalendarId = services.Calendar.Id, Fields = new EventFields { Title = "Carols night", Place = new EventPlace { Name = "Auditorium" } },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(10)), StartMinute = 18 * 60, EndMinute = 20 * 60 }, Showing = Showing.Shown,
        };
        started.EventId = (await EditingAsync(ann, (editing, context) => editing.SaveEventAsync(started, context))).Id;

        EventSave annRenames = Edited(started, save => (save.Fields.Title, save.Times.StartMinute) = ("Carols by candlelight", 19 * 60));
        EventSave benMoves = Edited(started, save => (save.Fields.Place, save.Times.StartMinute) = (new EventPlace { Name = "Fellowship Area" }, 17 * 60));
        await QueuedAsync(ann, "ann-1", (editing, context) => editing.SaveEventAsync(annRenames, context));
        EventDetail after = await QueuedAsync(ben, "ben-1", (editing, context) => editing.SaveEventAsync(benMoves, context));

        Assert.Equal(("Carols by candlelight", "Fellowship Area", 17 * 60), (after.Fields.Title, after.Fields.Place!.Name, after.Times.StartMinute));
    }

    [Fact]
    public async Task EditorRowsReachOnlyTheEditorsOfTheirCalendarAndANotYetShownEventStaysOutOfThePublicFeed()
    {
        CalendarSettings youth = await CalendarAsync("Youth", Audience.Everyone);
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        ClaimsPrincipal youthLeader = Person((EventsPermissions.EditNode, youth.Calendar.Id));
        ClaimsPrincipal kidsLeader = Person((EventsPermissions.EditNode, kids.Calendar.Id));
        ClaimsPrincipal member = Person();
        EventSave camp = new()
        {
            CalendarId = youth.Calendar.Id, Fields = new EventFields { Title = "Youth camp" },
            Times = new EventTimes { StartDate = EventDates.Format(Today.AddDays(20)) }, Showing = Showing.SavedForLater,
        };
        await EditingAsync(youthLeader, (editing, context) => editing.SaveEventAsync(camp, context));

        Assert.Equal(["calendar/" + youth.Calendar.Id, "event/"], Kinds(await SeenByAsync(youthLeader), youth.Calendar.Id));
        Assert.DoesNotContain(await SeenByAsync(kidsLeader), row => row.ScopeId == youth.Calendar.Id);
        Assert.Empty(await SeenByAsync(member));
        Assert.Empty(await OccurrencesAsync(EventsFeeds.Public));
    }

    private async Task<List<FeedChangeRecord>> SeenByAsync(ClaimsPrincipal person) =>
        await FeedVisibility.VisibleRowsAsync(new FeedDefinition(EventsFeeds.Editing, FeedAudience.Holding(EventsPermissions.Edit)),
            await LiveRowsAsync(EventsFeeds.Editing), person, Access, Cancel);

    private static List<string> Kinds(IEnumerable<FeedChangeRecord> rows, string calendarId) =>
        rows.Where(row => row.ScopeId == calendarId).Select(row => row.RecordId.StartsWith("event/", StringComparison.Ordinal) ? "event/" : row.RecordId).Order(StringComparer.Ordinal).ToList();

    private static EventSave Edited(EventSave started, Action<EventSave> change)
    {
        EventSave edited = EditSaves.Copy(started);
        change(edited);
        edited.Base = EditSaves.Copy(started);
        return edited;
    }
}
