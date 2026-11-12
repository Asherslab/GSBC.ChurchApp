using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class NotificationsPermissions
{
    public const string WriteNode = "notifications.announcements.write";
    public const string SendNode = "notifications.announcements.send";
    public const string WriterRole = "announcements-writer";
    public const string SenderRole = "announcements-sender";

    public static readonly Permission Write =
        new(WriteNode, ScopeKind.Global, "Write announcements, try them on their own phone, and ask for them to be sent", area: "Announcements", opensAdminArea: true);

    public static readonly Permission Send =
        new(SendNode, ScopeKind.Global, "Send, schedule and recall announcements to everyone", area: "Announcements");

    public static readonly IReadOnlyList<Permission> All = [Write, Send];
}
