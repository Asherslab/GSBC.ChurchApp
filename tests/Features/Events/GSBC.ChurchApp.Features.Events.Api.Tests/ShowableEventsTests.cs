using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class ShowableEventsTests
{
    private static readonly DateOnly Today = new(2026, 11, 8);

    [Fact]
    public void YourCalendarIsOfferedWhatsComingOnCalendarsYouCanSeeAndIsntOnYoursYet()
    {
        CalendarRecord kids = Calendar("Kids");
        CalendarRecord outreach = Calendar("Outreach");
        CalendarRecord members = Calendar("Church members");
        CalendarRecord copied = Calendar("School terms", "https://example.com/terms.ics");
        EventRecord picnic = Event(outreach, "Park picnic", Today.AddDays(6));
        EventRecord lastMonth = Event(outreach, "Last month's sausage sizzle", Today.AddDays(-30));
        EventRecord agm = Event(members, "Annual general meeting", Today.AddDays(9));
        EventRecord ownCamp = Event(kids, "Kids camp", Today.AddDays(12));
        EventRecord term = Event(copied, "Term 4 ends", Today.AddDays(30));
        RegularEventRecord doorKnocking = Regular(outreach, "Door knocking");
        RegularEventRecord prayer = Regular(outreach, "Prayer walk");
        ShowingRecord alreadyOnKids = new() { Id = Guid.CreateVersion7(), CalendarId = kids.Id, RegularEventId = doorKnocking.Id, AgreedAt = DateTime.UnixEpoch };
        ShowingRecord askedByKids = new() { Id = Guid.CreateVersion7(), CalendarId = kids.Id, RegularEventId = prayer.Id, AskedByOther = true };
        EventsBook book = new([kids, outreach, members, copied], [picnic, lastMonth, agm, ownCamp, term], [doorKnocking, prayer], [], [], [], [alreadyOnKids, askedByKids]);

        IReadOnlyList<ShowableEvent> offered = ShowableEvents.Offer(book, kids.Id, calendar => calendar != members, Today, "");

        Assert.Equal([("Park picnic", false), ("Prayer walk", true)], offered.Select(item => (item.Item.Fields.Title, item.Asked)).Order());
    }

    private static CalendarRecord Calendar(string name, string? iCal = null) => new()
    {
        Id = Guid.CreateVersion7(), Name = name, Slug = CalendarLinks.SlugOf(name), Audience = Audience.Everyone.Text, ICalSource = iCal,
    };

    private static EventRecord Event(CalendarRecord calendar, string title, DateOnly on) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = new EventFields { Title = title }, StartDate = on, Showing = Showing.Shown,
    };

    private static RegularEventRecord Regular(CalendarRecord calendar, string title) => new()
    {
        Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = new EventFields { Title = title }, Showing = Showing.Shown,
        Pattern = new RegularPattern { Repeats = Repeats.EverySecondWeek, Days = [DayOfWeek.Saturday], StartDate = EventDates.Format(Today) },
    };
}
