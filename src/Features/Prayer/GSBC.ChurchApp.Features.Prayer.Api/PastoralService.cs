using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PastoralService(PastoralCare care, IAccessEvaluator access) : IPastoralService
{
    public async ValueTask<Inbox> InboxAsync(InboxRequest request, CallContext context = default) =>
        await care.InboxAsync(await MayReadAsync(context), request.Search, context.CancellationToken);

    public async ValueTask<RequestDetails> RequestAsync(RequestRef request, CallContext context = default) =>
        await care.RequestAsync(await MayReadAsync(context), request.Id, context.CancellationToken);

    public async ValueTask<Done> CareAsync(CareChange request, CallContext context = default)
    {
        await care.CareAsync(Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> AddNoteAsync(NoteSave request, CallContext context = default)
    {
        await care.AddNoteAsync(Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteNoteAsync(NoteRef request, CallContext context = default)
    {
        await care.DeleteNoteAsync(Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ShowOnWallAsync(WallSave request, CallContext context = default)
    {
        await care.ShowOnWallAsync(Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> KeepPrivateAsync(RequestRef request, CallContext context = default)
    {
        await care.KeepPrivateAsync(Staff(context), request, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteAsync(RequestRef request, CallContext context = default)
    {
        await care.DeleteAsync(request, context.CancellationToken);
        return new Done();
    }

    private async ValueTask<bool> MayReadAsync(CallContext context) =>
        await access.IsAllowedAsync(User(context), PrayerPermissions.ReadNode, null, context.CancellationToken);

    private static Guid Staff(CallContext context) =>
        Caller.UserId(User(context)) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));

    private static ClaimsPrincipal User(CallContext context) => context.ServerCallContext!.GetHttpContext().User;
}
