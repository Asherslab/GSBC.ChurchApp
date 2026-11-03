using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal static class InboxPayloads
{
    public static FeedChangeRecord InboxRow(InboxEntry entry, NotificationRecord notification, long version) => new()
    {
        Feed = NotificationsFeeds.Inbox,
        RecordId = entry.Id.ToString(),
        ScopeId = entry.UserId.ToString(),
        Version = version,
        SortKey = SortKey(notification.RaisedAt),
        Payload = Encode(Item(entry.Id.ToString(), notification, entry.ReadAt)),
    };

    public static FeedChangeRecord PublicRow(NotificationRecord notification, long version = 1) => new()
    {
        Feed = NotificationsFeeds.Public,
        RecordId = notification.Id.ToString(),
        Version = version,
        SortKey = SortKey(notification.RaisedAt),
        GroupKey = notification.ScopeId,
        Payload = Encode(Item(notification.Id.ToString(), notification, null)),
    };

    public static InboxItem Item(string id, NotificationRecord notification, DateTime? readAt) => new()
    {
        Id = id,
        Kind = notification.Kind,
        Area = notification.Area,
        Title = notification.Title,
        Body = notification.Body,
        Link = notification.Link,
        Subject = notification.Subject,
        Urgent = notification.Urgent,
        RaisedAt = notification.RaisedAt,
        ReadAt = readAt,
        ScopeId = notification.ScopeId,
        Test = notification.Test,
        Waiting = notification.Waits && notification.SettledAt is null,
        Action = notification.Action,
        InAdminPanel = notification.InAdminPanel,
    };

    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        NotificationsContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    private static string SortKey(DateTime raisedAt) => raisedAt.ToString("O");
}
