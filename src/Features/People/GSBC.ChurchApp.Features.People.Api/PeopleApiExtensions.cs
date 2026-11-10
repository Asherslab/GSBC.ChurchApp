using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Api.SafeMinistry;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.Api;

public static class PeopleApiExtensions
{
    public static IServiceCollection AddPeopleApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPeopleData();
        services.AddScoped<Families>().AddScoped<FamilyViews>().AddScoped<MyFamilyFeed>();
        services.AddHostedService<MyFamilyUpkeep>();
        services.Configure<SafeMinistryOptions>(configuration.GetSection(SafeMinistryOptions.Section));
        services.AddSingleton<SmcGate>();
        services.AddHttpClient<SafeMinistryClient>(client => client.Timeout = TimeSpan.FromMinutes(1)).RemoveAllLoggers();
        services.AddScoped<SafeMinistrySync>();
        services.AddHostedService<SafeMinistrySchedule>();
        services.AddScoped<IPersonData, PeoplePersonData>();
        services.AddScoped<IFamilies, PeopleFamilies>();
        services.AddPermissions(PeoplePermissions.All);
        services.AddNotificationKinds(PeopleNotifications.All);

        services.AddSystemRole(new SystemRole(PeoplePermissions.ParentRole, "Parent or guardian",
            "Looks after this young person in the app: sees and changes their details, makes them a sign-in code, and acts for them", ScopeKind.Person,
            [PeoplePermissions.FamilyViewNode, PeoplePermissions.FamilyEditNode, PeoplePermissions.SignInCodeNode, PeoplePermissions.ActForNode],
            AssignableBy: [], Hidden: true));
        services.AddSystemRole(new SystemRole(PeoplePermissions.SafeguardingViewerRole, "Safeguarding Viewer",
            "See everyone's birth dates, families, blue cards and Safe Ministry training, and who's flagged", ScopeKind.Global,
            [PeoplePermissions.FamilyViewNode, PeoplePermissions.SafeguardingViewNode], Feature: RoleFeatures.People, Kits: [RoleKits.PeopleNames]));
        services.AddSystemRole(new SystemRole(PeoplePermissions.SafeguardingManagerRole, "Safeguarding Manager",
            "Everything a Safeguarding Viewer can, and link families, add children for a parent, make sign-in codes and record blue cards and training",
            ScopeKind.Global, ["people.*", $"-{PeoplePermissions.ActForNode}"], Feature: RoleFeatures.People, Kits: [RoleKits.PeopleNames]));

        services.AddFeed(new FeedDefinition(PeopleFeeds.Mine, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(PeopleFeeds.Me, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(PeopleFeeds.Family, FeedAudience.Holding(PeoplePermissions.FamilyView)));
        services.AddFeed(new FeedDefinition(PeopleFeeds.Safeguarding, FeedAudience.Holding(PeoplePermissions.SafeguardingView)));
        return services;
    }

    public static IEndpointRouteBuilder MapPeopleApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<PeopleService>();
        endpoints.MapGrpcService<SafeMinistryCheckService>();
        return endpoints;
    }
}
