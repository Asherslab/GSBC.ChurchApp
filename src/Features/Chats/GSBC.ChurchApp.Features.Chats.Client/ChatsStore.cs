using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.Chats.Client;

public sealed record ChatSummary(ChatCard Card, long Unread)
{
    public bool WaitingForYou => Card.AwaitingYourConsent.Count > 0;
}

public sealed record ChatsHome(IReadOnlyList<ChatSummary> Chats, bool Paused)
{
    public static readonly ChatsHome Empty = new([], false);

    public long Unread => Chats.Where(chat => !chat.Card.Muted).Sum(chat => chat.Unread);

    public int Waiting => Chats.Count(chat => chat.WaitingForYou);
}

public enum LineState
{
    Sent,
    Sending,
    Failed,
}

public sealed record ChatLine(ChatMessage Message, LineState State, string? OutboxId = null);

public sealed record Conversation(ChatCard Card, IReadOnlyList<ChatLine> Lines, bool Paused);

public sealed record OpenedChat(Conversation? Chat);

public sealed class ChatsStore(ILocalFeeds feeds, IOutbox outbox, ILocalPreferences preferences, TimeProvider time)
{
    public const string Remembered = "chats.home";
    private const string Service = "/gsbc.chats.Chats/";
    private const string PendingKey = "chats.pending";
    private const int MostChats = 200;
    private const int MostMessages = 400;
    private readonly Dictionary<string, long> _markedRead = [];
    private readonly Dictionary<string, string> _drafts = [];

    public static IReadOnlyList<string> Feeds => ChatsFeeds.Mine;

    public async Task<ChatsHome> HomeAsync()
    {
        if (!feeds.IsAvailable)
            return ChatsHome.Empty;
        IReadOnlyList<StoredRecord> rows = await feeds.ReadPageAsync(new FeedPageQuery(ChatsFeeds.Chats, Descending: true, Limit: MostChats));
        List<ChatSummary> chats = rows.Where(row => row.GroupKey == "chat")
            .Select(row => Decode<ChatCard>(row.Payload))
            .Select(card => new ChatSummary(card, Math.Max(0, card.LastSeq - Math.Max(card.ReadSeq, _markedRead.GetValueOrDefault(card.Id)))))
            .OrderByDescending(chat => chat.WaitingForYou)
            .ThenByDescending(chat => chat.Card.LastAt ?? chat.Card.CreatedAt)
            .ToList();
        return new ChatsHome(chats, rows.Any(row => row.GroupKey == "status"));
    }

    public async Task<Conversation?> ChatAsync(string chatId)
    {
        if (!feeds.IsAvailable)
            return null;
        ChatsHome home = await HomeAsync();
        if (home.Chats.FirstOrDefault(chat => chat.Card.Id == chatId)?.Card is not { } card)
            return null;

        List<ChatMessage> messages = (await feeds.ReadPageAsync(new FeedPageQuery(ChatsFeeds.Messages, Descending: true, Limit: MostMessages, GroupKey: chatId)))
            .Select(row => Decode<ChatMessage>(row.Payload))
            .OrderBy(message => message.Seq)
            .ToList();
        List<ChatLine> lines = messages.Select(message => new ChatLine(message, LineState.Sent)).ToList();

        PendingSends pending = await PendingAsync();
        HashSet<string> arrived = messages.Where(message => message.SenderId == card.You).Select(message => message.ClientId).ToHashSet();
        if (pending.Sends.RemoveAll(send => arrived.Contains(send.Message.ClientId)) > 0)
            await SaveAsync(pending);
        Dictionary<string, OutboxEntry> entries = (await outbox.ListAsync()).ToDictionary(entry => entry.Id);
        foreach (PendingSend send in pending.Sends.Where(send => send.Message.ChatId == chatId).OrderBy(send => send.At))
        {
            bool failed = entries.GetValueOrDefault(send.OutboxId)?.Failed == true;
            lines.Add(new ChatLine(Draft(send, card), failed ? LineState.Failed : LineState.Sending, send.OutboxId));
        }
        return new Conversation(card, lines, home.Paused);
    }

    public async Task SendAsync(ChatCard card, string text, ChatMessage? replyTo, IReadOnlyCollection<string> mentions, string forPersonId)
    {
        SendMessage message = new()
        {
            ChatId = card.Id, ClientId = Guid.NewGuid().ToString("N"), Text = text.Trim(), ReplyToId = replyTo?.Id ?? "",
            Mentions = mentions.ToList(), ForPersonId = forPersonId,
        };
        string id = await outbox.EnqueueAsync(Service + "Send", message, ChatsContractsModel.Instance);
        PendingSends pending = await PendingAsync();
        pending.Sends.Add(new PendingSend
        {
            OutboxId = id, Message = message, At = time.GetUtcNow().UtcDateTime, ReplyName = replyTo?.SenderName ?? "", ReplySnippet = replyTo?.Text ?? "",
        });
        await SaveAsync(pending);
    }

