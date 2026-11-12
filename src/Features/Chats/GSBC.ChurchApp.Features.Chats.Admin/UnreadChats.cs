using GSBC.ChurchApp.Features.Chats.Client;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Chats.Admin;

public sealed class UnreadChats(IChats chats) : INavCounts
{
    public const string Href = "chats";

    public IReadOnlyCollection<string> Feeds => ChatsFeeds.Mine;

    public async Task<IReadOnlyDictionary<string, int>> CountAsync()
    {
        ChatsHome home = await chats.HomeAsync();
        return new Dictionary<string, int> { [Href] = home.Chats.Count(chat => (chat.Unread > 0 && !chat.Card.Muted) || chat.WaitingForYou) };
    }
}
