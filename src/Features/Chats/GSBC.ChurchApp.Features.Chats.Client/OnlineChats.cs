using GSBC.ChurchApp.Platform.Contracts.Content;
using Grpc.Core;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Client.Signals;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Ui.Content;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Chats.Client;

[AdminOnly]
public sealed class OnlineChats(ChannelBase channel, IDirectUploads uploads, ClientSignals signals, TimeProvider time) : IChats
{
    private const long LargestUpload = 512L * 1024 * 1024;
    private readonly IChatsService _chats = channel.CreateGrpcService<IChatsService>();
    private readonly List<PendingSend> _sending = [];
    private readonly HashSet<string> _failed = [];
    private readonly Dictionary<string, long> _markedRead = [];
    private readonly Dictionary<string, string> _drafts = [];

    public bool CanAttach => true;

    public bool UploadsDirectly => true;

    public async Task<ChatsHome> HomeAsync()
    {
        MyChats mine = await _chats.MineAsync(new Nothing());
        return ChatsHome.Of(mine.Chats, mine.Paused, _markedRead);
    }

    public async Task<Conversation?> ChatAsync(string chatId, int most = ChatsStore.MostMessages)
    {
        ChatsHome home = await HomeAsync();
        if (home.Chats.FirstOrDefault(chat => chat.Card.Id == chatId)?.Card is not { } card)
            return null;

        ChatMessages page = await _chats.MessagesAsync(new MessagesRequest { ChatId = chatId, Limit = most });
        List<ChatLine> lines = page.Messages.Select(message => new ChatLine(message, LineState.Sent)).ToList();
        HashSet<string> arrived = PendingLines.Arrived(page.Messages, card);
        _sending.RemoveAll(send => arrived.Contains(send.Message.ClientId));
        foreach (PendingSend send in _sending.Where(send => send.Message.ChatId == chatId).OrderBy(send => send.At))
            lines.Add(new ChatLine(PendingLines.Draft(send, card), _failed.Contains(send.OutboxId) ? LineState.Failed : LineState.Sending, send.OutboxId));
        return new Conversation(card, lines, home.Paused);
    }

    public async Task<MediaRef> UploadAsync(ChatCard card, ChosenFile file)
    {
        await using Stream content = file.File.OpenReadStream(LargestUpload);
        return await uploads.UploadAsync(content, file.File.Name, file.ContentType, file.File.Size,
            new UploadTarget(PrivateTo: ChatsPermissions.MemberNode, PrivateToScope: card.Id, Area: ChatFiles.Area, AreaScopeId: card.Id));
    }

    public ValueTask ForgetAsync(MediaRef media) => ValueTask.CompletedTask;

    public async Task SendAsync(ChatCard card, string text, ChatMessage? replyTo, IReadOnlyCollection<string> mentions, string forPersonId,
        IReadOnlyList<MediaRef> picked)
    {
        List<Attachment> attachments = picked.Select(media => new Attachment
        {
            MediaId = media.Id, Key = media.Key, Name = media.FileName, ContentType = media.ContentType, Bytes = media.Bytes, Width = media.Width,
            Height = media.Height, Media = media,
        }).ToList();
        SendMessage message = new()
        {
            ChatId = card.Id, ClientId = Guid.NewGuid().ToString("N"), Text = text.Trim(), ReplyToId = replyTo?.Id ?? "",
            Mentions = mentions.ToList(), ForPersonId = forPersonId, Attachments = attachments,
        };
        PendingSend send = new()
        {
            OutboxId = message.ClientId, Message = message, At = time.GetUtcNow().UtcDateTime, ReplyName = replyTo?.SenderName ?? "",
            ReplySnippet = replyTo is null ? "" : PlainText.OfChat(replyTo.Text), Waiting = attachments,
        };
        _sending.Add(send);
        await DeliverAsync(send);
    }

    public string DraftOf(string chatId) => _drafts.GetValueOrDefault(chatId, "");

    public void KeepDraft(string chatId, string text)
    {
        if (text.Length == 0)
            _drafts.Remove(chatId);
        else
            _drafts[chatId] = text;
    }

    public async ValueTask RetryAsync(ChatLine line)
    {
        if (_sending.FirstOrDefault(pending => pending.OutboxId == line.OutboxId) is { } send)
            await DeliverAsync(send);
    }

    public Task DiscardAsync(ChatLine line)
    {
        _sending.RemoveAll(send => send.OutboxId == line.OutboxId);
        if (line.OutboxId is { } id)
            _failed.Remove(id);
        signals.ChangedHere(ChatsFeeds.Mine);
        return Task.CompletedTask;
    }

    public async Task EditAsync(ChatMessage message, string text) => await _chats.EditAsync(new EditMessage { MessageId = message.Id, Text = text });

    public async Task DeleteAsync(ChatMessage message) => await _chats.DeleteMessageAsync(new MessageRef { MessageId = message.Id });

    public async Task ReactAsync(ChatMessage message, string emoji, bool on) => await _chats.ReactAsync(new React { MessageId = message.Id, Emoji = emoji, On = on });

    public async Task MuteAsync(ChatCard card, bool muted) => await _chats.MuteAsync(new Mute { ChatId = card.Id, Muted = muted });

    public async Task ConsentAsync(ChatCard card, string childId, bool approve) =>
        await _chats.ConsentAsync(new Consent { ChatId = card.Id, ChildId = childId, Approve = approve });

    public async Task ReportAsync(ChatMessage message, string to, string note) =>
        await _chats.ReportAsync(new ReportMessage { MessageId = message.Id, To = to, Note = note.Trim(), ReportId = Guid.NewGuid().ToString() });

    public async Task StartAsync(StartChat request) => await _chats.StartAsync(request);

    public async Task ChangeAsync(ChatCard card, string name, string audience) =>
        await _chats.ChangeAsync(new ChangeChat { ChatId = card.Id, Name = name, Audience = audience });

    public async Task DeleteChatAsync(ChatCard card) => await _chats.DeleteAsync(new ChatRef { ChatId = card.Id });

    public async Task ChangePeopleAsync(ChatCard card, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove) =>
        await _chats.ChangePeopleAsync(new ChatPeopleChange { ChatId = card.Id, Add = add.ToList(), Remove = remove.ToList() });

    public async Task<string> AskToJoinAsync(string ministryId, string forPersonId, string place, string text)
    {
        string chatId = Guid.NewGuid().ToString();
        await _chats.AskToJoinAsync(new AskToJoin { ChatId = chatId, MinistryId = ministryId, ForPersonId = forPersonId, Place = place, Text = text });
        return chatId;
    }

    public async Task MarkReadAsync(ChatCard card, long upTo)
    {
        if (upTo <= Math.Max(card.ReadSeq, _markedRead.GetValueOrDefault(card.Id)))
            return;
        _markedRead[card.Id] = upTo;
        await _chats.MarkReadAsync(new MarkRead { ChatId = card.Id, UpToSeq = upTo });
    }

    private async Task DeliverAsync(PendingSend send)
    {
        _failed.Remove(send.OutboxId);
        signals.ChangedHere(ChatsFeeds.Mine);
        try
        {
            await _chats.SendAsync(send.Message);
        }
        catch (RpcException)
        {
            _failed.Add(send.OutboxId);
        }
        signals.ChangedHere(ChatsFeeds.Mine);
    }
}
