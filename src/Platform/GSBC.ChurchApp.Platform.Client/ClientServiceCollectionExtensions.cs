using GSBC.ChurchApp.Platform.Client.Access;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformClient(this IServiceCollection services)
    {
        services.AddSingleton<GrantedAccess>();
        services.AddSingleton<IAccess>(provider => provider.GetRequiredService<GrantedAccess>());
        return services;
    }
}
