using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

[Service("gsbc.feedback.Feedback")]
public interface IFeedbackService
{
    [AppInstallOrSignedIn]
    ValueTask<Done> SendAsync(FeedbackSend request, CallContext context = default);
}