    public string DraftOf(string chatId) => _drafts.GetValueOrDefault(chatId, "");

    public void KeepDraft(string chatId, string text)
    {
        if (text.Length == 0)
            _drafts.Remove(chatId);
        else
            _drafts[chatId] = text;
    }

    public ValueTask RetryAsync(ChatLine line) => line.OutboxId is { } id ? outbox.RetryAsync(id) : ValueTask.CompletedTask;

    public async Task DiscardAsync(ChatLine line)
    {
        if (line.OutboxId is not { } id)
            return;
        await outbox.DiscardAsync(id);
        PendingSends pending = await PendingAsync();
        pending.Sends.RemoveAll(send => send.OutboxId == id);
        await SaveAsync(pending);
    }

    public Task EditAsync(ChatMessage message, string text) => EnqueueAsync("Edit", new EditMessage { MessageId = message.Id, Text = text });

    public Task DeleteAsync(ChatMessage message) => EnqueueAsync("DeleteMessage", new MessageRef { MessageId = message.Id });

    public Task ReactAsync(ChatMessage message, string emoji, bool on) => EnqueueAsync("React", new React { MessageId = message.Id, Emoji = emoji, On = on });

    public Task MuteAsync(ChatCard card, bool muted) => EnqueueAsync("Mute", new Mute { ChatId = card.Id, Muted = muted });

    public Task ConsentAsync(ChatCard card, string childId, bool approve) =>
        EnqueueAsync("Consent", new Consent { ChatId = card.Id, ChildId = childId, Approve = approve });

    public Task ReportAsync(ChatMessage message, string to, string note) =>
        EnqueueAsync("Report", new ReportMessage { MessageId = message.Id, To = to, Note = note.Trim(), ReportId = Guid.NewGuid().ToString() });

    public Task StartAsync(StartChat request) => EnqueueAsync("Start", request);

    public Task ChangeAsync(ChatCard card, string name, string audience) => EnqueueAsync("Change", new ChangeChat { ChatId = card.Id, Name = name, Audience = audience });

    public Task DeleteChatAsync(ChatCard card) => EnqueueAsync("Delete", new ChatRef { ChatId = card.Id });

    public Task ChangePeopleAsync(ChatCard card, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove) =>
        EnqueueAsync("ChangePeople", new ChatPeopleChange { ChatId = card.Id, Add = add.ToList(), Remove = remove.ToList() });

    public async Task<string> AskToJoinAsync(string ministryId, string forPersonId, string text)
    {
        string chatId = Guid.NewGuid().ToString();
        await EnqueueAsync("AskToJoin", new AskToJoin { ChatId = chatId, MinistryId = ministryId, ForPersonId = forPersonId, Text = text });
        return chatId;
    }

    public async Task MarkReadAsync(ChatCard card, long upTo)
    {
        if (upTo <= Math.Max(card.ReadSeq, _markedRead.GetValueOrDefault(card.Id)))
            return;
        _markedRead[card.Id] = upTo;
        await EnqueueAsync("MarkRead", new MarkRead { ChatId = card.Id, UpToSeq = upTo });
    }

    private async Task EnqueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request) where T : class =>
        await outbox.EnqueueAsync(Service + method, request, ChatsContractsModel.Instance);

    private static ChatMessage Draft(PendingSend send, ChatCard card) => new()
    {
        Id = send.Message.ClientId, ChatId = send.Message.ChatId, Kind = MessageKinds.Text, SenderId = card.You, SenderName = "You",
        ForName = card.ForChildren.FirstOrDefault(child => child.Id == send.Message.ForPersonId)?.Name ?? "", SentAt = send.At,
        Text = send.Message.Text, ClientId = send.Message.ClientId, Mentions = send.Message.Mentions,
        ReplyTo = send.Message.ReplyToId.Length == 0 ? null : new Quote { MessageId = send.Message.ReplyToId, SenderName = send.ReplyName, Snippet = send.ReplySnippet },
    };

    private async Task<PendingSends> PendingAsync() =>
        await preferences.GetAsync(PendingKey) is { Length: > 0 } stored
            ? Decode<PendingSends>(Convert.FromBase64String(stored))
            : new PendingSends();

    private async Task SaveAsync(PendingSends pending)
    {
        using MemoryStream stream = new();
        ChatsContractsModel.Instance.Serialize(stream, pending);
        await preferences.SetAsync(PendingKey, pending.Sends.Count == 0 ? null : Convert.ToBase64String(stream.ToArray()));
    }

    private static T Decode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(byte[] payload) => ChatsContractsModel.Instance.Deserialize<T>(new MemoryStream(payload));
}
