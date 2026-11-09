using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class ScheduledPublishing(IServiceScopeFactory scopes, TimeProvider time, ILogger<ScheduledPublishing> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every, time);
        do
        {
            try
            {
                await PublishDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Publishing scheduled drafts failed; trying again shortly");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublishDueAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        foreach (DraftEditor.ScheduledOutcome outcome in await scope.ServiceProvider.GetRequiredService<DraftEditor>().PublishDueAsync(cancellationToken))
        {
            if (outcome.Published)
                logger.LogInformation("Published scheduled draft {DraftId} ({Name})", outcome.DraftId, outcome.Name);
            else if (outcome.NotPublished is { } reason)
                logger.LogWarning("Scheduled draft {DraftId} ({Name}) didn't publish: {Reason}", outcome.DraftId, outcome.Name, reason);
        }
    }
}
