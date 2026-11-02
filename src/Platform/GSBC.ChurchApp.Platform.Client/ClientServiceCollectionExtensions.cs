using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Client;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformClient(this IServiceCollection services)
    {
        services.AddSingleton<SessionSignal>();
        services.AddSingleton<GrantedAccess>();
        services.AddSingleton<IAccess>(provider => provider.GetRequiredService<GrantedAccess>());
        services.TryAddSingleton<IScopeNames, UnnamedScopes>();
        services.TryAddSingleton<IMediaFiles, WebMediaFiles>();
        return services;
    }
}
