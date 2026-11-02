using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Native;

public static class NativeStartup
{
    public static async Task<NativeBridge> InitialiseNativeAsync(this IServiceProvider services)
    {
        NativeBridge bridge = services.GetRequiredService<NativeBridge>();
        await bridge.InitialiseAsync();
        return bridge;
    }

    public static async Task StartNativeDataLayerAsync(this IServiceProvider services, ClientModules modules)
    {
        await services.GetRequiredService<NativeLocalFeeds>().StartAsync(modules.Feeds, modules.PrivateFeeds);
        await services.GetRequiredService<NativeOutbox>().StartAsync();
        services.GetService<MediaPrefetcher>()?.Start();
    }
}
