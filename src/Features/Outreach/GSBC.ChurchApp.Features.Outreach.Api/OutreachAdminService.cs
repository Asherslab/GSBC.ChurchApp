using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachAdminService(OutreachPlanning planning, OutreachReview review) : IOutreachAdminService
{
    public async ValueTask<RunningMinistries> MinistriesAsync(MinistryRef request, CallContext context = default) =>
        await planning.MinistriesAsync(context.CancellationToken);

    public async ValueTask<OutreachDesk> DeskAsync(MinistryRef request, CallContext context = default) =>
        await planning.DeskAsync(request.MinistryId, context.CancellationToken);

    public async ValueTask<Done> SaveSettingsAsync(SettingsSave request, CallContext context = default)
    {
        await planning.SaveSettingsAsync(StaffId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SaveCampaignAsync(CampaignSave request, CallContext context = default)
    {
        await planning.SaveCampaignAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> MoveCampaignAsync(CampaignMove request, CallContext context = default)
    {
        await planning.MoveCampaignAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<CampaignDeletion> CampaignDeletionAsync(ThingRef request, CallContext context = default) =>
        await planning.CampaignDeletionAsync(request, context.CancellationToken);

    public async ValueTask<Done> DeleteCampaignAsync(ThingRef request, CallContext context = default)
    {
        await planning.DeleteCampaignAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SaveDayAsync(DaySave request, CallContext context = default)
    {
        await planning.SaveDayAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteDayAsync(ThingRef request, CallContext context = default)
    {
        await planning.DeleteDayAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<DayDeletion> DayDeletionAsync(ThingRef request, CallContext context = default) =>
        await planning.DayDeletionAsync(request, context.CancellationToken);

    public async ValueTask<PlanningMarks> PlanningMarksAsync(MinistryRef request, CallContext context = default) =>
        await planning.PlanningMarksAsync(request.MinistryId, context.CancellationToken);

    public async ValueTask<DayPlan> PlanAsync(ThingRef request, CallContext context = default) =>
        await planning.PlanAsync(request, context.CancellationToken);

    public async ValueTask<Done> SaveDraftAsync(DraftSave request, CallContext context = default)
    {
        await planning.SaveDraftAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Published> PublishRoutesAsync(RoutesPublish request, CallContext context = default) =>
        await planning.PublishRoutesAsync(request, context.CancellationToken);

    public async ValueTask<DayRoutes> RoutesAsync(ThingRef request, CallContext context = default) =>
        await planning.RoutesAsync(request, context.CancellationToken);

    public async ValueTask<Done> SaveRouteAsync(RouteSave request, CallContext context = default)
    {
        await planning.SaveRouteAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteRouteAsync(ThingRef request, CallContext context = default)
    {
        await planning.DeleteRouteAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SaveAreaAsync(AreaSave request, CallContext context = default)
    {
        await planning.SaveAreaAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteAreaAsync(ThingRef request, CallContext context = default)
    {
        await planning.DeleteAreaAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<CoverageMap> CoverageAsync(CoverageQuery request, CallContext context = default) =>
        await review.CoverageAsync(request, context.CancellationToken);

    public async ValueTask<Expired> ExpireAsync(ExpireCoverage request, CallContext context = default) =>
        await review.ExpireAsync(request, context.CancellationToken);

    public async ValueTask<Expired> UndoExpiryAsync(ExpiryUndo request, CallContext context = default) =>
        await review.UndoExpiryAsync(request, context.CancellationToken);

    public async ValueTask<Totals> TotalsAsync(TotalsQuery request, CallContext context = default) =>
        await review.TotalsAsync(request, context.CancellationToken);

    public async ValueTask<DayBoard> BoardAsync(BoardQuery request, CallContext context = default) =>
        await review.BoardAsync(request, context.CancellationToken);

    public async ValueTask<Reports> ReportsAsync(MinistryRef request, CallContext context = default) =>
        await review.ReportsAsync(request.MinistryId, context.CancellationToken);

    public async ValueTask<Done> ResolveReportAsync(ReportResolve request, CallContext context = default)
    {
        await review.ResolveReportAsync(StaffId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteReportAsync(ThingRef request, CallContext context = default)
    {
        await review.DeleteReportAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<FollowUps> FollowUpsAsync(MinistryRef request, CallContext context = default) =>
        await review.FollowUpsAsync(request.MinistryId, context.CancellationToken);

    public async ValueTask<Done> FollowUpDoneAsync(FollowUpDone request, CallContext context = default)
    {
        await review.FollowUpDoneAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteFollowUpAsync(ThingRef request, CallContext context = default)
    {
        await review.DeleteFollowUpAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<SkipLines> SkipsAsync(MinistryRef request, CallContext context = default) =>
        await review.SkipsAsync(request.MinistryId, context.CancellationToken);

    public async ValueTask<Done> ClearSkipAsync(ThingRef request, CallContext context = default)
    {
        await review.ClearSkipAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Exclusions> ExclusionsAsync(MinistryRef request, CallContext context = default) =>
        await review.ExclusionsAsync(request.MinistryId, context.CancellationToken);

    public async ValueTask<Done> SaveZoneAsync(ZoneSave request, CallContext context = default)
    {
        await review.SaveZoneAsync(StaffId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteZoneAsync(ThingRef request, CallContext context = default)
    {
        await review.DeleteZoneAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SaveAddressAsync(AddressSave request, CallContext context = default)
    {
        await review.SaveAddressAsync(StaffId(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteAddressAsync(ThingRef request, CallContext context = default)
    {
        await review.DeleteAddressAsync(request, context.CancellationToken);
        return new Done();
    }

    private static Guid StaffId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user && Caller.UserId(user) is { } id
            ? id
            : throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
}
