using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

[Service("gsbc.outreach.Outreach")]
public interface IOutreachService
{
    [AppInstall]
    ValueTask<Done> RecordWalkAsync(WalkPiece request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> NoJunkMailAsync(NoJunkMail request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> UndoNoJunkMailAsync(MarkRef request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> DoorAsync(DoorOutcome request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> ReportAsync(ReportSend request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> ClaimAsync(ClaimSend request, CallContext context = default);

    [AppInstall]
    ValueTask<Done> UnclaimAsync(MarkRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> CountingAsync(CountingSave request, CallContext context = default);
}
