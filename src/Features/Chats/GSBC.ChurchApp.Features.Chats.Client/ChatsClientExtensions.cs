using GSBC.ChurchApp.Features.Chats.Client.Components;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.Client;

public static class ChatsClientExtensions
{
    public static IServiceCollection AddChatsOnPhone(this IServiceCollection services) =>
        services
            .AddDestinations(ChatsDestinations.All)
            .AddSingleton<ChatsStore>()
            .AddHubTile<ChatsTile>(ChatsTiles.Chats);
}
