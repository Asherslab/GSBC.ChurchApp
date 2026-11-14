using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachNotifications
{
    public static readonly NotificationKind FollowUpWanted = new(
        "outreach.follow-up", "Someone at a door asks for a follow-up", "Outreach", Waits: true,
        For: [OutreachPermissions.Review], InAdminPanel: true, Opens: new("/outreach/follow-ups"));

    public static readonly NotificationKind Reported = new(
        "outreach.report", "A walker reports a place for review", "Outreach", DefaultPush: false, Waits: true,
        For: [OutreachPermissions.Review], InAdminPanel: true, Opens: new("/outreach/reports"));

    public static readonly IReadOnlyList<NotificationKind> All = [FollowUpWanted, Reported];
}
