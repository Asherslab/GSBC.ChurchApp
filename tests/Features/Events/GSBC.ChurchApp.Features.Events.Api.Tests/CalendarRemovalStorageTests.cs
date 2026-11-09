using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("Removing a calendar deletes its rows in Postgres in one transaction and writes a deletion to the feeds for each row phones hold")]
public sealed class CalendarRemovalStorageTests : EventsHost
{
    [Fact]
    public async Task ARemovedCalendarLeavesNothingBehindAndPhonesAreToldItsGone()
    {
        CalendarSettings kids = await CalendarAsync("Kids", Audience.Everyone);
        CalendarSettings wholeChurch = await CalendarAsync("Whole church", Audience.Everyone);
        Guid kidsId = Guid.Parse(kids.Calendar.Id), wholeChurchId = Guid.Parse(wholeChurch.Calendar.Id);
        EventRecord camp = OneOff(kidsId, "Kids camp"), picnic = OneOff(wholeChurchId, "Park picnic");
        RegularEventRecord impactKids = new()
        {
            Id = Guid.CreateVersion7(), CalendarId = kidsId, Fields = new EventFields { Title = "Impact Kids" },
            Pattern = Weekly(DayOfWeek.Friday), StartMinute = 18 * 60, Showing = Showing.Shown,
        };
        await WithAsync(async provider =>
        {
            EventsDbContext db = provider.GetRequiredService<EventsDbContext>();
            db.AddRange(camp, picnic, impactKids);
            db.Specials.Add(new SpecialRecord { RegularEventId = impactKids.Id, Date = Today.AddDays(5), Changed = [EventFieldKeys.Title], Fields = new EventFields { Title = "Games night" } });
            db.Breaks.Add(new BreakRecord { Id = Guid.CreateVersion7(), Name = "Camp weekend", StartDate = Today.AddDays(12), EndDate = Today.AddDays(13), RegularEventId = impactKids.Id });
            db.Showings.AddRange(
                new ShowingRecord { Id = Guid.CreateVersion7(), CalendarId = wholeChurchId, EventId = camp.Id, AgreedAt = Clock.GetUtcNow().UtcDateTime },
                new ShowingRecord { Id = Guid.CreateVersion7(), CalendarId = kidsId, EventId = picnic.Id, AgreedAt = Clock.GetUtcNow().UtcDateTime });
            db.DetailsReminders.Add(new DetailsReminderRecord { OccurrenceId = "reminded", DaysBefore = 7, RegularEventId = impactKids.Id, Date = Today.AddDays(5) });
            db.Digests.Add(new DigestRecord { Id = Guid.CreateVersion7(), Start = Today.AddDays(1), End = Today.AddDays(14), CalendarIds = [kidsId, wholeChurchId] });
            await db.SaveChangesAsync(Cancel);
            return true;
        });
        await PublishAsync();
        await Roles.GiveAsFeatureAsync(Guid.CreateVersion7(), EventsPermissions.EditorRole, kids.Calendar.Id, null, Cancel);
        await Roles.GiveAsync(Guid.CreateVersion7(), EventsPermissions.AdderRole, kids.Calendar.Id, null, Cancel);
        await Roles.GiveAsFeatureAsync(Guid.CreateVersion7(), EventsPermissions.OrganiserRole, impactKids.Id.ToString(), null, Cancel);
        CalendarRemoval asked = await WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>().RemovalAsync(new CalendarToRemove { CalendarId = kids.Calendar.Id }, As(Person())));

        await WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>().RemoveCalendarAsync(new CalendarToRemove { CalendarId = kids.Calendar.Id }, As(Person())));

        Assert.Equal((1, 1, 1), (asked.Events, asked.RegularEvents, asked.AddedBy));
        Assert.Equal([wholeChurch.Calendar.Id], (await LiveRowsAsync(EventsFeeds.Calendars)).Select(row => row.RecordId));
        EventOccurrence left = Assert.Single(await OccurrencesAsync(EventsFeeds.Public));
        Assert.Equal(("Park picnic", 0), (left.Fields.Title, left.AlsoOn.Count));
        await WithAsync(async provider =>
        {
            EventsDbContext db = provider.GetRequiredService<EventsDbContext>();
            Assert.Empty(await db.RegularEvents.ToListAsync(Cancel));
            Assert.Equal([picnic.Id], await db.Events.Select(record => record.Id).ToListAsync(Cancel));
            Assert.Empty(await db.Specials.ToListAsync(Cancel));
            Assert.Empty(await db.Breaks.ToListAsync(Cancel));
            Assert.Empty(await db.Showings.ToListAsync(Cancel));
            Assert.Empty(await db.DetailsReminders.ToListAsync(Cancel));
            Assert.Equal([wholeChurchId], (await db.Digests.SingleAsync(Cancel)).CalendarIds);
            return true;
        });
        Assert.Equal(0, await Roles.HoldingCountAsync(EventsPermissions.EditorRole, kids.Calendar.Id, Cancel));
        Assert.Equal(0, await Roles.HoldingCountAsync(EventsPermissions.AdderRole, kids.Calendar.Id, Cancel));
        Assert.Equal(0, await Roles.HoldingCountAsync(EventsPermissions.OrganiserRole, impactKids.Id.ToString(), Cancel));
    }

    private static EventRecord OneOff(Guid calendarId, string title) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendarId, Fields = new EventFields { Title = title }, StartDate = Today.AddDays(6), StartMinute = 10 * 60,
        Showing = Showing.Shown,
    };
}
