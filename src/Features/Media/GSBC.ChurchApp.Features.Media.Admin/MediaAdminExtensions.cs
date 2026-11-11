using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Features.Media.Client;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Media.Admin;

public static class MediaAdminExtensions
{
    public static IServiceCollection AddMediaAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Media", typeof(MediaAdminExtensions).Assembly,
            [new NavEntry("Media", "media", "image", MediaPermissions.View, Area: NavAreas.TheApp, Summary: "Pictures for the app's tiles and pages")]));
        return services.AddMediaClient()
            .AddSingleton<MediaLibraryClient>()
            .AddSingleton(provider => new MediaUploadClient(provider.GetRequiredService<Grpc.Core.ChannelBase>(), new HttpClient()))
            .AddSingleton(new MediaDirectUpload(typeof(MediaUpload)))
            .AddSingleton<IFontLibrary>(provider => new FontLibrary(provider.GetRequiredService<Grpc.Core.ChannelBase>(), provider.GetRequiredService<GSBC.ChurchApp.Platform.Client.Access.IAccess>()))
            .AddPlaceAction(new PlaceAction("Open in Media", "image", place => place.Kind == MediaDestinations.File.Key ? "media" : null, MediaPermissions.View, Leads: true));
    }
}
