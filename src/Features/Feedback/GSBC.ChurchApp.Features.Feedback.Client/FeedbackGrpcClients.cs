using GSBC.ChurchApp.Features.Feedback.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Feedback.Client;

[ProtoGrpc(Model = typeof(FeedbackContractsModel))]
[ProtoService(typeof(IFeedbackDeskService))]
public sealed partial class FeedbackGrpcClients : ClientFactory;
