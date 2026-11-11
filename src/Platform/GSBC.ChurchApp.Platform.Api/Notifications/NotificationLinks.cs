using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Api.Notifications;

public static class NotificationLinks
{
    public static Notification Checked(Notification notification) =>
        notification is { Kind.Opens: { } opens, Link: { } link } && LinkTargets.KindOf(link) == LinkKind.InApp && !opens.Allows(link)
            ? throw new ArgumentException(
                $"'{notification.Kind.Key}' notifications open {string.Join(" or ", opens.Routes)}, not '{link}'. Declare the place it opens.", nameof(notification))
            : notification;
}
