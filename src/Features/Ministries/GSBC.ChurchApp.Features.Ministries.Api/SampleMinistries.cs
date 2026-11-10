using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class SampleMinistries(IServiceScopeFactory scopes, IHostEnvironment environment, TimeProvider time, ILogger<SampleMinistries> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
            return;
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
            if (await db.Ministries.AnyAsync(stoppingToken))
                return;
            SampleSet samples = Fill(db, ChurchTime.Today(time), time.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(stoppingToken);
            IMinistryCalendars calendars = scope.ServiceProvider.GetRequiredService<IMinistryCalendars>();
            foreach ((Guid ministry, string name, string audience) in samples.Calendars)
                await calendars.CreateAsync(ministry, name, MinistryViews.CalendarAudience(audience, ministry), stoppingToken);
            await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Adding the sample ministries failed");
        }
    }

    private static SampleSet Fill(MinistriesDbContext db, DateOnly today, DateTime now)
    {
        int order = 0;
        List<(Guid, string, string)> calendars = [];
        MinistryRecord Add(Guid? parent, string name, string summary, string about, string meets, string whoFor, string colour, string icon,
            string teamJoin, string attendJoin, bool inLifeGroups = false)
        {
            MinistryRecord ministry = new()
            {
                Id = Guid.CreateVersion7(), ParentId = parent, ShowsInLifeGroups = inLifeGroups, Name = name, Summary = summary, About = about, Meets = meets,
                WhoFor = whoFor, Colour = colour, Icon = icon, TeamJoin = teamJoin, AttendJoin = attendJoin, Order = order++, CreatedAt = now, UpdatedAt = now,
            };
            db.Ministries.Add(ministry);
            db.FeedChanges.Add(new() { Feed = MinistriesFeeds.Ministries, RecordId = ministry.Id.ToString(), Version = now.Ticks });
            return ministry;
        }
        void Post(MinistryRecord ministry, string title, string body, string audience, int daysAgo) =>
            db.Posts.Add(new PostRecord
            {
                Id = Guid.CreateVersion7(), MinistryId = ministry.Id, Title = title, Body = body, Audience = audience, AuthorName = "Sample leader",
                PostedAt = now.AddDays(-daysAgo),
            });
        void Link(MinistryRecord ministry, string title, string url, string audience) =>
            db.Files.Add(new FileRecord { Id = Guid.CreateVersion7(), MinistryId = ministry.Id, Title = title, Kind = FileKinds.Link, Url = url, Audience = audience, AddedAt = now });

        MinistryRecord music = Add(null, "Music", "Leads the church in song at the morning and evening services",
            "Sample description. Music leads the congregation in song and looks after the ministries under it.",
            "Sundays 10:30am and 6pm; rehearsals Thursdays 7:30pm", "Singers, musicians and sound", "#2a3d5e", "music", Joining.Ask, Joining.None);
        Add(music.Id, "Choir", "Sings at the morning service and at Christmas", "Sample description.", "Thursdays 7:30pm", "", "#2a3d5e", "music", Joining.Ask, Joining.None);
        Add(music.Id, "Vocal Groups", "Small groups who sing a special item", "Sample description.", "", "", "#2a3d5e", "music", Joining.Ask, Joining.None);
        Add(music.Id, "Song Leading", "Leads the congregational songs", "Sample description. Leaders invite new song leaders.", "", "", "#2a3d5e", "music", Joining.Invite, Joining.None);
        Add(music.Id, "Production", "Sound, slides and the livestream", "Sample description.", "", "", "#2a3d5e", "music", Joining.Ask, Joining.None);
        Post(music, "Songs for Sunday are up", "Sample post. Practice recordings are in Files.", Audiences.Team, 3);
        Link(music, "Practice recordings", "https://www.dropbox.com/", Audiences.Team);
        calendars.Add((music.Id, "Music team", Audiences.Team));

        MinistryRecord kids = Add(null, "Impact Kids", "Friday nights for ages 4 to 11",
            "Sample description. On Friday nights in school term, Impact Kids has games, a Bible lesson and craft.",
            "Fridays 6:30 to 8:30pm in school terms", "Ages 4 to 11", "#f89833", "smile", Joining.Ask, Joining.Ask);
        Post(kids, "This Friday: space night", "Sample post. Dress up if you like; doors open 6:15pm.", Audiences.Everyone, 4);
        Post(kids, "Pick up by 8:30 this Friday", "Sample post. We finish on time for the break-up night.", Audiences.Attendees, 1);
        Post(kids, "Permission forms for the zoo trip", "Sample post. Due back by the end of the month.", Audiences.Parents, 3);
        Post(kids, "Helpers: 6pm set-up this week", "Sample post. Space night needs the hall dark.", Audiences.Team, 0);
        Link(kids, "Helpers' guide", "https://example.com/helpers-guide", Audiences.Team);
        calendars.Add((kids.Id, "Impact Kids", Audiences.Everyone));
        calendars.Add((kids.Id, "Impact Kids team", Audiences.Team));

        MinistryRecord youth = Add(null, "Impact Youth", "Friday nights for high schoolers",
            "Impact Youth is for all high school aged youth and is a great way to get to know other teens, discover what a genuine faith is all about, and an opportunity to have fun at the same time! We meet every Friday night during school terms from 6:30-8:30pm.",
            "Fridays 6:30 to 8:30pm in school terms", "Years 7 to 12", "#6bcbd5", "zap", Joining.Ask, Joining.Ask);
        Post(youth, "Youth camp info night", "Sample post. Parents and youth: information about January's camp after youth.", Audiences.Attendees, 2);
        calendars.Add((youth.Id, "Impact Youth", Audiences.Everyone));

        MinistryRecord lifeGroups = Add(null, "Life Groups", "Sunday classes at 9:00", "Sample description. Classes on Sunday from 9:00 to 9:45, before the 10:30 service.",
            "Sundays 9:00 to 9:45", "Everyone", "#009c8f", "users", Joining.Ask, Joining.Open);
        DateOnly sunday = today.AddDays((7 - (int)today.DayOfWeek) % 7);
        RoundRecord round = new()
        {
            Id = Guid.CreateVersion7(), Name = "Sample round", Sundays = [.. Enumerable.Range(0, 4).Select(week => sunday.AddDays(7 * week))],
            Starts = new TimeOnly(9, 0), Ends = new TimeOnly(9, 45), ShowsFrom = today,
        };
        db.Rounds.Add(round);
        MinistryRecord first = Add(lifeGroups.Id, "Psalms of Ascent", "Four weeks in the songs pilgrims sang", "Sample group.", "", "Adults", "#009c8f", "book-open",
            Joining.Invite, Joining.Open, inLifeGroups: true);
        (first.RoundId, first.Meets) = (round.Id, "Library (5)");
        MinistryRecord second = Add(lifeGroups.Id, "Faith at Work", "Living it out Monday to Friday", "Sample group.", "", "Adults", "#e7ae74", "book-open",
            Joining.Invite, Joining.Open, inLifeGroups: true);
        (second.RoundId, second.Meets) = (round.Id, "Room 2");
        MinistryRecord kidsLife = Add(kids.Id, "Kids Life", "Prep to Year 6, all year", "Sample group.", "", "Prep to Year 6", "#f89833", "smile",
            Joining.Invite, Joining.Open, inLifeGroups: true);
        kidsLife.Meets = "Hall";
        db.Sessions.Add(new SessionRecord { GroupId = second.Id, Date = round.Sundays[1], Off = true, Note = "No class this week" });
        db.Sessions.Add(new SessionRecord { GroupId = first.Id, Date = round.Sundays[2], Note = "Combined with Faith at Work, in Room 2" });
        return new SampleSet(calendars);
    }

    private sealed record SampleSet(IReadOnlyList<(Guid Ministry, string Name, string Audience)> Calendars);
}
