using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Native;

public static class NativeStartup
{
    public static async Task StartNativeDataLayerAsync(this IServiceProvider services, ClientModules modules)
    {
        await services.GetRequiredService<NativeBridge>().InitialiseAsync();
        await services.GetRequiredService<NativeLocalFeeds>().StartAsync(modules.Feeds);
        await services.GetRequiredService<NativeOutbox>().StartAsync();
    }
}
