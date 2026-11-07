using GSBC.ChurchApp.Features.Events.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class ICalSchedule(IServiceScopeFactory scopes, ILogger<ICalSchedule> logger) : BackgroundService
{
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(30);
    private const long AdvisoryLockKey = 0x6576_6e74_6963_616c;
    private readonly SemaphoreSlim _soon = new(0);

    public void CopySoon() => _soon.Release();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CopyAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Copying calendars from their iCal addresses failed; trying again later");
            }
            await _soon.WaitAsync(Every, stoppingToken);
        }
    }

    private async Task CopyAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            await scope.ServiceProvider.GetRequiredService<ICalCopier>().CopyAllAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }
}
