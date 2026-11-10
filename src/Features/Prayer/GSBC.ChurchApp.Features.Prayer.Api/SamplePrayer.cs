using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class SamplePrayer(IServiceScopeFactory scopes, IHostEnvironment environment, TimeProvider time, ILogger<SamplePrayer> logger)
    : BackgroundService
{
    private static readonly (string Text, string Name, bool ShowName, int DaysAgo, int Prayed, WallState Wall, CareState Care, string Answered)[] Samples =
    [
        ("Please pray for my dad's surgery on Thursday, and for the surgeons.", "Sam Sample", true, 2, 14, WallState.Shown, CareState.FollowedUp, ""),
        ("For our family as we move house this month. It has been a hard year.", "", false, 4, 8, WallState.Shown, CareState.New, ""),
        ("For my sister's scan results next week.", "Jo Sample", true, 9, 31, WallState.Shown, CareState.Answered, "Thank you all. The scan came back clear!"),
        ("For wisdom at work, and for my team.", "", false, 10, 5, WallState.Shown, CareState.New, ""),
        ("For our exams next week, and for a friend whose family is going through a hard time.", "Riley Sample", false, 1, 0, WallState.Waiting, CareState.New, ""),
        ("For a decision about work. I'd rather this stayed with the pastoral team.", "", false, 3, 0, WallState.NotAsked, CareState.New, ""),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
            return;
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            PrayerDbContext db = scope.ServiceProvider.GetRequiredService<PrayerDbContext>();
            if (await db.Requests.AnyAsync(stoppingToken))
                return;
            PrayerPublisher publisher = scope.ServiceProvider.GetRequiredService<PrayerPublisher>();
            DateTime now = time.GetUtcNow().UtcDateTime;
            foreach ((string text, string name, bool showName, int daysAgo, int prayed, WallState wall, CareState care, string answered) in Samples)
            {
                DateTime sent = now.AddDays(-daysAgo);
                RequestRecord request = new()
                {
                    Id = Guid.NewGuid(), Text = text, SentAt = sent, ChangedAt = sent, Name = name, ShowName = showName,
                    Wall = wall, WallText = text, WallShowsName = showName, Prayed = prayed, Care = care, AnsweredNote = answered,
                    WallShownAt = wall == WallState.Shown ? sent.AddHours(2) : null,
                    WallUntil = wall == WallState.Shown ? sent.AddHours(2) + PrayerRules.OnTheWallFor : null,
                };
                request.Steps.Add(new StepRecord { At = sent, What = "Sent" });
                db.Requests.Add(request);
                publisher.Changed(request, now);
            }
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Adding the sample prayer requests failed");
        }
    }
}
