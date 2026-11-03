using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Notifications;

public sealed record NotificationKind(
    string Key,
    string Description,
    string Area,
    bool DefaultPush = true,
    bool Mandatory = false,
    ScopeKind? PerScope = null,
    bool MayBeUrgent = false,
    NotificationPacing? Pacing = null);

public sealed record NotificationPacing(int Pushes, TimeSpan Per);
