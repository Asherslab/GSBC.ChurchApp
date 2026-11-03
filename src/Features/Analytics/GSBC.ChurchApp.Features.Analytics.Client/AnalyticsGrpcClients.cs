using GSBC.ChurchApp.Features.Analytics.Contracts;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Client;

[ProtoGrpc(Model = typeof(AnalyticsContractsModel))]
[ProtoService(typeof(IDiagnosticsService))]
[ProtoService(typeof(IHealthService))]
[ProtoService(typeof(IAnalyticsService))]
[ProtoService(typeof(IInsightsService))]
public sealed partial class AnalyticsGrpcClients : ClientFactory;
