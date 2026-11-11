using GSBC.ChurchApp.Features.Prayer.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerSchedule(IServiceScopeFactory scopes, TimeProvider time, ILogger<PrayerSchedule> logger) : BackgroundService
{
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(15);
    private const long AdvisoryLockKey = 0x7072_6179_6572_6b70;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every, time);
        do
        {
            try
            {
                await KeepAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Taking prayer requests off the wall or deleting old ones failed; trying again later");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task KeepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        PrayerDbContext db = scope.ServiceProvider.GetRequiredService<PrayerDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            await scope.ServiceProvider.GetRequiredService<PrayerKeeping>().RunAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }
}
