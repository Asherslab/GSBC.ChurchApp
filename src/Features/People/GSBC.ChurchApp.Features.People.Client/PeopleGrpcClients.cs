using GSBC.ChurchApp.Features.People.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.People.Client;

[ProtoGrpc(Model = typeof(PeopleContractsModel))]
[ProtoService(typeof(IPeopleService))]
public sealed partial class PeopleGrpcClients : ClientFactory;
