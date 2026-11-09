using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Api;

public static class AccessApiExtensions
{
    public static IServiceCollection AddAccessApi(this IServiceCollection services)
    {
        services.AddAccessData();
        services.AddSingleton<GrantCache>();
        services.AddHttpContextAccessor().AddSingleton<ToolCallers>();
        services.AddScoped<IAccessEvaluator, AccessEvaluator>();
        services.AddScoped<GrantsFeed>();
        services.AddScoped<AccessChanges>();
        services.AddScoped<IPersonData, AccessPersonData>();
        services.AddHostedService<GrantsFeedSweep>();
        services.AddFeed(new FeedDefinition(AccessFeeds.Grants, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(AccessFeeds.Roles, FeedAudience.Holding(AccessPermissions.View)));
        services.AddScoped<AssignmentRules>();
        services.AddScoped<AccessInsights>();
        services.AddScoped<RoleAssignments>();
        services.AddScoped<IRoleAssignments>(provider => provider.GetRequiredService<RoleAssignments>());
        services.AddScoped<IPhoneRoles>(provider => provider.GetRequiredService<RoleAssignments>());
        services.AddScoped<IPermissionHolders, PermissionHolderLookup>();
        services.AddSingleton(provider =>
        {
            GrantCache cache = provider.GetRequiredService<GrantCache>();
            return new NotificationHandler(AccessDbContext.ChangesChannel, _ => cache.Clear(), cache.Clear);
        });
        services.AddHostedService<AccessReconciler>();
        services.AddPermissions(AccessPermissions.All);
        services.AddSystemRole(new SystemRole(AccessService.OwnerKey, "Owner", "Everything, including making other Owners", ScopeKind.Global, ["*"],
            Feature: RoleFeatures.Everything));
        services.AddSystemRole(new SystemRole(AccessService.RolesManagerKey, "Roles Manager",
            "Give people roles and take them away, but only roles whose abilities they have themselves", ScopeKind.Global,
            [AccessPermissions.ViewNode, AccessPermissions.EditAssignmentsNode], Feature: RoleFeatures.People, Kits: [RoleKits.PeopleNames],
            Replaces: ["access-manager"]));
        return services;
    }

    public static IEndpointRouteBuilder MapAccessApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AccessService>();
        return endpoints;
    }
}
