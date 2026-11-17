using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.National;

internal sealed class NationalAddressRunner(IServiceScopeFactory scopes, TimeProvider time, IOptions<MapSourcesOptions> options, ILogger<NationalAddressRunner> logger)
    : BackgroundService
{
    private const long AdvisoryLockKey = 0x6773_6263_676e_6166;
    private static readonly TimeSpan Every = TimeSpan.FromHours(1);
    private static readonly TimeSpan CheckedEvery = TimeSpan.FromDays(1);
    private static readonly TimeSpan Retry = TimeSpan.FromHours(6);
    private DateTimeOffset _importTriedAt = DateTimeOffset.MinValue;
    private DateTimeOffset _sweptAt = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.Value.ImportNationalAddresses)
                await LockedAsync(ImportAsync, stoppingToken);
            await LockedAsync(SweepAsync, stoppingToken);
            await Task.Delay(Every, time, stoppingToken);
        }
    }

    private async Task ImportAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        if (time.GetUtcNow() - _importTriedAt < CheckedEvery)
            return;
        _importTriedAt = time.GetUtcNow();
        GnafImport import = services.GetRequiredService<GnafImport>();
        if (!await import.DueAsync(cancellationToken))
            return;
        logger.LogInformation("Importing the national address file (G-NAF) for finding places");
        await import.ImportLatestAsync(cancellationToken);
        logger.LogInformation("Imported the national address file (G-NAF)");
    }

    private async Task SweepAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        if (time.GetUtcNow() - _sweptAt < CheckedEvery)
            return;
        _sweptAt = time.GetUtcNow();
        if (await services.GetRequiredService<PlaceMaps>().SweepAsync(cancellationToken) is var removed and > 0)
            logger.LogInformation("Removed {Count} place maps nothing uses", removed);
    }

    private async Task LockedAsync(Func<IServiceProvider, CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        try
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
                return;
            try
            {
                await work(scope.ServiceProvider, cancellationToken);
            }
            finally
            {
                await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _importTriedAt = time.GetUtcNow() - CheckedEvery + Retry;
            logger.LogWarning(exception, "Finding places: the national addresses or place maps failed; trying again in {Hours} hours", Retry.TotalHours);
        }
    }
}
