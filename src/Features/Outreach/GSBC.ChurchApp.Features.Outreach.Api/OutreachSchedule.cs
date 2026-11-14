using GSBC.ChurchApp.Features.Outreach.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachSchedule(IServiceScopeFactory scopes, TimeProvider time, ILogger<OutreachSchedule> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(15);
    private const long AdvisoryLockKey = 0x6f75_7472_6561_6368;

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
                logger.LogError(exception, "Keeping outreach's feeds current or forgetting old records failed; trying again later");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task KeepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        OutreachDbContext db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            await scope.ServiceProvider.GetRequiredService<OutreachKeeping>().RunAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }
}
