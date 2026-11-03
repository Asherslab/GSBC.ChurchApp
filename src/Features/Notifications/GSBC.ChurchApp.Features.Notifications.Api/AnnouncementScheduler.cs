using GSBC.ChurchApp.Features.Notifications.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class AnnouncementScheduler(IServiceScopeFactory scopes, TimeProvider time, ILogger<AnnouncementScheduler> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every, time);
        do
        {
            try
            {
                await SendDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Sending scheduled announcements failed; trying again shortly");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendDueAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Guid> due = await db.Database.SqlQuery<Guid>($"""
            UPDATE notifications.announcements SET state = 'Sent', sent_at = {now}
            WHERE state = 'Scheduled' AND send_at <= {now}
            RETURNING id AS "Value"
            """).ToListAsync(cancellationToken);
        AnnouncementsService announcements = scope.ServiceProvider.GetRequiredService<AnnouncementsService>();
        foreach (AnnouncementRecord record in await db.Announcements.Where(announcement => due.Contains(announcement.Id)).ToListAsync(cancellationToken))
            await announcements.SendNowAsync(record, cancellationToken);
    }
}
