using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class DraftsService(DraftEditor drafts) : IDraftsService
{
    public async ValueTask<DraftList> ListAsync(ListDraftsRequest request, CallContext context = default) =>
        await drafts.ListAsync(CallerOf(context), context.CancellationToken);

    public async ValueTask<Draft> GetAsync(DraftQuery request, CallContext context = default) =>
        await drafts.GetAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> CreateAsync(NewDraftRequest request, CallContext context = default) =>
        await drafts.CreateAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> RenameAsync(RenameDraftRequest request, CallContext context = default) =>
        await drafts.RenameAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> SaveAsync(SaveDraftRequest request, CallContext context = default) =>
        await drafts.SaveAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> SettleAsync(SettleRequest request, CallContext context = default) =>
        await drafts.SettleAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> AskToPublishAsync(DraftQuery request, CallContext context = default) =>
        await drafts.AskToPublishAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<PublishReply> PublishAsync(DraftQuery request, CallContext context = default) =>
        await drafts.PublishAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> TurnOnAsync(EventLayerRequest request, CallContext context = default) =>
        await drafts.TurnOnAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<DraftsDone> TurnOffAsync(DraftQuery request, CallContext context = default) =>
        await drafts.TurnOffAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<Draft> ShareAsync(ShareRequest request, CallContext context = default) =>
        await drafts.ShareAsync(CallerOf(context), request, sharing: true, context.CancellationToken);

    public async ValueTask<Draft> UnshareAsync(ShareRequest request, CallContext context = default) =>
        await drafts.ShareAsync(CallerOf(context), request, sharing: false, context.CancellationToken);

    public async ValueTask<DraftsDone> DeleteAsync(DraftQuery request, CallContext context = default) =>
        await drafts.DeleteAsync(CallerOf(context), request, context.CancellationToken);

    private static ClaimsPrincipal CallerOf(CallContext context) => context.ServerCallContext!.GetHttpContext().User;
}
