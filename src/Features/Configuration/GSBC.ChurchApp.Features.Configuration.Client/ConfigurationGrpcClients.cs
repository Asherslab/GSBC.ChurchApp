using GSBC.ChurchApp.Features.Configuration.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

[ProtoGrpc(Model = typeof(ConfigurationContractsModel))]
[ProtoService(typeof(IConfigurationService))]
[ProtoService(typeof(IDraftsService))]
public sealed partial class ConfigurationGrpcClients : ClientFactory;
