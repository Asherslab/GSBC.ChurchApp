using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatsService(ChatActions actions) : IChatsService
{
    private static readonly Done Done = new();

    public async ValueTask<Done> StartAsync(StartChat request, CallContext context = default) =>
        await Run(context, (me, token) => actions.StartAsync(me, request, token));

    public async ValueTask<Done> ChangeAsync(ChangeChat request, CallContext context = default) =>
        await Run(context, (me, token) => actions.ChangeAsync(me, request, token));

    public async ValueTask<Done> DeleteAsync(ChatRef request, CallContext context = default) =>
        await Run(context, (me, token) => actions.DeleteAsync(me, request, token));

    public async ValueTask<Done> ChangePeopleAsync(ChatPeopleChange request, CallContext context = default) =>
        await Run(context, (me, token) => actions.ChangePeopleAsync(me, request, token));

    public async ValueTask<Done> SendAsync(SendMessage request, CallContext context = default) =>
        await Run(context, (me, token) => actions.SendAsync(me, request, token));

    public async ValueTask<Done> EditAsync(EditMessage request, CallContext context = default) =>
        await Run(context, (me, token) => actions.EditAsync(me, request, token));

    public async ValueTask<Done> DeleteMessageAsync(MessageRef request, CallContext context = default) =>
        await Run(context, (me, token) => actions.DeleteMessageAsync(me, request, token));

    public async ValueTask<Done> ReactAsync(React request, CallContext context = default) =>
        await Run(context, (me, token) => actions.ReactAsync(me, request, token));

    public async ValueTask<Done> MarkReadAsync(MarkRead request, CallContext context = default) =>
        await Run(context, (me, token) => actions.MarkReadAsync(me, request, token));

    public async ValueTask<Done> MuteAsync(Mute request, CallContext context = default) =>
        await Run(context, (me, token) => actions.MuteAsync(me, request, token));

    public async ValueTask<Done> ConsentAsync(Consent request, CallContext context = default) =>
        await Run(context, (me, token) => actions.ConsentAsync(me, request, token));

    public async ValueTask<Done> ReportAsync(ReportMessage request, CallContext context = default) =>
        await Run(context, (me, token) => actions.ReportAsync(me, request, token));

    public async ValueTask<Done> AskToJoinAsync(AskToJoin request, CallContext context = default) =>
        await Run(context, (me, token) => actions.AskToJoinAsync(me, request, token));

    private static async Task<Done> Run(CallContext context, Func<Guid, CancellationToken, Task> action)
    {
        await action(SignedIn(context), context.CancellationToken);
        return Done;
    }

    internal static Guid SignedIn(CallContext context)
    {
        ClaimsPrincipal user = context.ServerCallContext!.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && Caller.UserId(user) is { } id
            ? id
            : throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));
    }
}

internal sealed class ChatOversightService(ChatOversight oversight) : IChatOversightService
{
    private static readonly Done Done = new();

    public async ValueTask<Desk> DeskAsync(DeskRequest request, CallContext context = default) =>
        await oversight.DeskAsync(ChatsService.SignedIn(context), context.CancellationToken);

    public async ValueTask<CheckedChat> CheckAsync(CheckRequest request, CallContext context = default) =>
        await oversight.CheckAsync(ChatsService.SignedIn(context), request, context.CancellationToken);

    public async ValueTask<Log> LogAsync(LogRequest request, CallContext context = default) =>
        await oversight.LogAsync(ChatsService.SignedIn(context), request, context.CancellationToken);

    public async ValueTask<Reports> ReportsAsync(Nothing request, CallContext context = default) =>
        await oversight.ReportsAsync(ChatsService.SignedIn(context), context.CancellationToken);

    public async ValueTask<Done> HandleAsync(HandleReport request, CallContext context = default)
    {
        await oversight.HandleAsync(ChatsService.SignedIn(context), request, context.CancellationToken);
        return Done;
    }

    public async ValueTask<PausedList> PausedAsync(Nothing request, CallContext context = default) =>
        await oversight.PausedAsync(context.CancellationToken);

    public async ValueTask<Done> PauseAsync(PauseRequest request, CallContext context = default)
    {
        await oversight.PauseAsync(ChatsService.SignedIn(context), request, context.CancellationToken);
        return Done;
    }
}
