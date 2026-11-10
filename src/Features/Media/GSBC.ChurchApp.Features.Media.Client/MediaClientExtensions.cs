using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Features.Media.Client;

public static class MediaClientExtensions
{
    public static IServiceCollection AddMediaClient(this IServiceCollection services) =>
        services.AddMediaShared().AddSingleton(new MediaPicker(typeof(MediaChoice)));

    public static IServiceCollection AddMediaOnPhone(this IServiceCollection services) =>
        services.AddMediaShared().AddSingleton(new MediaPicker(typeof(MediaReuse)));

    private static IServiceCollection AddMediaShared(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services
            .AddDestinations(MediaDestinations.All)
            .AddSingleton(provider => new MediaClient(provider.GetRequiredService<Grpc.Core.ChannelBase>(), new HttpClient()))
            .AddSingleton<IPrivateMediaLinks, PrivateMediaLinks>()
            .AddSingleton<MediaFolderMemory>()
            .AddSingleton<IFontLibrary>(provider => new FontLibrary(provider.GetRequiredService<Grpc.Core.ChannelBase>(), provider.GetRequiredService<GSBC.ChurchApp.Platform.Client.Access.IAccess>()))
            .AddSingleton<IClientStartupTask, PrivateMediaGuard>();
    }
}
