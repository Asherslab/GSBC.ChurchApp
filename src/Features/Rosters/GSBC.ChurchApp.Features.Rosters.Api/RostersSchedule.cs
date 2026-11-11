using GSBC.ChurchApp.Features.Rosters.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RostersSchedule(IServiceScopeFactory scopes, TimeProvider time, ILogger<RostersSchedule> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);
    private const long ScheduleLockKey = 0x7273_7472_7363_6864;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval, time);
        do
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Refreshing the rosters and sending their reminders failed; trying again next time");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        RostersDbContext db = scope.ServiceProvider.GetRequiredService<RostersDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({ScheduleLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            await scope.ServiceProvider.GetRequiredService<RosterRefresh>().RefreshEventsAsync(cancellationToken);
            await scope.ServiceProvider.GetRequiredService<RosterPublisher>().PublishAllAsync(cancellationToken);
            await scope.ServiceProvider.GetRequiredService<RosterReminders>().SendAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({ScheduleLockKey})", CancellationToken.None);
        }
    }
}
