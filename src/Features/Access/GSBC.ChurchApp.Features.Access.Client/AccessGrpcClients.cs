using GSBC.ChurchApp.Features.Access.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Access.Client;

[ProtoGrpc(Model = typeof(AccessContractsModel))]
[ProtoService(typeof(IAccessService))]
public sealed partial class AccessGrpcClients : ClientFactory;
