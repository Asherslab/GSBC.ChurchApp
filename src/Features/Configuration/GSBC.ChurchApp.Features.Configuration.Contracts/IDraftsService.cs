using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

[Service("gsbc.config.Drafts")]
public interface IDraftsService
{
    [SignedIn]
    ValueTask<DraftList> ListAsync(ListDraftsRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> GetAsync(DraftQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> CreateAsync(NewDraftRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> RenameAsync(RenameDraftRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> SaveAsync(SaveDraftRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> SettleAsync(SettleRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> AskToPublishAsync(DraftQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<PublishReply> PublishAsync(DraftQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> SchedulePublishAsync(SchedulePublishRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> CancelScheduledPublishAsync(DraftQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> TurnOnAsync(EventLayerRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<DraftsDone> TurnOffAsync(DraftQuery request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> ShareAsync(ShareRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Draft> UnshareAsync(ShareRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<DraftsDone> DeleteAsync(DraftQuery request, CallContext context = default);
}
