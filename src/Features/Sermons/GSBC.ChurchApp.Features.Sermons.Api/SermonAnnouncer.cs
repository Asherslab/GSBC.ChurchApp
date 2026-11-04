using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonAnnouncer(SermonsDbContext db, INotifications notifications, TimeProvider time)
{
    public static readonly TimeSpan NewFor = TimeSpan.FromDays(3);

    public async Task AnnounceAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<SermonRecord> waiting = await db.Sermons.Where(sermon => sermon.AnnouncedAt == null && sermon.PublishedHash != null && !sermon.Hidden)
            .OrderBy(sermon => sermon.PreachedAt).ToListAsync(cancellationToken);
        if (waiting.Count == 0)
            return;

        Dictionary<Guid, SeriesRecord> series = await db.Series.AsNoTracking().ToDictionaryAsync(record => record.Id, cancellationToken);
        Dictionary<Guid, SpeakerRecord> speakers = await db.Speakers.AsNoTracking().ToDictionaryAsync(record => record.Id, cancellationToken);
        foreach (SermonRecord sermon in waiting)
        {
            sermon.AnnouncedAt = now;
            if (now - sermon.PreachedAt > NewFor)
                continue;
            Sermon payload = SermonPublisher.Payload(sermon, series, speakers);
            string link = SermonsDestinations.Sermon.PathOf(payload.Id);
            string body = string.Join(" · ", new[] { payload.SpeakerName, payload.Service, payload.SeriesName }.Where(part => !string.IsNullOrEmpty(part)));
            await notifications.RaiseAsync(new Notification(SermonsNotifications.NewSermon, NotificationAudience.Holding(SermonsPermissions.HearNew),
                $"New sermon: {payload.Title}", body, Link: link, Subject: $"sermon/{payload.Id}", ScopeId: SermonsNotifications.AllSermonsScope,
                Action: "Listen"), cancellationToken);
            if (payload.SeriesId is { } seriesId)
                await notifications.RaiseAsync(new Notification(SermonsNotifications.NewInSeries, NotificationAudience.Holding(SermonsPermissions.HearSeries, seriesId),
                    $"New in {payload.SeriesName}: {payload.Title}", body, Link: link, Subject: $"sermon/{payload.Id}/series", ScopeId: seriesId,
                    Action: "Listen"), cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
