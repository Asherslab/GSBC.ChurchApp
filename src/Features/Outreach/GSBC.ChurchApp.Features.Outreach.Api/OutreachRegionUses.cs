using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachRegionUses(OutreachDbContext db, TimeProvider time) : IRegionUses
{
    public async Task<IReadOnlyList<RegionUse>> UsesAsync(IReadOnlyCollection<string> regionIds, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateOnly today = DateOnly.FromDateTime(now);
        List<CampaignRecord> campaigns = await db.Campaigns.AsNoTracking().ToListAsync(cancellationToken);
        Dictionary<Guid, string> names = campaigns.ToDictionary(campaign => campaign.Id, campaign => campaign.Name);
        List<RegionUse> uses = campaigns
            .Where(campaign => campaign.EndsOn is null || campaign.EndsOn >= today)
            .SelectMany(campaign => campaign.Regions.Where(regionIds.Contains).Select(region => new RegionUse
            {
                RegionId = region, Kind = RegionUseKinds.Campaign, Name = campaign.Name,
            }))
            .ToList();

        List<DayRecord> days = await db.Days.AsNoTracking().Where(day => day.EndsAt > now).OrderBy(day => day.StartsAt).ToListAsync(cancellationToken);
        List<Guid> dayIds = days.Select(day => day.Id).ToList();
        ILookup<Guid, RouteRecord> routes = (await db.Routes.AsNoTracking().Where(route => dayIds.Contains(route.DayId)).ToListAsync(cancellationToken))
            .ToLookup(route => route.DayId);
        foreach (DayRecord day in days)
            uses.AddRange(RegionsOf(day, routes[day.Id]).Where(regionIds.Contains).Select(region => new RegionUse
            {
                RegionId = region, Kind = RegionUseKinds.Day, Name = names.GetValueOrDefault(day.CampaignId, ""), On = day.StartsAt,
            }));
        return uses;
    }

    private static IEnumerable<string> RegionsOf(DayRecord day, IEnumerable<RouteRecord> routes)
    {
        HashSet<string> regions = [];
        if (day.Draft is { } draft && OutreachPayloads.Decode<DayDraft>(draft).Region is { Length: > 0 } drafted)
            regions.Add(drafted);
        foreach (RouteRecord route in routes)
            regions.UnionWith(OutreachPayloads.Decode<DayRoute>(route.Payload).Stretches.Select(stretch => stretch.Region));
        return regions;
    }
}
