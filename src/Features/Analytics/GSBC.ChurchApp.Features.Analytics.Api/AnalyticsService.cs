using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class AnalyticsService(UsageInbox inbox) : IAnalyticsService
{
    [EnableRateLimiting(RateLimits.Reports)]
    public async ValueTask<Received> ReportAsync(UsageReport request, CallContext context = default)
    {
        await inbox.AcceptAsync(request, RequestIds.Of(context), context.CancellationToken);
        return new Received();
    }
}
