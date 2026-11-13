using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

[Service("gsbc.chats.Chats")]
public interface IChatsService
{
    [SignedIn]
    ValueTask<Done> StartAsync(StartChat request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ChangeAsync(ChangeChat request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> DeleteAsync(ChatRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ChangePeopleAsync(ChatPeopleChange request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> SendAsync(SendMessage request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> EditAsync(EditMessage request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> DeleteMessageAsync(MessageRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ReactAsync(React request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> MarkReadAsync(MarkRead request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> MuteAsync(Mute request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ConsentAsync(Consent request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ReportAsync(ReportMessage request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> AskToJoinAsync(AskToJoin request, CallContext context = default);

    [SignedIn]
    ValueTask<MyChats> MineAsync(Nothing request, CallContext context = default);

    [SignedIn]
    ValueTask<ChatMessages> MessagesAsync(MessagesRequest request, CallContext context = default);
}

[Service("gsbc.chats.Oversight")]
public interface IChatOversightService
{
    [SignedIn]
    ValueTask<Desk> DeskAsync(DeskRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<CheckedChat> CheckAsync(CheckRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Log> LogAsync(LogRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Reports> ReportsAsync(Nothing request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> HandleAsync(HandleReport request, CallContext context = default);

    [Requires(ChatsPermissions.AuditNode)]
    ValueTask<PausedList> PausedAsync(Nothing request, CallContext context = default);

    [Requires(ChatsPermissions.AuditNode)]
    ValueTask<Done> PauseAsync(PauseRequest request, CallContext context = default);
}
