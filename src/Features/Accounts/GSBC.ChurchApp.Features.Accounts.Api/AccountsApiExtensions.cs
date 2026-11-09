using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Api.Installs;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api;

public static class AccountsApiExtensions
{
    private const string DeveloperRole = "developer";

    public static IServiceCollection AddAccountsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AccountsOptions>(configuration.GetSection(AccountsOptions.Section));
        services.AddAccountsData();
        services
            .AddIdentityCore<AccountUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AccountsDbContext>();
        services.AddRoleKit(new RoleKit(RoleKits.PeopleNames, [AccountsPermissions.ViewNode]));
        services.AddSystemRole(new SystemRole("people-viewer", "People Viewer", "See everyone's name and email", ScopeKind.Global,
            [AccountsPermissions.ViewNode], Feature: RoleFeatures.People));
        services.AddSystemRole(new SystemRole("people-manager", "People Manager",
            "Invite people, help them back in, turn accounts off, delete accounts and read the activity log", ScopeKind.Global,
            [AccountsPermissions.ViewNode, AccountsPermissions.ManageNode], Feature: RoleFeatures.People, Replaces: ["account-manager"]));
        services.AddSystemRole(new SystemRole("app-review-manager", "App Review Manager",
            "Make demo accounts and sign-in codes for Apple's app reviewers", ScopeKind.Global,
            [AccountsPermissions.ReviewAccessNode], Feature: RoleFeatures.People, StartsWithHoldersOf: ["account-manager"]));
        services.AddSystemRole(DemoAccounts.Role);
        services.AddToSystemRole(DeveloperRole, AccountsPermissions.ConnectToolNode);
        services.AddScoped<DemoAccounts>().AddScoped<ReviewSignIns>().AddScoped<Invitations>().AddScoped<AccountDeletion>();
        services.AddHostedService<AccountsCleanup>();
        services.AddSingleton(new AppAttest(AppAttest.AppleRoot(), AppIds(configuration)));
        services.AddScoped<IInstallVerifier, InstallVerifier>();
        services.AddScoped<IPeopleIndex, AccountsPeopleIndex>();
        services.AddScoped<ISignedInSessions, AccountSessionCheck>();
        services.AddFeed(new FeedDefinition(AccountsFeeds.People, FeedAudience.Holding(AccountsPermissions.View)));
        services.AddScoped<IAccountEmails, AccountEmailLookup>();
        services.AddDestinations(AccountsDestinations.All);
        return services.AddPermissions(AccountsPermissions.All);
    }

    public static IEndpointRouteBuilder MapAccountsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<SessionService>();
        endpoints.MapGrpcService<AccountsService>();
        endpoints.MapGrpcService<InstallsService>();
        return endpoints;
    }

    private static string[] AppIds(IConfiguration configuration)
    {
        IConfigurationSection apple = configuration.GetSection("Apple");
        return apple["TeamId"] is { Length: > 0 } teamId
            ? [$"{teamId.Trim()}.{(apple["BundleId"] is { Length: > 0 } bundleId ? bundleId : "au.com.baptist.app")}"]
            : [];
    }
}
