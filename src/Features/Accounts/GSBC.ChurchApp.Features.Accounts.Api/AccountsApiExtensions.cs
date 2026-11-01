using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Api.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api;

public static class AccountsApiExtensions
{
    public static IServiceCollection AddAccountsApi(this IServiceCollection services) =>
        services.AddPermissions(AccountsPermissions.All);

    public static IEndpointRouteBuilder MapAccountsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<SessionService>();
        return endpoints;
    }
}
