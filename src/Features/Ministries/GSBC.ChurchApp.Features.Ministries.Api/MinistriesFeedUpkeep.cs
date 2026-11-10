using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistriesFeedUpkeep(
    FeedChangeBroadcaster broadcaster, IServiceScopeFactory scopes, TimeProvider time, ILogger<MinistriesFeedUpkeep> logger) : BackgroundService
{
    private static readonly TimeSpan Sweep = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Gather = TimeSpan.FromMilliseconds(200);

    private static readonly IReadOnlySet<string> Inputs = new HashSet<string>
    {
        MinistriesFeeds.Ministries, "access.grants", "events.calendars", "events.calendars.private", "accounts.people", "people.mine", "people.family",
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FeedChangeBroadcaster.Subscription changes = broadcaster.Subscribe();
        await PublishAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ChangedFeeds? changed = await changes.WaitAsync(Sweep, stoppingToken);
            if (changed is not null && !changed.All && !changed.Names.Any(Inputs.Contains))
                continue;
            await Task.Delay(Gather, time, stoppingToken);
            await PublishAsync(stoppingToken);
        }
    }

    private async Task PublishAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Couldn't bring the ministries feeds up to date; trying again shortly");
        }
    }
}
