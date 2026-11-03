using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Platform.Client.Diagnostics;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Transport;

public static class TransportServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcWebTransport(this IServiceCollection services, Uri gateway) =>
        services.AddSingleton<ChannelBase>(provider => new ObservedChannel(CreateGrpcWebChannel(gateway), provider));

    public static IServiceCollection AddNativeDataLayer(this IServiceCollection services, Uri browserGateway) =>
        services
            .AddSingleton<NativeBridge>()
            .AddSingleton<NativeLocalFeeds>()
            .AddSingleton<ILocalFeeds>(provider => provider.GetRequiredService<NativeLocalFeeds>())
            .AddSingleton<NativeOutbox>()
            .AddSingleton<IOutbox>(provider => provider.GetRequiredService<NativeOutbox>())
            .AddSingleton<IMediaFiles, NativeMediaFiles>()
            .AddSingleton<IDeviceSession, NativeDeviceSession>()
            .AddSingleton<ILocalPreferences, NativeLocalPreferences>()
            .AddSingleton<ChannelBase>(provider =>
            {
                NativeBridge bridge = provider.GetRequiredService<NativeBridge>();
                return new ObservedChannel(bridge.IsNative ? new NativeChannel(bridge) : CreateGrpcWebChannel(browserGateway), provider);
            });

    private static ChannelBase CreateGrpcWebChannel(Uri gateway) =>
        GrpcChannel.ForAddress(gateway, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
        });
}
