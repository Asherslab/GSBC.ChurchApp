using GSBC.ChurchApp.Features.Ministries.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Ministries.Client;

[ProtoGrpc(Model = typeof(MinistriesContractsModel))]
[ProtoService(typeof(IMinistriesService))]
[ProtoService(typeof(IMinistryLeadingService))]
public sealed partial class MinistriesGrpcClients : ClientFactory;
