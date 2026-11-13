using GSBC.ChurchApp.Platform.Contracts.News;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.News.Contracts;

public static class NewsPermissions
{
    public const string WriterRole = "news-writer";
    public const string PublisherRole = "news-publisher";
    public const string AnnouncementsWriterRole = "announcements-writer";
    public const string AnnouncementsSenderRole = "announcements-sender";
    public const string AnnouncementsWriteNode = "notifications.announcements.write";
    public const string AnnouncementsSendNode = "notifications.announcements.send";

    public static readonly Permission Write =
        new(ChurchNews.WriteNode, ScopeKind.Global, "Write church news, try its notification on their own phone, and ask for it to be published",
            area: "Church news", opensAdminArea: true);

    public static readonly Permission Publish =
        new(ChurchNews.PublishNode, ScopeKind.Global,
            "Publish, schedule and delete church news and send its notifications, and put ministries' posts into church news", area: "Church news");

    public static readonly IReadOnlyList<Permission> All = [Write, Publish];
}
