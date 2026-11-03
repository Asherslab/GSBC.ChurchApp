using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[Service("gsbc.analytics.Health")]
public interface IHealthService
{
    [Requires(AnalyticsPermissions.HealthViewNode)]
    ValueTask<HealthOverview> GetOverviewAsync(HealthQuery request, CallContext context = default);

    [Requires(AnalyticsPermissions.HealthViewNode)]
    ValueTask<ErrorKindDetail> GetErrorKindAsync(ErrorKindRequest request, CallContext context = default);
}
