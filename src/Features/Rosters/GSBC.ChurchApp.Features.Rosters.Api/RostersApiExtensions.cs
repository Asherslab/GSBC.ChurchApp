using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Api;

public static class RostersApiExtensions
{
    private const string LeaderRole = "ministry-leader";
    private const string AdminRole = "ministry-admin";
    private const string TeamRole = "ministry-team";

    public static IServiceCollection AddRostersApi(this IServiceCollection services)
    {
        services.AddRostersData();
        services.AddScoped<RosterViews>().AddScoped<RosterPublisher>().AddScoped<RosterMessenger>().AddScoped<RosterReminders>().AddScoped<RosterRefresh>();
        services.AddScoped<IPersonData, RostersPersonData>();
        services.AddHostedService<RostersSchedule>();
        services.AddPermissions(RostersPermissions.All);
        services.AddNotificationKinds(RostersNotifications.All);
        services.AddToSystemRole(LeaderRole, RostersPermissions.EditNode, RostersPermissions.ReplyNode);
        services.AddToSystemRole(AdminRole, RostersPermissions.EditNode, RostersPermissions.ReplyNode);
        services.AddToSystemRole(TeamRole, RostersPermissions.ReplyNode);
        services.AddFeed(new FeedDefinition(RostersFeeds.Ministries, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(RostersFeeds.Away, FeedAudience.OwnRecords));
        services.AddSurfaces(RostersTiles.Surfaces);
        return services;
    }

    public static IEndpointRouteBuilder MapRostersApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<RostersService>();
        return endpoints;
    }
}
