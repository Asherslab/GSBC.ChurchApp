using GSBC.ChurchApp.Platform.Contracts.Content;
using System.Globalization;
using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal static class ChatRows
{
    public const string CardGroup = "chat";
    public const string StatusGroup = "status";
    private const int SnippetLength = 120;

    public static string CardId(Guid person, Guid chat) => $"{person:N}:{chat:N}";

    public static string MessageId(Guid person, Guid message) => $"{person:N}:{message:N}";

    public static string StatusId(Guid person) => $"{person:N}:status";

    public static string SortKey(long seq) => seq.ToString("D12", CultureInfo.InvariantCulture);

    public static FeedChangeRecord Card(Guid person, ChatCard card, DateTime at) => new()
    {
        Feed = ChatsFeeds.Chats, RecordId = CardId(person, Guid.Parse(card.Id)), ScopeId = person.ToString(), Version = at.Ticks,
        GroupKey = CardGroup, SortKey = (card.LastAt ?? card.CreatedAt).ToString("O", CultureInfo.InvariantCulture), Payload = Encode(card),
    };

    public static FeedChangeRecord CardGone(Guid person, Guid chat, DateTime at) => new()
    {
        Feed = ChatsFeeds.Chats, RecordId = CardId(person, chat), ScopeId = person.ToString(), Version = at.Ticks, Deleted = true, GroupKey = CardGroup,
    };

    public static FeedChangeRecord Status(Guid person, ChatsStatus status, DateTime at) => new()
    {
        Feed = ChatsFeeds.Chats, RecordId = StatusId(person), ScopeId = person.ToString(), Version = at.Ticks, GroupKey = StatusGroup,
        Deleted = !status.Paused, Payload = status.Paused ? Encode(status) : [],
    };

    public static FeedChangeRecord Message(Guid person, ChatMessage message, byte[] payload, DateTime at) => new()
    {
        Feed = ChatsFeeds.Messages, RecordId = MessageId(person, Guid.Parse(message.Id)), ScopeId = person.ToString(), Version = at.Ticks,
        GroupKey = message.ChatId, SortKey = SortKey(message.Seq), StartsAt = message.SentAt, Payload = payload,
    };

    public static FeedChangeRecord MessageGone(Guid person, Guid chat, Guid message, DateTime at) => new()
    {
        Feed = ChatsFeeds.Messages, RecordId = MessageId(person, message), ScopeId = person.ToString(), Version = at.Ticks, Deleted = true,
        GroupKey = chat.ToString(),
    };

    public static ChatMessage MessageView(
        MessageRecord message, IReadOnlyDictionary<Guid, string> names, IReadOnlyCollection<ReactionRecord> reactions, MessageRecord? replyTo) => new()
    {
        Id = message.Id.ToString(),
        ChatId = message.ChatId.ToString(),
        Seq = message.Seq,
        Kind = message.Kind,
        SenderId = message.SenderId?.ToString() ?? "",
        SenderName = NameOf(message.SenderId, names),
        ForName = message.ForPersonId is { } child ? NameOf(child, names) : "",
        SentAt = message.SentAt,
        Text = message.DeletedAt is null ? message.Text : "",
        ReplyTo = replyTo is null ? null : new Quote
        {
            MessageId = replyTo.Id.ToString(), SenderName = NameOf(replyTo.SenderId, names),
            Snippet = replyTo.DeletedAt is null ? Snippet(replyTo) : ChatCopy.DeletedMessage,
        },
        Mentions = message.Mentions.Select(person => person.ToString()).ToList(),
        Reactions = message.DeletedAt is not null ? [] : reactions
            .GroupBy(reaction => reaction.Emoji)
            .OrderBy(group => group.Min(reaction => reaction.At))
            .Select(group => new Reaction { Emoji = group.Key, People = group.OrderBy(reaction => reaction.At).Select(reaction => reaction.PersonId.ToString()).ToList() })
            .ToList(),
        Attachments = message.DeletedAt is null ? message.Attachments.Select(ToAttachment).ToList() : [],
        EditedAt = message.EditedAt,
        Deleted = message.DeletedAt is not null,
        ClientId = message.ClientId,
    };

    public static string Snippet(MessageRecord message)
    {
        string text = PlainText.OfChat(message.Text).ReplaceLineEndings(" ").Trim();
        if (text.Length == 0)
            return message.Attachments.Count switch
            {
                0 => "",
                1 => message.Attachments[0].ContentType.StartsWith("image/", StringComparison.Ordinal) ? "Photo" : message.Attachments[0].Name,
                var count => $"{count} files",
            };
        return text.Length <= SnippetLength ? text : text[..SnippetLength].TrimEnd() + "…";
    }

    public static string NameOf(Guid? person, IReadOnlyDictionary<Guid, string> names) =>
        person is { } id ? names.GetValueOrDefault(id, "Someone") : ChatCopy.DeletedAccount;

    public static Attachment ToAttachment(StoredAttachment attachment) => new()
    {
        Key = attachment.Key, Name = attachment.Name, ContentType = attachment.ContentType, Bytes = attachment.Bytes, Width = attachment.Width, Height = attachment.Height,
        MediaId = attachment.MediaId,
        Media = new MediaRef
        {
            Id = attachment.MediaId, Key = attachment.Key, ContentType = attachment.ContentType, Bytes = attachment.Bytes, Width = attachment.Width,
            Height = attachment.Height, FileName = attachment.Name, IsPrivate = true,
        },
    };

    public static byte[] Encode<T>(T value)
    {
        using MemoryStream stream = new();
        ChatsContractsModel.Instance.Serialize(stream, value);
        return stream.ToArray();
    }
}
