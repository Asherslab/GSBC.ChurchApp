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
            if (Announcements(SermonPublisher.Payload(sermon, series, speakers), now) is { Count: > 0 } mostSpecificFirst)
                await notifications.RaiseOneOfAsync(mostSpecificFirst, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<Notification> Announcements(Sermon payload, DateTime now)
    {
        if (now - payload.PreachedAt > NewFor)
            return [];
        string link = SermonsDestinations.Sermon.PathOf(payload.Id);
        string body = string.Join(" · ", new[] { payload.SpeakerName, payload.Service, payload.SeriesName }.Where(part => !string.IsNullOrEmpty(part)));
        string subject = $"sermon/{payload.Id}";
        List<Notification> announcements = [];
        if (payload.SeriesId is { } seriesId)
            announcements.Add(new Notification(SermonsNotifications.NewInSeries, NotificationAudience.Holding(SermonsPermissions.HearSeries, seriesId),
                $"New in {payload.SeriesName}: {payload.Title}", body, Link: link, Subject: subject, ScopeId: seriesId, Action: "Listen"));
        announcements.Add(new Notification(SermonsNotifications.NewSermon, NotificationAudience.Holding(SermonsPermissions.HearNew),
            $"New sermon: {payload.Title}", body, Link: link, Subject: subject, ScopeId: SermonsNotifications.AllSermonsScope, Action: "Listen"));
        return announcements;
    }
}
