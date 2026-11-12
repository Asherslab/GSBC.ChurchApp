using GSBC.ChurchApp.Features.Chats.Client;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.App;

public static class ChatsAppExtensions
{
    public static IServiceCollection AddChatsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Chats", typeof(ChatsAppExtensions).Assembly,
            [new NavEntry("Chats", "chats", AppIcons.Chats)], [], ChatsFeeds.Mine));
        return services.AddChatsOnPhone();
    }
}
