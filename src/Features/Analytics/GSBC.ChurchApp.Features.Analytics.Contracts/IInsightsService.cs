using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[Service("gsbc.analytics.Insights")]
public interface IInsightsService
{
    [Requires(AnalyticsPermissions.ViewNode)]
    ValueTask<InsightsSummary> GetSummaryAsync(InsightsQuery request, CallContext context = default);
}
