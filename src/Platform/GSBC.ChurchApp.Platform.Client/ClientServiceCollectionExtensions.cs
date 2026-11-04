using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Analytics;
using GSBC.ChurchApp.Platform.Client.Audio;
using GSBC.ChurchApp.Platform.Client.Diagnostics;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Client.Signals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Client;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformClient(this IServiceCollection services)
    {
        services.AddSingleton<SessionSignal>();
        services.AddSingleton<ClientSignals>();
        services.AddSingleton<GrantedAccess>();
        services.AddSingleton<IAccess>(provider => provider.GetRequiredService<GrantedAccess>());
        services.TryAddSingleton<IScopeNames, ScopeNames>();
        services.TryAddSingleton<IMediaFiles, WebMediaFiles>();
        services.TryAddSingleton<IDeviceSession, CookieSession>();
        services.AddSingleton<CurrentPage>();
        services.AddSingleton<OpeningSheets>();
        services.TryAddSingleton<IUsage, NoUsage>();
        services.TryAddSingleton<IAudioPlayer, AudioPlayer>();
        return services;
    }
}
