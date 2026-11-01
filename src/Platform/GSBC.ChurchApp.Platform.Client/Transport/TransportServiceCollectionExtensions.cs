using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Transport;

public static class TransportServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcWebTransport(this IServiceCollection services, Uri gateway) =>
        services.AddSingleton(_ => CreateGrpcWebChannel(gateway));

    public static IServiceCollection AddNativeOrGrpcWebTransport(this IServiceCollection services, Uri browserGateway) =>
        services
            .AddSingleton<NativeBridge>()
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
