namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsFeeds
{
    public const string Chats = "chats.chats";
    public const string Messages = "chats.messages";
    public const string Oversight = "chats.oversight";

    public static readonly IReadOnlyList<string> Mine = [Chats, Messages];
}
