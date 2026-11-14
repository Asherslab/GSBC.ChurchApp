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

    public async Task ResolveReportAsync(string ministryId, string id, ReportState outcome) =>
        await _admin.ResolveReportAsync(new ReportResolve { MinistryId = ministryId, Id = id, Outcome = outcome, ExcludeAs = ExclusionReason.NotHomes });

    public async Task DeleteReportAsync(string ministryId, string id) => await _admin.DeleteReportAsync(new ThingRef { MinistryId = ministryId, Id = id });
}
