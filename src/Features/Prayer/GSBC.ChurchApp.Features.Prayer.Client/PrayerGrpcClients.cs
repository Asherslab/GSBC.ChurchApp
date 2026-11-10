using GSBC.ChurchApp.Features.Prayer.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Prayer.Client;

[ProtoGrpc(Model = typeof(PrayerContractsModel))]
[ProtoService(typeof(IPastoralService))]
public sealed partial class PrayerGrpcClients : ClientFactory;
