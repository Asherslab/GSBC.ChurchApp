using GSBC.ChurchApp.Features.Chats.Client;
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
            [new NavEntry("Chats", "chats", AppIcons.Chats, ChatsPermissions.Check, Area: NavAreas.MinistriesAndGroups,
                Summary: "Reports about messages, checking a ministry's chats (every check is logged), and people paused from chats")],
            [ChatsFeeds.Oversight], []));
        return services
            .AddSingleton(provider => new OversightClient(provider.GetRequiredService<Grpc.Core.ChannelBase>()))
            .AddSingleton<ChatChecks>()
            .AddSingleton(new MinistryPartView(ChatsParts.Key, On));
    }

    private static RenderFragment On(MinistryOnPage on) => builder =>
    {
        builder.OpenComponent<Pages.MinistryChatsNote>(0);
        builder.AddComponentParameter(1, nameof(Pages.MinistryChatsNote.On), on);
        builder.CloseComponent();
    };
}
