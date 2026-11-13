using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsSchedule(IServiceScopeFactory scopes, TimeProvider time, ILogger<NewsSchedule> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every, time);
        do
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<NewsDesk>().PublishDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Publishing scheduled church news failed; trying again in a minute.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
