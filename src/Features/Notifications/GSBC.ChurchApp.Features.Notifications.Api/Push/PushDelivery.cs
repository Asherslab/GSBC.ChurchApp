using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Notifications.Api.Push;

internal sealed class PushDelivery(
    IServiceScopeFactory scopes, PushSignal signal, IOptions<ApnsOptions> apns, TimeProvider time, ILogger<PushDelivery> logger) : BackgroundService
{
    private static readonly TimeSpan Idle = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!apns.Value.IsConfigured)
            logger.LogWarning("Push is off: no APNs key configured (Notifications:Apns). The inbox still works.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                PushDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<PushDispatcher>();
                while (await dispatcher.DeliverDueAsync(time.GetUtcNow().UtcDateTime, stoppingToken) > 0)
                {
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Sending pushes failed; trying again shortly");
            }

            await signal.WaitAsync(Idle, stoppingToken);
        }
    }
}
