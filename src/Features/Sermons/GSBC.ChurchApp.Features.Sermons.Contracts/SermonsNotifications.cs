using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsNotifications
{
    public const string AllSermonsScope = "sermons";

    public static readonly NotificationKind NewSermon = new(
        "sermons.new", "A new sermon is out", "Sermons", For: SermonsPermissions.HearNew);

    public static readonly NotificationKind NewInSeries = new(
        "sermons.series.new", "A series you follow has a new sermon", "Sermons", PerScope: ScopeKind.SermonSeries, For: SermonsPermissions.HearSeries);

    public static readonly IReadOnlyList<NotificationKind> All = [NewSermon, NewInSeries];
}
