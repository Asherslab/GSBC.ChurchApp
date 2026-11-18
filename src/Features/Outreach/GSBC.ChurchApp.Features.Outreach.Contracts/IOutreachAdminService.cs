using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

[Service("gsbc.outreach.OutreachAdmin")]
public interface IOutreachAdminService
{
    [SignedIn]
    ValueTask<RunningMinistries> MinistriesAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReportsNode)]
    ValueTask<OutreachDesk> DeskAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<Done> SaveSettingsAsync(SettingsSave request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> SaveCampaignAsync(CampaignSave request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> MoveCampaignAsync(CampaignMove request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<CampaignDeletion> CampaignDeletionAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> DeleteCampaignAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> SaveDayAsync(DaySave request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> DeleteDayAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<DayDeletion> DayDeletionAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<PlanningMarks> PlanningMarksAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<DayPlan> PlanAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> SaveDraftAsync(DraftSave request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Published> PublishRoutesAsync(RoutesPublish request, CallContext context = default);

    [Requires(OutreachPermissions.ReportsNode)]
    ValueTask<DayRoutes> RoutesAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> SaveRouteAsync(RouteSave request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> DeleteRouteAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> SaveAreaAsync(AreaSave request, CallContext context = default);

    [Requires(OutreachPermissions.PlanNode)]
    ValueTask<Done> DeleteAreaAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReportsNode)]
    ValueTask<CoverageMap> CoverageAsync(CoverageQuery request, CallContext context = default);

    [Requires(OutreachPermissions.ExpireNode)]
    ValueTask<Expired> ExpireAsync(ExpireCoverage request, CallContext context = default);

    [Requires(OutreachPermissions.ExpireNode)]
    ValueTask<Expired> UndoExpiryAsync(ExpiryUndo request, CallContext context = default);

    [Requires(OutreachPermissions.ReportsNode)]
    ValueTask<Totals> TotalsAsync(TotalsQuery request, CallContext context = default);

    [Requires(OutreachPermissions.ReportsNode)]
    ValueTask<DayBoard> BoardAsync(BoardQuery request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<Reports> ReportsAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<Done> ResolveReportAsync(ReportResolve request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<Done> DeleteReportAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<FollowUps> FollowUpsAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<Done> FollowUpDoneAsync(FollowUpDone request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<Done> DeleteFollowUpAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<SkipLines> SkipsAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.ReviewNode)]
    ValueTask<Done> ClearSkipAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<Exclusions> ExclusionsAsync(MinistryRef request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<Done> SaveZoneAsync(ZoneSave request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<Done> DeleteZoneAsync(ThingRef request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<AddressMatches> FindAddressesAsync(AddressSearch request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<Done> SaveAddressAsync(AddressSave request, CallContext context = default);

    [Requires(OutreachPermissions.SettingsNode)]
    ValueTask<Done> DeleteAddressAsync(ThingRef request, CallContext context = default);
}
