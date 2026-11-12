using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.People.Contracts;

public static class PeopleNotifications
{
    public static readonly NotificationKind Asked = new(
        "people.family.asked", "Someone asks you to look after them in the app", "Your family", Waits: true, Opens: new("/account"));

    public static readonly NotificationKind Linked = new(
        "people.family.linked", "A parent or guardian is linked to you", "Your family", Opens: new("/account"));

    public static readonly IReadOnlyList<NotificationKind> All = [Asked, Linked];
}
