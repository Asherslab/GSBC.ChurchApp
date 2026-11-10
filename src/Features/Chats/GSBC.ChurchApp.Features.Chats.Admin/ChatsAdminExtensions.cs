using GSBC.ChurchApp.Features.Chats.Client;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.Admin;

public static class ChatsAdminExtensions
{
    public static IServiceCollection AddChatsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Chats", typeof(ChatsAdminExtensions).Assembly,
            [new NavEntry("Chats", "chats", "message-square", ChatsPermissions.Check, Area: NavAreas.People,
                Summary: "Reports about messages, checking a ministry's chats (every check is logged), and people paused from chats")],
            [ChatsFeeds.Oversight], []));
        return services.AddSingleton(provider => new OversightClient(provider.GetRequiredService<Grpc.Core.ChannelBase>()));
    }
}
