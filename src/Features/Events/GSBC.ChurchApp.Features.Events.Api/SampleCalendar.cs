using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class SampleCalendar(IServiceScopeFactory scopes, IHostEnvironment environment, TimeProvider time, ILogger<SampleCalendar> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
            return;
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            if (await db.Calendars.AnyAsync(stoppingToken))
                return;
            Fill(db, ChurchTime.Today(time), time.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(stoppingToken);
            await scope.ServiceProvider.GetRequiredService<EventsPublisher>().PublishAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Adding the sample calendar failed");
        }
    }

    public static void Fill(EventsDbContext db, DateOnly today, DateTime now)
    {
        DateOnly sunday = today.AddDays(-(int)today.DayOfWeek);
        int order = 0;
        CalendarRecord Calendar(string name, string description, Audience audience, bool onNewPhones)
        {
            CalendarRecord calendar = new()
            {
                Id = Guid.CreateVersion7(), Name = name, Slug = CalendarLinks.SlugOf(name), Description = description, Audience = audience.Text,
                ShownOnNewPhones = onNewPhones, Order = order++, CreatedAt = now, UpdatedAt = now,
            };
            db.Calendars.Add(calendar);
            return calendar;
        }

        RegularEventRecord Regular(CalendarRecord calendar, string title, RegularPattern pattern, int start, int? end, EventFields? fields = null)
        {
            RegularEventRecord regular = new()
            {
                Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = fields ?? new EventFields { Title = title }, Pattern = pattern,
                StartMinute = start, EndMinute = end, Showing = Showing.Shown, CreatedAt = now, UpdatedAt = now,
            };
            regular.Fields.Title = title;
            db.RegularEvents.Add(regular);
            return regular;
        }

        void OneOff(CalendarRecord calendar, DateOnly date, int? start, int? end, EventFields fields) =>
            db.Events.Add(new EventRecord
            {
                Id = Guid.CreateVersion7(), CalendarId = calendar.Id, Fields = fields, StartDate = date, StartMinute = start, EndMinute = end,
                Showing = Showing.Shown, CreatedAt = now, UpdatedAt = now,
            });

        RegularPattern Weekly(DayOfWeek day) => new() { Repeats = Repeats.Weekly, Days = [day], StartDate = EventDates.Format(sunday.AddDays(-56)) };
        EventPlace auditorium = new() { Name = "Auditorium" };

        CalendarRecord services = Calendar("Sunday services", "Every Sunday's services", Audience.Everyone, true);
        CalendarRecord wholeChurch = Calendar("Whole church", "Specials, meetings and days for everyone", Audience.Everyone, true);
        CalendarRecord kids = Calendar("Kids", "Fridays in term, camps", Audience.Everyone, false);
        CalendarRecord kidsTeam = Calendar("Kids team", "For the Kids ministry's helpers", Audience.SignedIn, false);
        CalendarRecord outreach = Calendar("Outreach", "Saturday mornings and outreach days", Audience.Everyone, false);
        CalendarRecord members = Calendar("Church members", "Members' meetings, such as the AGM", Audience.SignedIn, false);

        Regular(services, "LIFE Groups", Weekly(DayOfWeek.Sunday), 9 * 60, 10 * 60);
        RegularEventRecord morning = Regular(services, "Morning service", Weekly(DayOfWeek.Sunday), 10 * 60 + 30, 12 * 60, new EventFields { Place = auditorium });
        RegularEventRecord evening = Regular(services, "Evening service", Weekly(DayOfWeek.Sunday), 18 * 60, 19 * 60 + 30, new EventFields { Place = auditorium });

        BreakRecord christmas = new() { Id = Guid.CreateVersion7(), Name = "Christmas break", StartDate = new DateOnly(sunday.Year, 12, 21), EndDate = new DateOnly(sunday.Year + 1, 1, 25), CreatedAt = now };
        db.Breaks.Add(christmas);
        db.RegularEventBreaks.Add(new RegularEventBreak { RegularEventId = evening.Id, BreakId = christmas.Id });

        db.Specials.Add(new SpecialRecord
        {
            RegularEventId = morning.Id, Date = sunday.AddDays(7), Changed = [EventFieldKeys.Title, EventFieldKeys.Label, EventFieldKeys.Description],
            Fields = new EventFields
            {
                Title = "Anniversary Sunday Service", Label = "Anniversary",
                Description = new RichText { Markdown = "One service for the whole church, then lunch together after. Bring a plate to share." },
            },
            UpdatedAt = now,
        });

        Regular(wholeChurch, "Bible study and prayer", Weekly(DayOfWeek.Wednesday), 19 * 60, 20 * 60 + 30, new EventFields { Place = new EventPlace { Name = "Fellowship Area" } });
        Regular(wholeChurch, "Men's prayer", Weekly(DayOfWeek.Saturday), 6 * 60, 7 * 60);
        OneOff(wholeChurch, new DateOnly(sunday.Year, 12, 20), 18 * 60 + 30, 20 * 60, new EventFields
        {
            Title = "Carols by candlelight", Label = "Christmas", Place = new EventPlace { Name = "School oval", Address = "School oval, behind the church" },
            Description = new RichText { Markdown = "Carols on the oval as the sun goes down. Bring a picnic rug and a plate." },
        });

        RegularEventRecord impactKids = Regular(kids, "Impact Kids", Weekly(DayOfWeek.Friday), 18 * 60 + 30, 20 * 60, new EventFields { Place = auditorium });
        db.Breaks.Add(new BreakRecord { Id = Guid.CreateVersion7(), Name = "Kids camp weekend", StartDate = sunday.AddDays(5), EndDate = sunday.AddDays(6), RegularEventId = impactKids.Id, CreatedAt = now });
        OneOff(kids, sunday.AddDays(5), null, null, new EventFields { Title = "Kids camp", Label = "Camp", Place = new EventPlace { Name = "Lakeside camp", Address = "Lakeside Road, near the boat ramp" } });
        OneOff(kids, new DateOnly(sunday.Year, 12, 11), 18 * 60 + 30, 21 * 60, new EventFields { Title = "Break-up night", Place = auditorium });
        OneOff(kidsTeam, sunday.AddDays(3), 19 * 60 + 30, 21 * 60, new EventFields
        {
            Title = "Children's Ministry helpers", Label = "Kids team", Place = new EventPlace { Name = "Fellowship Area" },
            Contacts = [new EventContact { Name = "Robin Ashdown", Phone = "0400 000 001", Email = "kids@example.org" }],
        });

        RegularEventRecord saturdays = Regular(outreach, "Outreach Saturday", new RegularPattern
        {
            Repeats = Repeats.EverySecondWeek, Days = [DayOfWeek.Saturday], StartDate = EventDates.Format(sunday.AddDays(6)),
        }, 8 * 60 + 30, 11 * 60 + 30, new EventFields
        {
            Label = "Outreach",
            Contacts = [new EventContact { Name = "Sam Okafor", Phone = "0400 000 002" }],
        });
        saturdays.Needs = [EventFieldKeys.Place];
        saturdays.RemindDaysBefore = [7, 2];
        db.Specials.Add(new SpecialRecord
        {
            RegularEventId = saturdays.Id, Date = sunday.AddDays(6), Changed = [EventFieldKeys.Place],
            Fields = new EventFields { Place = new EventPlace { Name = "Teralba Park", Address = "Teralba Park, Teralba" } },
            ChangeNote = "Now at Teralba Park", ChangedAt = now, UpdatedAt = now,
        });

        OneOff(members, sunday.AddDays(4), 19 * 60 + 30, 21 * 60, new EventFields
        {
            Title = "Annual general meeting", Label = "Church members", Place = new EventPlace { Name = "Fellowship Area" },
        });
    }
}
