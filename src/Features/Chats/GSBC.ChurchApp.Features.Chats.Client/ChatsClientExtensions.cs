using GSBC.ChurchApp.Features.Chats.Client.Components;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.Client;

public static class ChatsClientExtensions
{
    public static IServiceCollection AddChatsOnPhone(this IServiceCollection services) =>
        services
            .AddDestinations(ChatsDestinations.All)
            .AddSingleton<ChatsStore>()
            .AddSingleton(new MinistryPartView(ChatsParts.Key, On))
            .AddSingleton(provider => new ChatAsks(ask => provider.GetRequiredService<ChatsStore>().AskToJoinAsync(ask.MinistryId, ask.PersonId, ask.Place, ask.Text)))
            .AddHubTile<ChatsTile>(ChatsTiles.Chats);

    private static RenderFragment On(MinistryOnPage on) => builder =>
    {
        builder.OpenComponent<MinistryChatsTab>(0);
        builder.AddComponentParameter(1, nameof(MinistryChatsTab.On), on);
        builder.CloseComponent();
    };
}
