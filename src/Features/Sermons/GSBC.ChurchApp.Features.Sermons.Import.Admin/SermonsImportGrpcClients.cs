using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Import.Admin;

[ProtoGrpc(Model = typeof(SermonsImportContractsModel))]
[ProtoService(typeof(ISermonImportService))]
public sealed partial class SermonsImportGrpcClients : ClientFactory;
