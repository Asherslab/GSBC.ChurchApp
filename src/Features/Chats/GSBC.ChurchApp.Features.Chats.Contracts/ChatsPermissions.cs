using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsPermissions
{
    public const string ManageNode = "chats.manage";
    public const string CheckNode = "chats.check";
    public const string AuditNode = "chats.audit";
    public const string MemberNode = "chats.member";

    public const string MemberRole = "chat-member";
    public const string AuditorRole = "chat-auditor";
    public const string PausedRole = "chats-paused";

    public static readonly Permission Manage =
        new(ManageNode, ScopeKind.Ministry, "Start, change and delete a ministry's chats, post in its channels and add people to its chats");

    public static readonly Permission Check =
        new(CheckNode, ScopeKind.Ministry, "Check the chats in a ministry and the ministries under it. Every check is logged");

    public static readonly Permission Audit =
        new(AuditNode, ScopeKind.Global, "Check any chat, receive reports and pause people from chats. Every check is logged", isSensitive: true);

    public static readonly Permission Member =
        new(MemberNode, ScopeKind.Chat, "Read and write in a chat they were added to");

    public static readonly IReadOnlyList<Permission> All = [Manage, Check, Audit, Member];
}
