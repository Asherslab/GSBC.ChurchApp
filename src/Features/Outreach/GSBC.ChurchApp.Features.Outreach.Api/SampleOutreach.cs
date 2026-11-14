using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class SampleOutreach(IServiceScopeFactory scopes, IHostEnvironment environment, TimeProvider time, ILogger<SampleOutreach> logger)
    : BackgroundService
{
    private static readonly Guid Carols = new("0f7a3c1e-55b2-4d0a-9c61-2b8e4f6a7d10");
    private static readonly (int DaysAgo, int Metres, int Letterboxes, bool Carols)[] Walks = [(1, 3100, 104, false), (6, 2400, 81, false), (12, 4200, 150, false), (3, 1800, 60, true)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
            return;
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            OutreachDbContext db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
            if (await db.Walks.AnyAsync(stoppingToken) || await db.Campaigns.CountAsync(stoppingToken) > 1)
                return;
            OutreachPublisher publisher = scope.ServiceProvider.GetRequiredService<OutreachPublisher>();
            DateTime now = time.GetUtcNow().UtcDateTime;
            CampaignRecord carols = new()
            {
                Id = Carols, Name = "Carols sample", HandedOut = "Invitations to carols", StartsOn = DateOnly.FromDateTime(now.AddDays(-7)),
                EndsOn = DateOnly.FromDateTime(now.AddDays(30)), Leaderboard = true, CreatedAt = now, ChangedAt = now,
            };
            db.Campaigns.Add(carols);
            publisher.Campaign(carols, now);
            foreach ((int daysAgo, int metres, int letterboxes, bool forCarols) in Walks)
            {
                DateTime started = now.AddDays(-daysAgo);
                db.Walks.Add(new WalkRecord
                {
                    Id = Guid.NewGuid(), Campaigns = [forCarols ? Carols : OutreachCampaigns.GeneralId], StartedAt = started, LastAt = started.AddHours(1),
                    Ended = true, Metres = metres, Letterboxes = letterboxes, Conversations = 1,
                });
            }
            FollowUpRecord followUp = new()
            {
                Id = Guid.NewGuid(), CampaignId = OutreachCampaigns.GeneralId, Street = "sample", Label = "Sample St, this side", FirstName = "Sam",
                Contact = "0400 000 000", Note = "Would like to hear about Sunday services.", At = now.AddDays(-1),
            };
            db.FollowUps.Add(followUp);
            await publisher.FollowUpAsync(followUp, now, stoppingToken);
            db.Reports.Add(new ReportRecord
            {
                Id = Guid.NewGuid(), Reason = ReportReason.NotAHome, Street = "sample", Label = "Sample Rd, the end of the street",
                Note = "This is a car yard, not houses.", At = now.AddDays(-2),
            });
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Adding the sample outreach failed");
        }
    }
}
