using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Notifications.Admin;

internal sealed class AnnouncementsOfficeWeek(AnnouncementsClient announcements, IAccess access) : IOfficeWeekSource
{
    private const string WaitingToSend = "WaitingToSend";
    private const string Scheduled = "Scheduled";

    public IReadOnlyCollection<string> Feeds { get; } = [NotificationsFeeds.Announcements];

    public async Task<OfficeWeek> LoadAsync(DateTime from, DateTime until)
    {
        if (!access.Can(NotificationsPermissions.Write))
            return OfficeWeek.Nothing;

        IReadOnlyList<Announcement> all = await announcements.ListAsync();
        List<OfficeTask> tasks = access.Can(NotificationsPermissions.Send)
            ? all.Where(announcement => announcement.State == WaitingToSend)
                .Select(announcement => new OfficeTask(announcement.Title, "An announcement waiting to be sent", Href(announcement), "Review", announcement.UpdatedAt))
                .ToList()
            : [];
        List<OfficeSpan> spans = all
            .Where(announcement => announcement.State == Scheduled && announcement.SendAt is { } at && at >= from && at < until)
            .Select(announcement => new OfficeSpan(announcement.Title, announcement.SendAt!.Value, announcement.SendAt.Value, Href(announcement), OfficeSpanKind.Everyone))
            .ToList();
        return new OfficeWeek(tasks, spans);
    }

    private static string Href(Announcement announcement) => $"announcements/{announcement.Id}";
}
