using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Api;

public static class MinistriesApiExtensions
{
    private const string PeopleParentRole = "parent-or-guardian";
    private const string EditCalendarsNode = "events.edit";
    private const string PublishCalendarsNode = "events.publish";

    internal static readonly IReadOnlyList<string> LeaderPatterns =
    [
        MinistriesPermissions.TeamViewNode, MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode,
        MinistriesPermissions.PostNode, MinistriesPermissions.PeopleNode, MinistriesPermissions.SettingsNode,
        EditCalendarsNode, PublishCalendarsNode,
    ];

    public static IServiceCollection AddMinistriesApi(this IServiceCollection services)
    {
        services.AddMinistriesData();
        services.AddScoped<MinistryViews>().AddScoped<MinistryChanges>().AddScoped<MinistryRemover>().AddScoped<MinistryMover>();
        services.AddSingleton<IScopeHierarchy, MinistryHierarchy>();
        services.AddScoped<IScopeObjects, MinistryScopeObjects>();
        services.AddScoped<IPersonData, MinistriesPersonData>();
        services.AddHostedService<SampleMinistries>();
        services.AddPermissions(MinistriesPermissions.All);
        services.AddNotificationKinds(MinistriesNotifications.All);

        string[] givers = [MinistriesPermissions.LeaderRole, MinistriesPermissions.AdminRole, MinistriesPermissions.ManagerRole];
        services.AddSystemRole(new SystemRole(MinistriesPermissions.LeaderRole, "Leader",
            "Leads this ministry and everything under it: its people, posts, files, calendars and settings. Listed as a leader", ScopeKind.Ministry,
            LeaderPatterns, AssignableBy: givers, AppliesToChildren: true, Feature: RoleFeatures.Ministries, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.AdminRole, "Admin",
            "Everything a leader can do in this ministry and under it, without being listed as a leader", ScopeKind.Ministry,
            LeaderPatterns, AssignableBy: givers, AppliesToChildren: true, Feature: RoleFeatures.Ministries, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.TeamRole, "Team",
            "On this ministry's team: sees what it shares with its team, attendees and parents", ScopeKind.Ministry,
            [MinistriesPermissions.TeamViewNode, MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode],
            AssignableBy: givers, Feature: RoleFeatures.Ministries));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.AttendeeRole, "Attendee",
            "Comes along to this ministry: sees what it shares with its attendees. Their parents see it too", ScopeKind.Ministry,
            [MinistriesPermissions.AttendeesViewNode], AssignableBy: givers, Feature: RoleFeatures.Ministries,
            Through: [new RoleThrough(PeopleParentRole, MinistriesPermissions.AttendeesParentRole)]));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.AttendeesParentRole, "Attendee's parent",
            "A parent of someone who comes along: sees what the ministry shares with attendees and parents", ScopeKind.Ministry,
            [MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode], AssignableBy: [], Hidden: true));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.ManagerRole, "Ministries Manager",
            "Creates and deletes the church's ministries and does everything a leader can in every one of them", ScopeKind.Global,
            ["ministries.*"], Feature: RoleFeatures.Ministries, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.LifeGroupsPlannerRole, "Life Groups planner",
            "Plans the Life Groups: the Sundays each set of groups meets, their time and when they show in the app", ScopeKind.Global,
            [MinistriesPermissions.PlanLifeGroupsNode], Feature: RoleFeatures.Ministries));

        services.AddFeed(new FeedDefinition(MinistriesFeeds.Ministries, FeedAudience.Public));
        services.AddDestinations(MinistriesDestinations.All);
        services.AddDestinationSource<MinistryDestinationSource>();
        services.AddSurfaces(MinistriesTiles.Surfaces);
        return services;
    }

    public static IEndpointRouteBuilder MapMinistriesApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<MinistriesService>();
        endpoints.MapGrpcService<MinistryLeadingService>();
        return endpoints;
    }
}
