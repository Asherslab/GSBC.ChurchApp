using GSBC.ChurchApp.Features.Chats.Client;
using GSBC.ChurchApp.Features.Chats.Client.Components;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.Admin;

public static class ChatsAdminExtensions
{
    public static IServiceCollection AddChatsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Chats", typeof(ChatsAdminExtensions).Assembly,
            [
                new NavEntry("Chats", UnreadChats.Href, AppIcons.Chats, Summary: "Your own chats, from every ministry you're in: read, reply and react from this computer"),
                new NavEntry("Chat safety", "chat-safety", AppIcons.ChatSafety, ChatsPermissions.Check, Area: NavAreas.Safety,
                    Summary: "Reports about messages, checking a ministry's chats (every check is logged), and people paused from chats"),
            ],
            [ChatsFeeds.Oversight], ChatsFeeds.Mine));
        return services
            .AddSingleton(provider => new OversightClient(provider.GetRequiredService<Grpc.Core.ChannelBase>()))
            .AddSingleton<IChats, OnlineChats>()
            .AddSingleton<INavCounts, UnreadChats>()
            .AddSingleton<ChatChecks>()
            .AddSingleton(new MinistryPartView(ChatsParts.Key, On));
    }

    private static RenderFragment On(MinistryOnPage on) => builder =>
    {
        builder.OpenComponent<Pages.MinistryChatsAdmin>(0);
        builder.AddComponentParameter(1, nameof(Pages.MinistryChatsAdmin.On), on);
        builder.CloseComponent();
    };
}
