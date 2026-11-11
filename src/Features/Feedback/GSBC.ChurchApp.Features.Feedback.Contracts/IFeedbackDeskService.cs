using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

[Service("gsbc.feedback.Desk")]
public interface IFeedbackDeskService
{
    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<DeskList> ListAsync(DeskQuery request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Ticket> TicketAsync(TicketRef request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Done> SetStatusAsync(StatusChange request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Done> AddNoteAsync(NoteSave request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Done> DeleteNoteAsync(NoteRef request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Done> DeleteAsync(TicketRef request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Done> BlockAsync(BlockSave request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<Done> UnblockAsync(PhoneRef request, CallContext context = default);

    [Requires(FeedbackPermissions.TriageNode)]
    ValueTask<BlockedPhones> BlockedAsync(BlockedQuery request, CallContext context = default);
}
