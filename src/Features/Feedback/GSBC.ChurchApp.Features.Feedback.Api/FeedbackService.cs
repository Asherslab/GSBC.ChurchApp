using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Api.Sync;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackService(FeedbackSenders senders, FeedbackSending sending) : IFeedbackService
{
    public async ValueTask<Done> SendAsync(FeedbackSend request, CallContext context = default)
    {
        Sender sender = await senders.WhoAsync(FeedbackCalls.User(context), CallingInstall.Find(context), context.CancellationToken);
        await sending.SendAsync(sender, request, context.CancellationToken);
        return new Done();
    }
}

internal sealed class FeedbackDeskService(FeedbackDesk desk, IAccessEvaluator access) : IFeedbackDeskService
{
    public async ValueTask<DeskList> ListAsync(DeskQuery request, CallContext context = default) => await desk.ListAsync(request, context.CancellationToken);

    public async ValueTask<Ticket> TicketAsync(TicketRef request, CallContext context = default) =>
        await desk.TicketAsync(request,
            await access.IsAllowedAsync(FeedbackCalls.User(context), FeedbackPermissions.PhoneDataNode, null, context.CancellationToken), context.CancellationToken);

    public async ValueTask<Done> SetStatusAsync(StatusChange request, CallContext context = default)
    {
        await desk.SetStatusAsync(FeedbackCalls.Staff(context), request, QueuedRequest.IdOf(context), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> AddNoteAsync(NoteSave request, CallContext context = default)
    {
        await desk.AddNoteAsync(FeedbackCalls.Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteNoteAsync(NoteRef request, CallContext context = default)
    {
        await desk.DeleteNoteAsync(FeedbackCalls.Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteAsync(TicketRef request, CallContext context = default)
    {
        await desk.DeleteAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> BlockAsync(BlockSave request, CallContext context = default)
    {
        await desk.BlockAsync(FeedbackCalls.Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> UnblockAsync(PhoneRef request, CallContext context = default)
    {
        await desk.UnblockAsync(request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<BlockedPhones> BlockedAsync(BlockedQuery request, CallContext context = default) => await desk.BlockedAsync(context.CancellationToken);
}

internal static class FeedbackCalls
{
    public static ClaimsPrincipal User(CallContext context) => context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();

    public static Guid Staff(CallContext context) =>
        Caller.UserId(User(context)) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
}
