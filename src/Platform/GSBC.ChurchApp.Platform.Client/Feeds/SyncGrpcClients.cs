using GSBC.ChurchApp.Platform.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

[ProtoGrpc(Model = typeof(PlatformContractsModel))]
[ProtoService(typeof(ISyncService))]
public sealed partial class SyncGrpcClients : ClientFactory;
