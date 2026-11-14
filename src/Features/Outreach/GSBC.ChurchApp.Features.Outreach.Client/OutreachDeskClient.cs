using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Outreach.Client;

[AdminOnly]
public sealed class OutreachDeskClient(ChannelBase channel)
{
    private readonly IOutreachAdminService _admin = channel.CreateGrpcService<IOutreachAdminService>();

    public async Task<RunningMinistries> MinistriesAsync() => await _admin.MinistriesAsync(new MinistryRef());

    public async Task<OutreachDesk> DeskAsync(string ministryId) => await _admin.DeskAsync(new MinistryRef { MinistryId = ministryId });

    public async Task SaveSettingsAsync(string ministryId, OutreachSettings settings) =>
        await _admin.SaveSettingsAsync(new SettingsSave { MinistryId = ministryId, Settings = settings });

    public async Task SaveCampaignAsync(string ministryId, Campaign campaign) =>
        await _admin.SaveCampaignAsync(new CampaignSave { MinistryId = ministryId, Campaign = campaign });

    public async Task<CampaignDeletion> CampaignDeletionAsync(string ministryId, string id) =>
        await _admin.CampaignDeletionAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task DeleteCampaignAsync(string ministryId, string id) => await _admin.DeleteCampaignAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task<FollowUps> FollowUpsAsync(string ministryId) => await _admin.FollowUpsAsync(new MinistryRef { MinistryId = ministryId });

    public async Task FollowUpDoneAsync(string ministryId, string id, bool done) =>
        await _admin.FollowUpDoneAsync(new FollowUpDone { MinistryId = ministryId, Id = id, Done = done });

    public async Task DeleteFollowUpAsync(string ministryId, string id) => await _admin.DeleteFollowUpAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task<Reports> ReportsAsync(string ministryId) => await _admin.ReportsAsync(new MinistryRef { MinistryId = ministryId });

    public async Task ResolveReportAsync(string ministryId, string id, ReportState outcome, ExclusionReason excludeAs = ExclusionReason.NotHomes, List<GeoPoint>? area = null) =>
        await _admin.ResolveReportAsync(new ReportResolve { MinistryId = ministryId, Id = id, Outcome = outcome, ExcludeAs = excludeAs, Area = area ?? [] });

    public async Task DeleteReportAsync(string ministryId, string id) => await _admin.DeleteReportAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task<Totals> TotalsAsync(string ministryId, DateTime from, DateTime to, TotalsBy by, string campaign) =>
        await _admin.TotalsAsync(new TotalsQuery { MinistryId = ministryId, From = from, To = to, By = by, Campaign = campaign });

    public async Task<CoverageMap> CoverageAsync(string ministryId, string campaign) =>
        await _admin.CoverageAsync(new CoverageQuery { MinistryId = ministryId, Campaign = campaign });

    public async Task<Expired> ExpireAsync(string ministryId, string campaign, List<GeoPoint> area, List<Stretch> stretches) =>
        await _admin.ExpireAsync(new ExpireCoverage { MinistryId = ministryId, Campaign = campaign, Area = area, Stretches = stretches });

    public async Task<Expired> UndoExpiryAsync(string ministryId, string campaign, DateTime at) =>
        await _admin.UndoExpiryAsync(new ExpiryUndo { MinistryId = ministryId, Campaign = campaign, At = at });

    public async Task<SkipLines> SkipsAsync(string ministryId) => await _admin.SkipsAsync(new MinistryRef { MinistryId = ministryId });

    public async Task ClearSkipAsync(string ministryId, string id) => await _admin.ClearSkipAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task<Exclusions> ExclusionsAsync(string ministryId) => await _admin.ExclusionsAsync(new MinistryRef { MinistryId = ministryId });

    public async Task SaveZoneAsync(string ministryId, ExclusionZone zone) => await _admin.SaveZoneAsync(new ZoneSave { MinistryId = ministryId, Zone = zone });

    public async Task DeleteZoneAsync(string ministryId, string id) => await _admin.DeleteZoneAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task SaveAddressAsync(string ministryId, ExclusionAddress address) =>
        await _admin.SaveAddressAsync(new AddressSave { MinistryId = ministryId, Address = address });

    public async Task DeleteAddressAsync(string ministryId, string id) => await _admin.DeleteAddressAsync(new ThingRef { MinistryId = ministryId, Id = id });
}
