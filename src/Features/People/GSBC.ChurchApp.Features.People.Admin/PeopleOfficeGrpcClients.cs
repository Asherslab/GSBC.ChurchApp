using GSBC.ChurchApp.Features.People.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.People.Admin;

[ProtoGrpc(Model = typeof(PeopleContractsModel))]
[ProtoService(typeof(IPeopleService))]
[ProtoService(typeof(ISafeMinistryCheckService))]
public sealed partial class PeopleOfficeGrpcClients : ClientFactory;
