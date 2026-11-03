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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api;

public static class AccountsApiExtensions
{
    public static IServiceCollection AddAccountsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AccountsOptions>(configuration.GetSection(AccountsOptions.Section));
        services.AddAccountsData();
        services
            .AddIdentityCore<AccountUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AccountsDbContext>();
        services.AddSystemRole(new SystemRole("account-manager", "Account manager",
            "Invite people, help them back in, disable accounts, read the audit log", ScopeKind.Global, ["accounts.*"]));
        services.AddSystemRole(DemoAccounts.Role);
        services.AddScoped<DemoAccounts>().AddScoped<ReviewSignIns>();
        services.AddHostedService<AccountsCleanup>();
        services.AddSingleton(new AppAttest(AppAttest.AppleRoot(), AppIds(configuration)));
        services.AddScoped<IInstallVerifier, InstallVerifier>();
        services.AddScoped<IPeopleIndex, AccountsPeopleIndex>();
        services.AddScoped<ISignedInSessions, AccountSessionCheck>();
        services.AddFeed(new FeedDefinition(AccountsFeeds.People, FeedAudience.Holding(AccountsPermissions.View)));
        services.AddScoped<IAccountEmails, AccountEmailLookup>();
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
