using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.News;
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
    private const string ManageChatsNode = "chats.manage";
    private const string CheckChatsNode = "chats.check";

    internal static readonly IReadOnlyList<string> LeaderPatterns =
    [
        MinistriesPermissions.TeamViewNode, MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode, MinistriesPermissions.SeeHiddenNode,
        MinistriesPermissions.PostNode, MinistriesPermissions.PeopleNode, MinistriesPermissions.SettingsNode,
        EditCalendarsNode, PublishCalendarsNode, ManageChatsNode, CheckChatsNode,
    ];

    public static IServiceCollection AddMinistriesApi(this IServiceCollection services)
    {
        services.AddMinistriesData();
        services.AddScoped<MinistryViews>().AddScoped<MinistriesPublisher>().AddScoped<MinistryChanges>().AddScoped<MinistryRemover>().AddScoped<MinistryMover>().AddScoped<MinistryFileMedia>();
        services.AddScoped<MinistryPostMedia>().AddScoped<MinistryChurchNews>().AddScoped<ISharedPostSource>(provider => provider.GetRequiredService<MinistryChurchNews>());
        services.AddMediaSystemArea(MinistryFileMedia.Area);
        services.AddMediaSystemArea(MinistryPostMedia.Area);
        services.AddSingleton<IScopeHierarchy, MinistryHierarchy>();
        services.AddScoped<IScopeObjects, MinistryScopeObjects>();
        services.AddScoped<IPersonData, MinistriesPersonData>();
        services.AddScoped<IMinistryFacts, MinistryFactsSource>();
        services.AddScoped<IChatPlaces, MinistryChatPlaces>();
        services.AddHostedService<SampleMinistries>();
        services.AddHostedService<MinistriesFeedUpkeep>();
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
            [MinistriesPermissions.TeamViewNode, MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode, MinistriesPermissions.SeeHiddenNode],
            AssignableBy: givers, Feature: RoleFeatures.Ministries));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.AttendeeRole, "Attendee",
            "Comes along to this ministry: sees what it shares with its attendees. Their parents see it too", ScopeKind.Ministry,
            [MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.SeeHiddenNode], AssignableBy: givers, Feature: RoleFeatures.Ministries,
            Through: [new RoleThrough(PeopleParentRole, MinistriesPermissions.AttendeesParentRole)]));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.AttendeesParentRole, "Attendee's parent",
            "A parent of someone who comes along: sees what the ministry shares with attendees and parents", ScopeKind.Ministry,
            [MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode, MinistriesPermissions.SeeHiddenNode], AssignableBy: [], Hidden: true));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.ManagerRole, "Ministries Manager",
            "Creates and deletes the church's ministries and does everything a leader can in every one of them", ScopeKind.Global,
            ["ministries.*"], Feature: RoleFeatures.Ministries, Kits: [RoleKits.Pictures]));
        services.AddSystemRole(new SystemRole(MinistriesPermissions.LifeGroupsPlannerRole, "Life Groups planner",
            "Plans the Life Groups: the Sundays each set of groups meets, their time and when they show in the app", ScopeKind.Global,
            [MinistriesPermissions.PlanLifeGroupsNode], Feature: RoleFeatures.Ministries));

        services.AddFeed(new FeedDefinition(MinistriesFeeds.Ministries, FeedAudience.Public));
        foreach (string feed in MinistriesFeeds.OnPhones)
            services.AddFeed(new FeedDefinition(feed, FeedAudience.Public));
        foreach (string feed in new[] { MinistriesFeeds.PrivateDirectory, MinistriesFeeds.PrivatePosts, MinistriesFeeds.PrivateFiles, MinistriesFeeds.PrivateCalendars, MinistriesFeeds.People })
            services.AddFeed(new FeedDefinition(feed, FeedAudience.EachRow));
        services.AddFeed(new FeedDefinition(MinistriesFeeds.Yours, FeedAudience.OwnRecords));
        services.AddDestinations(MinistriesDestinations.All);
        services.AddDestinationSource<MinistryDestinationSource>();
        services.AddAppCardSource<MinistryCardSource>();
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
