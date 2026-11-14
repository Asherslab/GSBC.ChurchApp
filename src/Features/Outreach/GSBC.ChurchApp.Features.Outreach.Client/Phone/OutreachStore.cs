using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record OutreachNow(
    OutreachSettings Settings,
    IReadOnlyList<Campaign> Campaigns,
    IReadOnlyList<OutreachDay> Days,
    IReadOnlyList<DayRoute> Routes,
    IReadOnlyList<Claim> Claims,
    IReadOnlyList<Skip> Skips,
    IReadOnlyList<Exclusion> Exclusions,
    IReadOnlyList<StreetCoverage> Coverage,
    IReadOnlyList<OutreachRegion> Regions,
    IReadOnlyList<YourWalk> Mine,
    bool CountedByName)
{
    public static OutreachNow Empty { get; } = new(OutreachDefaults.Settings(), [], [], [], [], [], [], [], [], [], true);

    public Campaign? CampaignOf(string id) => Campaigns.FirstOrDefault(campaign => campaign.Id == id);

    public IEnumerable<DayRoute> RoutesOf(string dayId) => Routes.Where(route => route.DayId == dayId).OrderBy(route => route.Number);

    public IEnumerable<Claim> ClaimsOf(string routeId) => Claims.Where(claim => claim.RouteId == routeId).OrderBy(claim => claim.At);
}

public sealed class OutreachStore(ILocalFeeds feeds, IAccess access)
{
    public static readonly IReadOnlyList<string> Feeds =
    [
        OutreachFeeds.Settings, OutreachFeeds.Campaigns, OutreachFeeds.Coverage, OutreachFeeds.Days, OutreachFeeds.Routes, OutreachFeeds.Claims,
        OutreachFeeds.Skips, OutreachFeeds.Exclusions, OutreachFeeds.Mine, OutreachMapsFeeds.Regions,
    ];

    private const int Page = 500;
    private const string WalkPrefix = "walk/";

    public async Task<OutreachNow> LoadAsync()
    {
        if (!feeds.IsAvailable)
            return OutreachNow.Empty;
        List<OutreachSettings> settings = await ReadAllAsync<OutreachSettings>(OutreachFeeds.Settings, OutreachContractsModel.Instance);
        List<YourWalk> mine = [];
        bool byName = true;
        if (access.SignedIn)
        {
            foreach (StoredRecord record in await ReadRecordsAsync(OutreachFeeds.Mine))
            {
                using MemoryStream stream = new(record.Payload);
                if (record.Id.StartsWith(WalkPrefix, StringComparison.Ordinal))
                    mine.Add(OutreachContractsModel.Instance.Deserialize<YourWalk>(stream));
                else
                    byName = OutreachContractsModel.Instance.Deserialize<YourCounting>(stream).ByName;
            }
        }
        return new OutreachNow(
            settings.FirstOrDefault() ?? OutreachDefaults.Settings(),
            await ReadAllAsync<Campaign>(OutreachFeeds.Campaigns, OutreachContractsModel.Instance),
            [.. (await ReadAllAsync<OutreachDay>(OutreachFeeds.Days, OutreachContractsModel.Instance)).OrderBy(day => day.StartsAt)],
            await ReadAllAsync<DayRoute>(OutreachFeeds.Routes, OutreachContractsModel.Instance),
            await ReadAllAsync<Claim>(OutreachFeeds.Claims, OutreachContractsModel.Instance),
            await ReadAllAsync<Skip>(OutreachFeeds.Skips, OutreachContractsModel.Instance),
            await ReadAllAsync<Exclusion>(OutreachFeeds.Exclusions, OutreachContractsModel.Instance),
            await ReadAllAsync<StreetCoverage>(OutreachFeeds.Coverage, OutreachContractsModel.Instance),
            await ReadAllAsync<OutreachRegion>(OutreachMapsFeeds.Regions, OutreachMapsContractsModel.Instance),
            [.. mine.OrderByDescending(walk => walk.StartedAt)],
            byName);
    }

    private async Task<List<T>> ReadAllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string feed, TypeModel model)
    {
        List<T> all = [];
        foreach (StoredRecord record in await ReadRecordsAsync(feed))
        {
            using MemoryStream stream = new(record.Payload);
            all.Add(model.Deserialize<T>(stream));
        }
        return all;
    }

    private async Task<List<StoredRecord>> ReadRecordsAsync(string feed)
    {
        List<StoredRecord> all = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(feed, Limit: Page, Offset: offset));
            all.AddRange(page);
            if (page.Count < Page)
                return all;
        }
    }
}
