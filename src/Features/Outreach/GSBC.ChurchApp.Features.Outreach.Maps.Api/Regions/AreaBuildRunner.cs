using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed class AreaBuildRunner(IServiceScopeFactory scopes, TimeProvider time, ILogger<AreaBuildRunner> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(5);
    private const long AdvisoryLockKey = 0x6773_6263_6f6d_6170;
    private readonly SemaphoreSlim _wake = new(0, 1);

    public void Wake()
    {
        if (_wake.CurrentCount == 0)
            _wake.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MapAreaSeeding.SeedAsync(scopes, stoppingToken);
                while (await RunNextAsync(stoppingToken))
                {
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Building outreach maps failed; trying again shortly");
            }
            await _wake.WaitAsync(Every, stoppingToken);
        }
    }

    private async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        if (!await db.Areas.AnyAsync(area => area.State == MapAreaStates.Waiting || area.State == MapAreaStates.Building, cancellationToken))
            return false;
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return false;
        try
        {
            MapAreaRecord? area = await db.Areas
                .Where(candidate => candidate.State == MapAreaStates.Waiting || candidate.State == MapAreaStates.Building)
                .OrderBy(candidate => candidate.State == MapAreaStates.Building ? 0 : 1).ThenBy(candidate => candidate.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (area is null)
                return false;
            (area.State, area.StartedAt, area.Problem) = (MapAreaStates.Building, time.GetUtcNow().UtcDateTime, null);
            await db.SaveChangesAsync(cancellationToken);
            try
            {
                await scope.ServiceProvider.GetRequiredService<AreaBuilder>().BuildAsync(area, cancellationToken);
                logger.LogInformation("Built outreach map area {Area}: {Report}", area.Id, area.Report);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Building outreach map area {Area} failed", area.Id);
                db.ChangeTracker.Clear();
                await db.Areas.Where(failed => failed.Id == area.Id).ExecuteUpdateAsync(set => set
                    .SetProperty(failed => failed.State, MapAreaStates.Failed)
                    .SetProperty(failed => failed.Problem, $"{area.Stage}: {exception.Message}")
                    .SetProperty(failed => failed.Stage, (string?)null), cancellationToken);
            }
            return true;
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }
}
