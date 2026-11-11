using GSBC.ChurchApp.Features.Feedback.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackSchedule(IServiceScopeFactory scopes, TimeProvider time, ILogger<FeedbackSchedule> logger) : BackgroundService
{
    public static readonly TimeSpan Every = TimeSpan.FromHours(1);
    private const long AdvisoryLockKey = 0x6665_6564_6261_636b;

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
                logger.LogError(exception, "Deleting old feedback and unsent uploads failed; trying again later");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task KeepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        FeedbackDbContext db = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            await scope.ServiceProvider.GetRequiredService<FeedbackKeeping>().RunAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }
}
