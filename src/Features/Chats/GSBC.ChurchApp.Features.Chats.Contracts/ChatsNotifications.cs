using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsNotifications
{
    public static readonly NotificationKind Message = new(
        "chats.message", "A new message in a chat you're in", "Chats", PerScope: ScopeKind.Ministry, Opens: new("/chats/{id}"));

    public static readonly NotificationKind Mention = new(
        "chats.mention", "Someone mentions you in a chat, even one you've muted", "Chats", Opens: new("/chats/{id}"));

    public static readonly NotificationKind Consent = new(
        "chats.consent", "A leader asks you to approve a chat for your child", "Chats", Waits: true, Opens: new("/chats/{id}"));

    public static readonly NotificationKind Report = new(
        "chats.report", "Someone reports a message in a chat in a ministry you lead", "Chats", Waits: true, Opens: new("/chats/{id}"));

    public static readonly NotificationKind Urgent = new(
        "chats.urgent", "Someone reports a message straight to the church's chat auditors", "Chats", Mandatory: true, MayBeUrgent: true, Waits: true,
        For: ChatsPermissions.Audit, InAdminPanel: true, Opens: new("/chats/reports"));

    public static readonly IReadOnlyList<NotificationKind> All = [Message, Mention, Consent, Report, Urgent];
}
