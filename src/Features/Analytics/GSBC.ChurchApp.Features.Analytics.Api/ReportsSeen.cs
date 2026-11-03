using GSBC.ChurchApp.Features.Analytics.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class ReportsSeen(AnalyticsDbContext db, TimeProvider time)
{
    private const int MaximumRequestIdLength = 64;

    public async Task<bool> FirstTimeAsync(string requestId, CancellationToken cancellationToken)
    {
        string id = requestId.Length > MaximumRequestIdLength ? requestId[..MaximumRequestIdLength] : requestId;
        return await db.Database.ExecuteSqlAsync($"""
            INSERT INTO analytics.reports_seen (request_id, seen_at) VALUES ({id}, {time.GetUtcNow().UtcDateTime})
            ON CONFLICT (request_id) DO NOTHING
            """, cancellationToken) == 1;
    }
}
