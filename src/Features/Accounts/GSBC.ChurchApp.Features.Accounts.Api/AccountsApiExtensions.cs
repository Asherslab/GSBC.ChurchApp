using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
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
        services.AddHostedService<AccountsCleanup>();
        return services.AddPermissions(AccountsPermissions.All);
    }

    public static IEndpointRouteBuilder MapAccountsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<SessionService>();
        endpoints.MapGrpcService<AccountsService>();
        endpoints.MapGrpcService<PhoneLinkService>();
        return endpoints;
    }
}
