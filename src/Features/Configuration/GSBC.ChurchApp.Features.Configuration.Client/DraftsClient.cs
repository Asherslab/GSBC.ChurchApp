using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Configuration.Client;

[AdminOnly]
public sealed class DraftsClient(ChannelBase channel)
{
    private readonly IDraftsService _drafts = channel.CreateGrpcService<IDraftsService>();

    public ValueTask<DraftList> ListAsync() => _drafts.ListAsync(new ListDraftsRequest());

    public ValueTask<Draft> GetAsync(string draftId) => _drafts.GetAsync(new DraftQuery { DraftId = draftId });

    public ValueTask<Draft> CreateAsync(string name, string? copyOf = null) => _drafts.CreateAsync(new NewDraftRequest { Name = name, CopyOf = copyOf });

    public ValueTask<Draft> RenameAsync(string draftId, string name) => _drafts.RenameAsync(new RenameDraftRequest { DraftId = draftId, Name = name });

    public ValueTask<Draft> SaveAsync(string draftId, IEnumerable<PatchMessage> set, IEnumerable<string> cleared) =>
        _drafts.SaveAsync(new SaveDraftRequest { DraftId = draftId, Set = [.. set], Cleared = [.. cleared] });

    public ValueTask<Draft> SettleAsync(string draftId, string placementId, bool keepMine) =>
        _drafts.SettleAsync(new SettleRequest { DraftId = draftId, PlacementId = placementId, KeepMine = keepMine });

    public ValueTask<Draft> AskToPublishAsync(string draftId) => _drafts.AskToPublishAsync(new DraftQuery { DraftId = draftId });

    public ValueTask<PublishReply> PublishAsync(string draftId) => _drafts.PublishAsync(new DraftQuery { DraftId = draftId });

    public ValueTask<Draft> SchedulePublishAsync(string draftId, DateTime at) =>
        _drafts.SchedulePublishAsync(new SchedulePublishRequest { DraftId = draftId, At = at });

    public ValueTask<Draft> CancelScheduledPublishAsync(string draftId) => _drafts.CancelScheduledPublishAsync(new DraftQuery { DraftId = draftId });

    public ValueTask<Draft> TurnOnAsync(string draftId, DateTime? startsAt, DateTime? endsAt) =>
        _drafts.TurnOnAsync(new EventLayerRequest { DraftId = draftId, StartsAt = startsAt, EndsAt = endsAt });

    public ValueTask<DraftsDone> TurnOffAsync(string draftId) => _drafts.TurnOffAsync(new DraftQuery { DraftId = draftId });

    public ValueTask<Draft> ShareAsync(string draftId, string userId) => _drafts.ShareAsync(new ShareRequest { DraftId = draftId, UserId = userId });

    public ValueTask<Draft> UnshareAsync(string draftId, string userId) => _drafts.UnshareAsync(new ShareRequest { DraftId = draftId, UserId = userId });

    public ValueTask<DraftsDone> DeleteAsync(string draftId) => _drafts.DeleteAsync(new DraftQuery { DraftId = draftId });
}
