using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[Service("gsbc.analytics.Diagnostics")]
public interface IDiagnosticsService
{
    public const string ReportMethod = "/gsbc.analytics.Diagnostics/Report";

    [Public]
    ValueTask<Received> ReportAsync(DiagnosticsReport request, CallContext context = default);
}
