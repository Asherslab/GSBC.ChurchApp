using GSBC.ChurchApp.Platform.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Client.Locations;

[ProtoGrpc(Model = typeof(PlatformContractsModel))]
[ProtoService(typeof(ILocationsService))]
public sealed partial class LocationsGrpcClients : ClientFactory;
