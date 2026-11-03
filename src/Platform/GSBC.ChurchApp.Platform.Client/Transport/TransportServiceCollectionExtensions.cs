using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Platform.Client.Transport;

public static class TransportServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcWebTransport(this IServiceCollection services, Uri gateway) =>
        services
            .AddSingleton(_ => CreateGrpcWebChannel(gateway))
            .AddSingleton(provider => new FeedWatch(provider.GetRequiredService<ChannelBase>().CreateGrpcService<ISyncService>(), provider.GetRequiredService<SessionSignal>()))
            .AddSingleton<IFeedSignals>(provider => provider.GetRequiredService<FeedWatch>())
            .AddSingleton<IClientStartupTask, FeedWatchWaker>();

    public static IServiceCollection AddNativeDataLayer(this IServiceCollection services, Uri browserGateway) =>
        services
            .AddSingleton<NativeBridge>()
            .AddSingleton<NativeLocalFeeds>()
            .AddSingleton<ILocalFeeds>(provider => provider.GetRequiredService<NativeLocalFeeds>())
            .AddSingleton<IFeedSignals>(provider => provider.GetRequiredService<NativeLocalFeeds>())
            .AddSingleton<NativeOutbox>()
            .AddSingleton<IOutbox>(provider => provider.GetRequiredService<NativeOutbox>())
            .AddSingleton<IMediaFiles, NativeMediaFiles>()
            .AddSingleton<IDeviceSession, NativeDeviceSession>()
            .AddSingleton<ILocalPreferences, NativeLocalPreferences>()
            .AddSingleton(provider =>
            {
                NativeBridge bridge = provider.GetRequiredService<NativeBridge>();
                return bridge.IsNative ? new NativeChannel(bridge) : CreateGrpcWebChannel(browserGateway);
            });

    private static ChannelBase CreateGrpcWebChannel(Uri gateway) =>
        GrpcChannel.ForAddress(gateway, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
        });
}
