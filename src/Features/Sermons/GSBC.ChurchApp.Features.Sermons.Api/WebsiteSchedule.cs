using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class WebsiteSchedule(IServiceScopeFactory scopes, IOptions<WebsiteOptions> options, ILogger<WebsiteSchedule> logger) : BackgroundService
{
    private const long AdvisoryLockKey = 0x6773_6263_7365_726d;
    private readonly SemaphoreSlim _now = new(0);

    public void ReadSoon() => _now.Release();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReadAsync(force: false, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Reading sermons from the website failed; trying again later");
            }
            await _now.WaitAsync(options.Value.Every, stoppingToken);
        }
    }

    public async Task<bool> ReadAsync(bool force, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        SermonsDbContext db = scope.ServiceProvider.GetRequiredService<SermonsDbContext>();
        if ((await SourceStates.GetAsync(db, cancellationToken)).Source != SermonSources.Website)
            return false;

        await db.Database.OpenConnectionAsync(cancellationToken);
        bool locked = await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken);
        if (!locked)
            return false;
        try
        {
            await scope.ServiceProvider.GetRequiredService<WebsiteReader>().ReadAsync(force, cancellationToken);
            return true;
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }
}
