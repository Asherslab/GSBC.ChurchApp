using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsDestinations
{
    public static readonly DestinationPage Chats = new("/chats", "Chats", "message-square", DestinationAreas.Pages, "Chats", Audience.SignedIn, Rank: 42);

    private static readonly DestinationArea Area = new("chats", "Chats", "message-square", 42);

    private static readonly DestinationKind OneChat = new("chats.chat", Area, "Chat", "message-square", "chats");

    public static readonly DestinationSet All = new([Chats], [OneChat]);

    public static string Chat(string chatId) => OneChat.PathOf(chatId);
}
