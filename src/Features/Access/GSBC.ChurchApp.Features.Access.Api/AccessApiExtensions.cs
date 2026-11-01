using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Notifications;
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
        services.AddScoped<IAccessEvaluator, AccessEvaluator>();
        services.AddSingleton(provider =>
        {
            GrantCache cache = provider.GetRequiredService<GrantCache>();
            return new NotificationHandler(AccessDbContext.ChangesChannel, _ => cache.Clear(), cache.Clear);
        });
        services.AddHostedService<AccessReconciler>();
        services.AddPermissions(AccessPermissions.All);
        services.AddSystemRole(new SystemRole(AccessService.OwnerKey, "Owner", "Everything, including making other owners", ScopeKind.Global, ["*"]));
        services.AddSystemRole(new SystemRole("access-manager", "Access manager", "Manage roles and who holds them", ScopeKind.Global,
            ["access.*", "accounts.view"]));
        return services;
    }

    public static IEndpointRouteBuilder MapAccessApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<AccessService>();
        return endpoints;
    }
}
