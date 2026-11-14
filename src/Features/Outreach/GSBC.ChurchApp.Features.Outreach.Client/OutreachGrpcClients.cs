using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Outreach.Client;

[ProtoGrpc(Model = typeof(OutreachContractsModel))]
[ProtoService(typeof(IOutreachAdminService))]
public sealed partial class OutreachGrpcClients : ClientFactory;

[ProtoGrpc(Model = typeof(OutreachMapsContractsModel))]
[ProtoService(typeof(IOutreachMapsService))]
public sealed partial class OutreachMapsGrpcClients : ClientFactory;
