using GSBC.ChurchApp.Features.Outreach.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Outreach.Client;

[ProtoGrpc(Model = typeof(OutreachContractsModel))]
[ProtoService(typeof(IOutreachAdminService))]
public sealed partial class OutreachGrpcClients : ClientFactory;
