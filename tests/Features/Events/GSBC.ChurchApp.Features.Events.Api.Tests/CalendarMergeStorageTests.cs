using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

[Integration("Merging a ministry's calendars moves its rows in Postgres in one transaction and writes the change to the feeds phones hold")]
public sealed class CalendarMergeStorageTests : EventsHost
{
    private static readonly Guid Kids = Guid.CreateVersion7();

    [Fact]
    public async Task AMergedCalendarsEventsAndFollowersMoveToTheCalendarItMergedInto()
    {
        Guid camps = await MinistryCalendarAsync("Kids camps"), kids = await MinistryCalendarAsync("Impact Kids"), church = Guid.Parse((await CalendarAsync("Whole church", Audience.Everyone)).Calendar.Id);
        EventRecord camp = OneOff(camps, "Kids camp"), picnic = OneOff(church, "Park picnic"), games = OneOff(kids, "Games night");
        RegularEventRecord fridays = new()
        {
            Id = Guid.CreateVersion7(), CalendarId = camps, Fields = new EventFields { Title = "Impact Kids" }, Pattern = Weekly(DayOfWeek.Friday), StartMinute = 18 * 60,
            Showing = Showing.Shown,
        };
        await WithAsync(async provider =>
        {
            EventsDbContext db = provider.GetRequiredService<EventsDbContext>();
            db.AddRange(camp, picnic, games, fridays);
            DateTime agreed = Clock.GetUtcNow().UtcDateTime;
            db.Showings.AddRange(
                new ShowingRecord { Id = Guid.CreateVersion7(), CalendarId = kids, EventId = camp.Id, AgreedAt = agreed },
                new ShowingRecord { Id = Guid.CreateVersion7(), CalendarId = camps, EventId = picnic.Id, AgreedAt = agreed },
                new ShowingRecord { Id = Guid.CreateVersion7(), CalendarId = kids, EventId = picnic.Id, AgreedAt = agreed },
                new ShowingRecord { Id = Guid.CreateVersion7(), CalendarId = church, EventId = games.Id, AgreedAt = agreed });
            db.Digests.Add(new DigestRecord
            {
                Id = Guid.CreateVersion7(), Start = Today.AddDays(1), End = Today.AddDays(14), CalendarIds = [camps, kids, church],
                Sent = new SentDigest { Items = [new SentDigestItem { CalendarIds = [camps] }] },
            });
            await db.SaveChangesAsync(Cancel);
            return true;
        });
        Guid follower = Guid.CreateVersion7();
        await Roles.GiveAsync(follower, EventsPermissions.AdderRole, camps.ToString(), null, Cancel);

        await WithAsync(async provider =>
        {
            await provider.GetRequiredService<IMinistryCalendars>().MergeAsync(Kids, camps, kids, Cancel);
            return true;
        });

        await WithAsync(async provider =>
        {
            EventsDbContext db = provider.GetRequiredService<EventsDbContext>();
            Assert.False(await db.Calendars.AnyAsync(calendar => calendar.Id == camps, Cancel));
            Assert.Equal(new HashSet<Guid> { camp.Id, games.Id }, (await db.Events.Where(record => record.CalendarId == kids).Select(record => record.Id).ToListAsync(Cancel)).ToHashSet());
            Assert.Equal(kids, (await db.RegularEvents.SingleAsync(Cancel)).CalendarId);
            Assert.Equal(new HashSet<(Guid, Guid)> { (kids, picnic.Id), (church, games.Id) },
                (await db.Showings.ToListAsync(Cancel)).Select(showing => (showing.CalendarId, showing.EventId!.Value)).ToHashSet());
            DigestRecord digest = await db.Digests.SingleAsync(Cancel);
            Assert.Equal([kids, church], digest.CalendarIds);
            Assert.Equal([kids], digest.Sent.Items.Single().CalendarIds);
            return true;
        });
        Assert.Equal([kids.ToString()], (await LiveRowsAsync(EventsFeeds.Calendars)).Select(row => row.RecordId).Where(id => id != church.ToString()));
        Assert.Equal(1, await Roles.HoldingCountAsync(EventsPermissions.AdderRole, kids.ToString(), Cancel));
        Assert.Equal([(camps, kids)], Merges.Merged);
    }

    private async Task<Guid> MinistryCalendarAsync(string name)
    {
        Guid id = Guid.Parse((await CalendarAsync(name, Audience.Everyone)).Calendar.Id);
        await WithAsync(async provider =>
        {
            EventsDbContext db = provider.GetRequiredService<EventsDbContext>();
            (await db.Calendars.SingleAsync(calendar => calendar.Id == id, Cancel)).MinistryId = Kids;
            await db.SaveChangesAsync(Cancel);
            return true;
        });
        return id;
    }

    private static EventRecord OneOff(Guid calendarId, string title) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendarId, Fields = new EventFields { Title = title }, StartDate = Today.AddDays(6), StartMinute = 10 * 60,
        Showing = Showing.Shown,
    };
}
