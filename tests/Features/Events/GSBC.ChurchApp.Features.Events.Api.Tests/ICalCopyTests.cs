using System.Net;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class ICalCopyTests : EventsHost
{
    private const string YouthCalendar = """
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//Example//Calendar//EN
        BEGIN:VEVENT
        UID:school-camp
        DTSTART;VALUE=DATE:20261201
        DTEND;VALUE=DATE:20261204
        SUMMARY:School camp\, Year 6
        END:VEVENT
        BEGIN:VEVENT
        UID:youth-night
        DTSTART;TZID=Australia/Sydney:20261113T190000
        DTEND;TZID=Australia/Sydney:20261113T210000
        RRULE:FREQ=WEEKLY;BYDAY=FR;COUNT=4
        EXDATE;TZID=Australia/Sydney:20261120T190000
        SUMMARY:Youth night
        LOCATION:Church hall
        BEGIN:VALARM
        ACTION:DISPLAY
        DESCRIPTION:Reminder
        TRIGGER:-PT15M
        END:VALARM
        END:VEVENT
        BEGIN:VEVENT
        UID:youth-night
        RECURRENCE-ID;TZID=Australia/Sydney:20261127T190000
        DTSTART;TZID=Australia/Sydney:20261127T200000
        DTEND;TZID=Australia/Sydney:20261127T220000
        SUMMARY:Youth night: games
        END:VEVENT
        END:VCALENDAR
        """;

    [Fact]
    public async Task AnOutsideCalendarIsCopiedWithItsRepeatsSkippedDatesAndChangedDatesInChurchTime()
    {
        CalendarSettings youth = await CopiedCalendarAsync();
        ICalSource.Calendar = YouthCalendar;

        await CopyAsync();

        List<EventRecord> copied = await CopiedAsync(youth);
        Assert.Equal(
            [
                ("Youth night", new DateOnly(2026, 11, 13), (DateOnly?)null, (int?)(18 * 60), (int?)(20 * 60), "Church hall"),
                ("Youth night: games", new DateOnly(2026, 11, 27), null, 19 * 60, 21 * 60, ""),
                ("School camp, Year 6", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), null, null, ""),
                ("Youth night", new DateOnly(2026, 12, 4), null, 18 * 60, 20 * 60, "Church hall"),
            ],
            copied.OrderBy(record => record.StartDate)
                .Select(record => (record.Fields.Title, record.StartDate, record.EndDate, record.StartMinute, record.EndMinute, record.Fields.Place?.Address ?? ""))
                .ToList());
        Assert.All(copied, record => Assert.Null(record.Fields.Description));
    }

    [Fact]
    public async Task AnUnchangedCalendarIsAskedForWithItsETagAndAFailureKeepsWhatWasCopied()
    {
        CalendarSettings youth = await CopiedCalendarAsync();
        ICalSource.Calendar = YouthCalendar;
        await CopyAsync();

        await CopyAsync();
        Assert.Equal("\"1\"", Assert.Single(ICalSource.Requests[^1].Headers.IfNoneMatch).Tag);
        Assert.Equal(4, (await CopiedAsync(youth)).Count);

        ICalSource.ETag = "\"2\"";
        ICalSource.Calendar = YouthCalendar.Replace("UID:school-camp", "UID:school-camp-moved", StringComparison.Ordinal).Replace("20261201", "20261202", StringComparison.Ordinal);
        await CopyAsync();
        Assert.Equal([new DateOnly(2026, 12, 2)], (await CopiedAsync(youth)).Where(record => record.StartMinute is null).Select(record => record.StartDate));

        ICalSource.Failing = HttpStatusCode.InternalServerError;
        await CopyAsync();
        CalendarRecord calendar = await WithAsync(provider => provider.GetRequiredService<EventsDbContext>().Calendars.SingleAsync(found => found.Name == "Youth", Cancel));
        Assert.Equal("The calendar's address answered 500.", calendar.ICalProblem);
        Assert.Equal(Clock.UtcNow, calendar.ICalReadAt);
        Assert.Equal(4, (await CopiedAsync(youth)).Count);
    }

    private Task<CalendarSettings> CopiedCalendarAsync() =>
        WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>().SaveCalendarAsync(new CalendarSettings
        {
            Calendar = new CalendarInfo { Name = "Youth", Audience = Audience.Everyone.Text },
            ICalSource = "webcal://calendar.example.org/youth.ics",
        }, As(Person())));

    private Task CopyAsync() => WithAsync(provider => provider.GetRequiredService<ICalCopier>().CopyAllAsync(Cancel));

    private Task<List<EventRecord>> CopiedAsync(CalendarSettings calendar) =>
        WithAsync(provider => provider.GetRequiredService<EventsDbContext>().Events.AsNoTracking()
            .Where(record => record.CalendarId == Guid.Parse(calendar.Calendar.Id)).ToListAsync(Cancel));
}
