using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class DiagnosticsService(DiagnosticsInbox inbox) : IDiagnosticsService
{
    [EnableRateLimiting(RateLimits.Reports)]
    public async ValueTask<Received> ReportAsync(DiagnosticsReport request, CallContext context = default)
    {
        await inbox.AcceptAsync(request, RequestIds.Of(context), context.CancellationToken);
        return new Received();
    }
}
