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

    public async Task SaveDayAsync(string ministryId, OutreachDay day) => await _admin.SaveDayAsync(new DaySave { MinistryId = ministryId, Day = day });

    public async Task<DayDeletion> DayDeletionAsync(string ministryId, string id) => await _admin.DayDeletionAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task DeleteDayAsync(string ministryId, string id) => await _admin.DeleteDayAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task<DayPlan> PlanAsync(string ministryId, string dayId) => await _admin.PlanAsync(new ThingRef { MinistryId = ministryId, Id = dayId });

    public async Task SaveDraftAsync(string ministryId, string dayId, DayDraft? draft) =>
        await _admin.SaveDraftAsync(new DraftSave { MinistryId = ministryId, DayId = dayId, Draft = draft });

    public async Task<IReadOnlyList<DayRoute>> PublishAsync(string ministryId, string dayId, IReadOnlyList<DayRoute> routes) =>
        (await _admin.PublishRoutesAsync(new RoutesPublish { MinistryId = ministryId, DayId = dayId, Routes = [.. routes] })).Routes;

    public async Task<DayBoard> BoardAsync(string ministryId, string dayId) => await _admin.BoardAsync(new BoardQuery { MinistryId = ministryId, DayId = dayId });

    public async Task SaveAreaAsync(string ministryId, SavedArea area) => await _admin.SaveAreaAsync(new AreaSave { MinistryId = ministryId, Area = area });

    public async Task DeleteAreaAsync(string ministryId, string id) => await _admin.DeleteAreaAsync(new ThingRef { MinistryId = ministryId, Id = id });
}
