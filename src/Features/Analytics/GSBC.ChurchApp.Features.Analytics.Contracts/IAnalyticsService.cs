using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[Service("gsbc.analytics.Analytics")]
public interface IAnalyticsService
{
    public const string ReportMethod = "/gsbc.analytics.Analytics/Report";

    [Public]
    ValueTask<Received> ReportAsync(UsageReport request, CallContext context = default);
}
