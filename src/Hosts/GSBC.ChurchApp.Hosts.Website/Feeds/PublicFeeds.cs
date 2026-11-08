using GSBC.ChurchApp.Platform.Contracts.Sync;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GSBC.ChurchApp.Hosts.Website.Feeds;

public sealed class PublicFeeds : IHealthCheck
{
    private readonly Lock _lock = new();
    private readonly FeedMirror _mirror = new(WebsiteData.Feeds);
    private volatile WebsiteData _current = WebsiteData.Empty;
    private volatile bool _read;

    public WebsiteData Current => _current;

    public bool HasRead => _read;

    public WatchRequest NextRequest()
    {
        lock (_lock)
            return new WatchRequest { Cursors = _mirror.Cursors, Origin = _mirror.Origin };
    }

    public void Apply(WatchEvent watched)
    {
        if (watched.IsHeartbeat)
            return;

        lock (_lock)
        {
            if (_mirror.Apply(watched.Origin, watched.Feeds) || !_read)
                _current = WebsiteData.From(_mirror);
            if (watched.Feeds.All(batch => !batch.HasMore))
                _read = true;
        }
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(_read ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("The public feeds haven't been read yet."));
}
