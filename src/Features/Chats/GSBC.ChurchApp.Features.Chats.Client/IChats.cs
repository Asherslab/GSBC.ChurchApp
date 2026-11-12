using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Ui.Content;

namespace GSBC.ChurchApp.Features.Chats.Client;

public interface IChats
{
    bool CanAttach { get; }

    bool UploadsDirectly { get; }

    Task<ChatsHome> HomeAsync();

    Task<Conversation?> ChatAsync(string chatId);

    Task<MediaRef> UploadAsync(ChatCard card, ChosenFile file);

    ValueTask ForgetAsync(MediaRef media);

    Task SendAsync(ChatCard card, string text, ChatMessage? replyTo, IReadOnlyCollection<string> mentions, string forPersonId, IReadOnlyList<MediaRef> picked);

    string DraftOf(string chatId);

    void KeepDraft(string chatId, string text);

    ValueTask RetryAsync(ChatLine line);

    Task DiscardAsync(ChatLine line);

    Task EditAsync(ChatMessage message, string text);

    Task DeleteAsync(ChatMessage message);

    Task ReactAsync(ChatMessage message, string emoji, bool on);

    Task MuteAsync(ChatCard card, bool muted);

    Task ConsentAsync(ChatCard card, string childId, bool approve);

    Task ReportAsync(ChatMessage message, string to, string note);

    Task StartAsync(StartChat request);

    Task ChangeAsync(ChatCard card, string name, string audience);

    Task DeleteChatAsync(ChatCard card);

    Task ChangePeopleAsync(ChatCard card, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove);

    Task<string> AskToJoinAsync(string ministryId, string forPersonId, string place, string text);

    Task MarkReadAsync(ChatCard card, long upTo);
}
