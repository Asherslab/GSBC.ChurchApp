using GSBC.ChurchApp.Platform.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

[ProtoGrpc(Model = typeof(PlatformContractsModel))]
[ProtoService(typeof(IDestinationsService))]
public sealed partial class DestinationsGrpcClients : ClientFactory;
