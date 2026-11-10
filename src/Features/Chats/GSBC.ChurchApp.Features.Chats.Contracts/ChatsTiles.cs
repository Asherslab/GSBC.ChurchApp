using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsTiles
{
    public static readonly HubTileKind Chats = new("hub.chats", "Chats",
        "Your ministry chats, with the newest unread message", "Chats",
        new TileDefaults("Chats", "Group chats and channels from your ministries", Tone: TileTones.LiveSoft, Icon: "message-square", Link: ChatsDestinations.Chats.Path,
            TitleLabel: "Title when nothing is unread"));

    public static SurfaceSet Surfaces => new([Chats.Surface]);
}
