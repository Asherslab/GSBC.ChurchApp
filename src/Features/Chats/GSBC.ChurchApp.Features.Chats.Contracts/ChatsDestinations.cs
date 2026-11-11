using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsDestinations
{
    public static readonly DestinationPage Chats = new("/chats", "Chats", "message-square", DestinationAreas.Pages, "Chats", Audience.SignedIn, Rank: 42);

    public static readonly DestinationSet All = new([Chats], []);

    public static string Chat(string chatId) => $"/chats/{chatId}";
}
