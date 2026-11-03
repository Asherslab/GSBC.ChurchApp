using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class DiagnosticsService(DiagnosticsInbox inbox) : IDiagnosticsService
{
    private const string RequestIdHeader = "gsbc-request-id";

    [EnableRateLimiting(RateLimits.Reports)]
    public async ValueTask<Received> ReportAsync(DiagnosticsReport request, CallContext context = default)
    {
        string? requestId = context.RequestHeaders?.GetValue(RequestIdHeader);
        await inbox.AcceptAsync(request, string.IsNullOrWhiteSpace(requestId) ? null : requestId, context.CancellationToken);
        return new Received();
    }
}
