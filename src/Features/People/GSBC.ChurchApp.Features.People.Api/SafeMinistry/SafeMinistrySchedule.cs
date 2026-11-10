using GSBC.ChurchApp.Platform.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal sealed class SafeMinistrySchedule(IServiceScopeFactory scopes, TimeProvider time, ILogger<SafeMinistrySchedule> logger) : BackgroundService
{
    public static readonly TimeOnly NightlyAt = new(2, 0);
    private static readonly TimeSpan MissedAfter = TimeSpan.FromHours(25);

    public static DateTime NextRun(DateTime nowUtc)
    {
        DateTime local = ChurchTime.ToChurch(nowUtc);
        DateTime next = local.Date + NightlyAt.ToTimeSpan();
        return ChurchTime.ToUtc(next > local ? next : next.AddDays(1));
    }

    public static bool Missed(DateTime? lastRunUtc, DateTime nowUtc) => lastRunUtc is not { } last || nowUtc - last > MissedAfter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(onlyIfMissed: true, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(NextRun(time.GetUtcNow().UtcDateTime) - time.GetUtcNow().UtcDateTime, time, stoppingToken);
            await RunAsync(onlyIfMissed: false, stoppingToken);
        }
    }

    private async Task RunAsync(bool onlyIfMissed, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            SafeMinistrySync sync = scope.ServiceProvider.GetRequiredService<SafeMinistrySync>();
            if (!sync.Connected)
                return;
            if (onlyIfMissed && !Missed((await sync.StateAsync(cancellationToken)).LastRunAt, time.GetUtcNow().UtcDateTime))
                return;
            await sync.RunAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Syncing from Safe Ministry Check failed; trying again tomorrow night");
        }
    }
}
