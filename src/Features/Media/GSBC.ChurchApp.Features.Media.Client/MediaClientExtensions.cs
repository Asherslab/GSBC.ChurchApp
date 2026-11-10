using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Uploads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Features.Media.Client;

public static class MediaClientExtensions
{
    public static IServiceCollection AddMediaClient(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services
            .AddDestinations(MediaDestinations.All)
            .AddSingleton(provider => new MediaClient(provider.GetRequiredService<Grpc.Core.ChannelBase>()))
            .AddSingleton<IPrivateMediaLinks, PrivateMediaLinks>()
            .AddSingleton<MediaFolderMemory>()
            .AddSingleton(new MediaPicker(typeof(MediaChoice)))
            .AddSingleton<IUploadRequests, MediaUploadRequests>()
            .AddSingleton<IClientStartupTask, PrivateMediaGuard>();
    }
}
