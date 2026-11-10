using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class MembershipWatcher(IServiceScopeFactory scopes, FeedChangeBroadcaster broadcaster, ILogger<MembershipWatcher> logger) : BackgroundService
{
    internal static readonly IReadOnlyList<string> Follows = ["access.grants", "ministries", "people.safeguarding", "people.family"];
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Sweep = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FeedChangeBroadcaster.Subscription subscription = broadcaster.Subscribe();
        bool due = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (due)
            {
                await PublishAllAsync(stoppingToken);
                due = false;
            }
            ChangedFeeds? changed = await subscription.WaitAsync(Sweep, stoppingToken);
            due = changed is null || Follows.Any(changed.Includes);
            if (due && changed is not null)
                await subscription.WaitAsync(Settle, stoppingToken);
        }
    }

    private async Task PublishAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ChatPublisher>().PublishAllAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Couldn't bring chat membership up to date; trying again on the next change");
        }
    }
}
