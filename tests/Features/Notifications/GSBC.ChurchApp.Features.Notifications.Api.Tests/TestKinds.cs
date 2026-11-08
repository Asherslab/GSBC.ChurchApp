using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

internal static class TestKinds
{
    public const string Music = "music";

    public static readonly Permission ReceiveNotices = new("ministries.notices.receive", ScopeKind.Ministry, "Get notices");

    public static readonly NotificationKind Notice = new("ministries.notice", "Important notices", "Ministries",
        PerScope: ScopeKind.Ministry, MayBeUrgent: true, Pacing: new NotificationPacing(3, TimeSpan.FromHours(1)));

    public static readonly NotificationKind NewPasskey = new("accounts.passkey.added", "A passkey was added to your account", "Your account",
        Mandatory: true);

    public static readonly NotificationKind ChatMessage = new("ministries.chat", "New messages in your chats", "Ministries", PerScope: ScopeKind.Ministry);

    public static readonly NotificationKind RosterSwap = new("ministries.roster.swap", "Someone asks you to swap a roster", "Ministries", Waits: true);

    public static readonly Permission HearNew = new("sermons.new.receive", ScopeKind.Global, "Hear of new sermons");

    public static readonly Permission HearSeries = new("sermons.series.receive", ScopeKind.SermonSeries, "Hear of new sermons in a series");

    public static readonly NotificationKind NewSermon = new("sermons.new", "A new sermon is out", "Sermons", For: HearNew);

    public static readonly NotificationKind NewInSeries = new("sermons.series.new", "A series you follow has a new sermon", "Sermons",
        PerScope: ScopeKind.SermonSeries, For: HearSeries);

    public static NotificationKindCatalog Catalog =>
        new([new NotificationKindSet([Notice, NewPasskey, ChatMessage, RosterSwap, NewSermon, NewInSeries, .. AnnouncementNotifications.All])]);
}
