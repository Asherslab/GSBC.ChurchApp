using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.News.Contracts;

public static class NewsNotifications
{
    private static readonly Permission PostsForAMinistry =
        new("ministries.posts.write", ScopeKind.Ministry, "Post news and add files and links to a ministry", area: "Ministries");

    public static readonly NotificationKind Post = new(
        "notifications.announcement", "Church news", "Church news",
        MayBeUrgent: true, Pacing: new NotificationPacing(3, TimeSpan.FromHours(1)), Opens: NotificationOpens.ChosenByStaff);

    public static readonly NotificationKind WaitingToPublish = new(
        "news.waiting", "A church news post is waiting for you to publish it", "Church news",
        Waits: true, For: [NewsPermissions.Publish], Opens: new("/news/{id}"));

    public static readonly NotificationKind Asked = new(
        "news.asked", "A ministry asks for one of its posts to go in church news", "Church news",
        Waits: true, For: [NewsPermissions.Publish], Opens: new("/news/{id}"));

    public static readonly NotificationKind Answered = new(
        "news.answered", "Church staff answer your ask for church news", "Church news", For: [PostsForAMinistry],
        Opens: NotificationOpens.ChosenByStaff);

    public static readonly IReadOnlyList<NotificationKind> All = [Post, WaitingToPublish, Asked, Answered];
}
