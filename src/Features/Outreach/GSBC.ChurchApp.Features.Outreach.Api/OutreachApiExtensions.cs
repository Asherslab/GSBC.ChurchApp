using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Api;

public static class OutreachApiExtensions
{
    private const string MinistryLeaderRole = "ministry-leader";
    private const string MinistryAdminRole = "ministry-admin";
    private const string MinistryTeamRole = "ministry-team";

    public static IServiceCollection AddOutreachApi(this IServiceCollection services)
    {
        services.AddOutreachData();
        services.AddScoped<OutreachMinistries>().AddScoped<OutreachPublisher>().AddScoped<OutreachSettingsStore>().AddScoped<OutreachMedia>()
            .AddScoped<OutreachTelling>().AddScoped<OutreachLeaderboards>().AddScoped<Walking>().AddScoped<OutreachPlanning>().AddScoped<OutreachReview>()
            .AddScoped<OutreachKeeping>();
        services.AddScoped<IPersonData, OutreachPersonData>();
        services.AddScoped<IMediaAreaAccess, ReportPhotoAccess>();
        services.AddMediaSystemArea(OutreachMedia.ReportPhotos);
        services.AddHostedService<OutreachSchedule>();
        services.AddPermissions(OutreachPermissions.All);
        services.AddNotificationKinds(OutreachNotifications.All);

        services.AddToSystemRole(MinistryLeaderRole, OutreachPermissions.LeaderNodes);
        services.AddToSystemRole(MinistryAdminRole, OutreachPermissions.LeaderNodes);
        services.AddToSystemRole(MinistryTeamRole, OutreachPermissions.ReportsNode);
        services.AddSystemRole(new SystemRole(OutreachPermissions.ManagerRole, "Outreach Manager",
            "Runs the church's outreach without leading the Outreach ministry: campaigns, outreach days, reports, follow-ups, settings and the maps",
            ScopeKind.Global, ["outreach.*"], Feature: RoleFeatures.Outreach));

        foreach (string feed in OutreachFeeds.Public)
            services.AddFeed(new FeedDefinition(feed, FeedAudience.Public));
        services.AddFeed(new FeedDefinition(OutreachFeeds.Mine, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(OutreachFeeds.FollowUps, FeedAudience.Holding(OutreachPermissions.Review)));
        services.AddFeed(new FeedDefinition(OutreachFeeds.Desk, FeedAudience.Holding(OutreachPermissions.Reports)));
        return services;
    }

    public static IEndpointRouteBuilder MapOutreachApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<OutreachService>();
        endpoints.MapGrpcService<OutreachAdminService>();
        return endpoints;
    }
}
