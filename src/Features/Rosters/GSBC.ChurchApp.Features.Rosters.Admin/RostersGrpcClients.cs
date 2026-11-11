using GSBC.ChurchApp.Features.Rosters.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Rosters.Admin;

[ProtoGrpc(Model = typeof(RostersContractsModel))]
[ProtoService(typeof(IRostersService))]
public sealed partial class RostersGrpcClients : ClientFactory;
