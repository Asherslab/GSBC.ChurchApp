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
            OutreachPublisher publisher = scope.ServiceProvider.GetRequiredService<OutreachPublisher>();
            await AddHistoryAsync(db, publisher, stoppingToken);
            if (await db.Walks.AnyAsync(stoppingToken) || await db.Campaigns.CountAsync(stoppingToken) > 1)
                return;
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

    private async Task AddHistoryAsync(OutreachDbContext db, OutreachPublisher publisher, CancellationToken cancellationToken)
    {
        if (await db.Days.AnyAsync(cancellationToken) || await db.Skips.AnyAsync(cancellationToken) || await db.ExclusionZones.AnyAsync(cancellationToken))
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        Random random = new(2026);
        DayRecord day = new()
        {
            Id = Guid.NewGuid(), CampaignId = OutreachCampaigns.GeneralId, StartsAt = now.AddDays(-40).Date.AddHours(22.5), EndsAt = now.AddDays(-40).Date.AddHours(25.5),
            MeetingLat = -27.3466, MeetingLng = 152.9667, MeetingPlace = "The church car park", Mode = WalkMode.Letterbox, Published = true, ChangedAt = now,
        };
        db.Days.Add(day);
        for (int week = 1; week <= 56; week++)
        {
            for (int walk = random.Next(0, 4); walk > 0; walk--)
            {
                DateTime started = now.AddDays(-7 * week + random.Next(0, 7)).Date.AddHours(random.Next(21, 30));
                int metres = random.Next(1500, 5200);
                db.Walks.Add(new WalkRecord
                {
                    Id = Guid.NewGuid(), Campaigns = [OutreachCampaigns.GeneralId], StartedAt = started, LastAt = started.AddMinutes(metres / 50.0), Ended = true,
                    Metres = metres, Letterboxes = metres * random.Next(30, 40) / 1000, Conversations = random.Next(0, 3),
                });
            }
        }
        for (int walker = 0; walker < 14; walker++)
        {
            int metres = random.Next(2200, 3600);
            db.Walks.Add(new WalkRecord
            {
                Id = Guid.NewGuid(), Campaigns = [OutreachCampaigns.GeneralId], DayId = day.Id, StartedAt = day.StartsAt.AddMinutes(walker), LastAt = day.EndsAt,
                Ended = true, Metres = metres, Letterboxes = metres * 36 / 1000, Conversations = random.Next(0, 2),
            });
        }
        (string Label, SkipKind Kind)[] marks = [("Sample St, this side", SkipKind.NoJunkMail), ("Sample Ave, the far end", SkipKind.NoJunkMail), ("Sample Cres, by the park", SkipKind.Hostile)];
        foreach ((string label, SkipKind kind) in marks)
        {
            SkipRecord skip = new()
            {
                Id = Guid.NewGuid(), Kind = kind, Street = "sample", Label = label, Lat = -27.35, Lng = 152.97, At = now.AddDays(-random.Next(3, 90)),
            };
            skip.Until = kind == SkipKind.Hostile ? null : skip.At.AddDays(OutreachDefaults.Settings().NoJunkMailKeptDays);
            db.Skips.Add(skip);
            publisher.Skip(skip, now);
        }
        ExclusionZoneRecord zone = new()
        {
            Id = Guid.NewGuid(), Name = "Sample retirement village", AreaLats = [-27.351, -27.351, -27.354, -27.354], AreaLngs = [152.968, 152.972, 152.972, 152.968],
            Reason = ExclusionReason.PrivateEstate, Note = "The manager asked us not to deliver inside the gates.", ChangedAt = now,
        };
        db.ExclusionZones.Add(zone);
        publisher.Zone(zone, now);
        ExclusionAddressRecord address = new()
        {
            Id = Guid.NewGuid(), Street = "sample", Label = "Sample Rd, this side", Lat = -27.348, Lng = 152.965, Reason = ExclusionReason.Complaint,
            Note = "Phoned the office.", Caller = "Sample caller, 0400 000 000", ChangedAt = now,
        };
        db.ExclusionAddresses.Add(address);
        publisher.Address(address, now);
        await db.SaveChangesAsync(cancellationToken);
    }
}
