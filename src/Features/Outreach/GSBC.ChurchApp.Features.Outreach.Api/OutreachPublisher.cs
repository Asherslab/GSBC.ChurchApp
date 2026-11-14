using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachPublisher(OutreachDbContext db, OutreachMinistries ministries)
{
    public const string SettingsId = "settings";
    public static readonly TimeSpan DaysAhead = TimeSpan.FromDays(28);
    public static readonly TimeSpan DaysKeptAfter = TimeSpan.FromDays(1);

    public void Settings(OutreachSettings settings, DateTime now) =>
        db.FeedChanges.Add(Row(OutreachFeeds.Settings, SettingsId, now, settings));

    public void Campaign(CampaignRecord campaign, DateTime now) =>
        db.FeedChanges.Add(Row(OutreachFeeds.Campaigns, campaign.Id.ToString(), now, OutreachViews.Campaign(campaign)));

    public void CampaignGone(Guid campaign, DateTime now)
    {
        db.FeedChanges.Add(Gone(OutreachFeeds.Campaigns, campaign.ToString(), now));
        db.FeedChanges.Add(Gone(OutreachFeeds.Leaderboards, campaign.ToString(), now));
    }

    public async Task StreetsAsync(Guid campaign, IEnumerable<string> streets, OutreachSettings settings, DateTime now, CancellationToken cancellationToken)
    {
        List<string> named = streets.Distinct().ToList();
        if (named.Count == 0)
            return;
        DateTime restStarts = CoverageRules.RestStarts(now, settings.RestDays);
        List<CoverageRecord> rows = await db.Coverage.AsNoTracking()
            .Where(coverage => coverage.CampaignId == campaign && named.Contains(coverage.Street) && coverage.ExpiredAt == null && coverage.At > restStarts)
            .ToListAsync(cancellationToken);
        rows.AddRange(db.Coverage.Local.Where(coverage => coverage.Id == 0 && coverage.CampaignId == campaign && named.Contains(coverage.Street)
                                                          && CoverageRules.Counts(coverage, restStarts)));
        foreach (string street in named)
        {
            List<CoverageRecord> onStreet = rows.Where(coverage => coverage.Street == street).ToList();
            string id = CoverageId(campaign, street);
            if (onStreet.Count == 0)
            {
                db.FeedChanges.Add(Gone(OutreachFeeds.Coverage, id, now));
                continue;
            }
            DateTime newest = onStreet.Max(coverage => coverage.At);
            FeedChangeRecord row = Row(OutreachFeeds.Coverage, id, now, new StreetCoverage
            {
                Campaign = campaign.ToString(), Region = onStreet.MaxBy(coverage => coverage.At)!.Region, Street = street,
                Stretches = CoverageRules.Merge(onStreet),
            });
            row.GroupKey = campaign.ToString();
            row.StartsAt = newest;
            row.EndsAt = onStreet.Min(coverage => coverage.At) + TimeSpan.FromDays(settings.RestDays);
            db.FeedChanges.Add(row);
        }
    }

    public void CoverageGone(Guid campaign, string street, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Coverage, CoverageId(campaign, street), now));

    private static string CoverageId(Guid campaign, string street) => $"{campaign:N}/{street}";

    public void Day(DayRecord day, IReadOnlyList<RouteRecord> routes, DateTime now)
    {
        bool shown = day.Published && day.EndsAt + DaysKeptAfter > now && day.StartsAt - DaysAhead < now;
        if (shown)
        {
            FeedChangeRecord row = Row(OutreachFeeds.Days, day.Id.ToString(), now, OutreachViews.Day(day));
            row.StartsAt = day.StartsAt;
            row.EndsAt = day.EndsAt;
            row.SortKey = day.StartsAt.ToString("O");
            db.FeedChanges.Add(row);
            foreach (RouteRecord route in routes)
            {
                FeedChangeRecord routeRow = new()
                {
                    Feed = OutreachFeeds.Routes, RecordId = route.Id.ToString(), Version = now.Ticks, Payload = route.Payload,
                    GroupKey = day.Id.ToString(), SortKey = route.Number.ToString("D4", System.Globalization.CultureInfo.InvariantCulture),
                };
                db.FeedChanges.Add(routeRow);
            }
        }
        else if (day.OnPhones)
            DayGone(day.Id, routes.Select(route => route.Id), now);
        day.OnPhones = shown;
    }

    public void DayGone(Guid day, IEnumerable<Guid> routes, DateTime now)
    {
        db.FeedChanges.Add(Gone(OutreachFeeds.Days, day.ToString(), now));
        foreach (Guid route in routes)
            db.FeedChanges.Add(Gone(OutreachFeeds.Routes, route.ToString(), now));
    }

    public void RouteGone(Guid route, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Routes, route.ToString(), now));

    public void Claim(ClaimRecord claim, DateTime now)
    {
        FeedChangeRecord row = Row(OutreachFeeds.Claims, claim.Id.ToString(), now, new Claim
        {
            Id = claim.Id.ToString(), RouteId = claim.RouteId.ToString(), DayId = claim.DayId.ToString(), FirstName = claim.FirstName, At = claim.At,
        });
        row.GroupKey = claim.DayId.ToString();
        db.FeedChanges.Add(row);
    }

    public void ClaimGone(Guid claim, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Claims, claim.ToString(), now));

    public void Skip(SkipRecord skip, DateTime now)
    {
        FeedChangeRecord row = Row(OutreachFeeds.Skips, skip.Id.ToString(), now, OutreachViews.Skip(skip));
        row.EndsAt = skip.Until;
        db.FeedChanges.Add(row);
    }

    public void SkipGone(Guid skip, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Skips, skip.ToString(), now));

    public void Zone(ExclusionZoneRecord zone, DateTime now) =>
        db.FeedChanges.Add(Row(OutreachFeeds.Exclusions, ZoneId(zone.Id), now, new Exclusion { Id = zone.Id.ToString(), Area = OutreachViews.Polygon(zone.AreaLats, zone.AreaLngs) }));

    public void Address(ExclusionAddressRecord address, DateTime now) =>
        db.FeedChanges.Add(Row(OutreachFeeds.Exclusions, AddressId(address.Id), now, new Exclusion
        {
            Id = address.Id.ToString(), Address = OutreachViews.Address(address.AddressKey, address.Region, address.Street, address.Side, address.Along,
                address.Lat, address.Lng, address.ShowMarker ? address.Label : ""),
            ShowMarker = address.ShowMarker,
        }));

    public void ZoneGone(Guid zone, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Exclusions, ZoneId(zone), now));

    public void AddressGone(Guid address, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Exclusions, AddressId(address), now));

    public void Leaderboard(Leaderboard board, DateTime now) => db.FeedChanges.Add(Row(OutreachFeeds.Leaderboards, board.Campaign, now, board));

    public void LeaderboardGone(Guid campaign, DateTime now) => db.FeedChanges.Add(Gone(OutreachFeeds.Leaderboards, campaign.ToString(), now));

    public void Walk(WalkRecord walk, bool counted, DateTime now)
    {
        if (walk.UserId is not { } owner)
            return;
        FeedChangeRecord row = Row(OutreachFeeds.Mine, $"walk/{walk.Id}", now, OutreachViews.Yours(walk, counted));
        row.ScopeId = owner.ToString();
        row.SortKey = walk.StartedAt.ToString("O");
        db.FeedChanges.Add(row);
    }

    public void WalkGone(Guid walk, Guid owner, DateTime now) =>
        db.FeedChanges.Add(new FeedChangeRecord
            { Feed = OutreachFeeds.Mine, RecordId = $"walk/{walk}", ScopeId = owner.ToString(), Version = now.Ticks, Deleted = true });

    public void Counting(Guid owner, bool byName, DateTime now)
    {
        FeedChangeRecord row = Row(OutreachFeeds.Mine, CountingId(owner), now, new YourCounting { ByName = byName });
        row.ScopeId = owner.ToString();
        db.FeedChanges.Add(row);
    }

    public void CountingGone(Guid owner, DateTime now) =>
        db.FeedChanges.Add(new FeedChangeRecord { Feed = OutreachFeeds.Mine, RecordId = CountingId(owner), ScopeId = owner.ToString(), Version = now.Ticks, Deleted = true });

    public async Task FollowUpAsync(FollowUpRecord followUp, DateTime now, CancellationToken cancellationToken)
    {
        foreach (string? scope in await ministries.ScopesAsync(cancellationToken))
        {
            string id = $"{scope ?? "church"}/{followUp.Id}";
            if (followUp.DoneAt is not null)
            {
                db.FeedChanges.Add(new FeedChangeRecord { Feed = OutreachFeeds.FollowUps, RecordId = id, ScopeId = scope, Version = now.Ticks, Deleted = true });
                continue;
            }
            FeedChangeRecord row = Row(OutreachFeeds.FollowUps, id, now, OutreachViews.FollowUp(followUp, ""));
            row.ScopeId = scope;
            row.SortKey = followUp.At.ToString("O");
            db.FeedChanges.Add(row);
        }
        await DeskAsync("follow-up", followUp.Id, now, cancellationToken);
    }

    public async Task FollowUpGoneAsync(Guid followUp, DateTime now, CancellationToken cancellationToken)
    {
        foreach (string? scope in await ministries.ScopesAsync(cancellationToken))
            db.FeedChanges.Add(new FeedChangeRecord
                { Feed = OutreachFeeds.FollowUps, RecordId = $"{scope ?? "church"}/{followUp}", ScopeId = scope, Version = now.Ticks, Deleted = true });
        await DeskAsync("follow-up", followUp, now, cancellationToken);
    }

    public async Task DeskAsync(string kind, Guid id, DateTime now, CancellationToken cancellationToken)
    {
        foreach (string? scope in await ministries.ScopesAsync(cancellationToken))
        {
            FeedChangeRecord marker = FeedChangeRecord.Marker(OutreachFeeds.Desk, $"{scope ?? "church"}/{kind}/{id}", now);
            marker.ScopeId = scope;
            db.FeedChanges.Add(marker);
        }
    }

    private static string ZoneId(Guid zone) => $"zone/{zone}";

    private static string AddressId(Guid address) => $"address/{address}";

    private static string CountingId(Guid owner) => $"counting/{owner}";

    private static FeedChangeRecord Row<T>(string feed, string id, DateTime now, T payload) =>
        new() { Feed = feed, RecordId = id, Version = now.Ticks, Payload = OutreachPayloads.Encode(payload) };

    private static FeedChangeRecord Gone(string feed, string id, DateTime now) => new() { Feed = feed, RecordId = id, Version = now.Ticks, Deleted = true };
}
