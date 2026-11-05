using GSBC.ChurchApp.Features.Sermons.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Client;

[ProtoGrpc(Model = typeof(SermonsContractsModel))]
[ProtoService(typeof(ISermonsService))]
[ProtoService(typeof(ISermonLibraryService))]
public sealed partial class SermonsGrpcClients : ClientFactory;
