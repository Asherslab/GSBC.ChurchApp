using GSBC.ChurchApp.Platform.Contracts.Sync;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Hosts.Website.Feeds;

public sealed class FeedWatcher(ISyncService sync, PublicFeeds feeds, TimeProvider time, ILogger<FeedWatcher> log) : BackgroundService
{
    private static readonly TimeSpan FirstWait = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan LongestWait = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan wait = FirstWait;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (WatchEvent watched in sync.WatchAsync(feeds.NextRequest(), new CallContext(new CallOptions(cancellationToken: stoppingToken))))
                {
                    feeds.Apply(watched);
                    wait = FirstWait;
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                log.LogWarning(exception, "The public feeds' stream ended; trying again in {Wait}", wait);
            }

            if (stoppingToken.IsCancellationRequested)
                return;
            await Task.Delay(wait, time, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
            wait = TimeSpan.FromTicks(Math.Min(wait.Ticks * 2, LongestWait.Ticks));
        }
    }
}
