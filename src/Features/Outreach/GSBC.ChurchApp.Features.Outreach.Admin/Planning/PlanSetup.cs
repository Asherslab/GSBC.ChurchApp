using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GeoPoint = GSBC.ChurchApp.Features.Outreach.Contracts.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Planning;

public sealed record PlanSetup(
    string Scope,
    OutreachDay Day,
    string Campaign,
    string HandedOut,
    OutreachSettings Settings,
    int BundleSize,
    DayPlan Plan,
    IReadOnlyList<SavedArea> Areas,
    OutreachRegion? Region,
    OutreachAtlas Atlas,
    StreetGraph? Graph,
    CoverageMap Coverage,
    PlanningMarks Marks)
{
    public static async Task<PlanSetup?> LoadAsync(OutreachDeskClient outreach, OutreachMapsClient maps, StreetGraphLoader streets, string scope, string dayId)
    {
        OutreachDesk desk = await outreach.DeskAsync(scope);
        if (desk.Days.All(day => day.Id != dayId))
            return null;
        DayPlan plan = await outreach.PlanAsync(scope, dayId);
        OutreachDay day = plan.Day;
        IReadOnlyList<OutreachRegion> regions = (await maps.RegionsAsync()).Regions;
        OutreachRegion? region = RegionFor(regions, plan);
        Task<StreetGraph?> graph = region is null ? Task.FromResult<StreetGraph?>(null) : LoadAsync(streets, region);
        Task<CoverageMap> coverage = outreach.CoverageAsync(scope, day.Campaign);
        Task<PlanningMarks> marks = outreach.PlanningMarksAsync(scope);
        await Task.WhenAll(graph, coverage, marks);
        Campaign? campaign = desk.Campaigns.FirstOrDefault(found => found.Id == day.Campaign);
        return new PlanSetup(scope, day, campaign?.Name ?? "", campaign?.HandedOut ?? "", desk.Settings, CampaignStages.BundleSizeOf(campaign, desk.Settings), plan, desk.Areas, region, new OutreachAtlas(regions, []), graph.Result, coverage.Result, marks.Result);
    }

    private static async Task<StreetGraph?> LoadAsync(StreetGraphLoader streets, OutreachRegion region) => await streets.LoadAsync(region);

    private static OutreachRegion? RegionFor(IReadOnlyList<OutreachRegion> regions, DayPlan plan)
    {
        if (plan.Draft is { Region.Length: > 0 } draft && regions.FirstOrDefault(region => region.Id == draft.Region) is { } drafted)
            return drafted;
        GeoPoint? at = plan.Day.MeetingPoint is { Lat: not 0 } meeting ? meeting
            : plan.Routes.SelectMany(route => route.Stretches).Select(stretch => stretch.Middle).FirstOrDefault()
            ?? plan.Draft?.Area.FirstOrDefault();
        return at is null ? regions.FirstOrDefault(region => region.Streets.Url.Length > 0) : OutreachRegions.For(regions, at.Lat, at.Lng);
    }
}
