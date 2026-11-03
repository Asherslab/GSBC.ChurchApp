using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationKindCatalog(IEnumerable<NotificationKindSet> sets)
{
    public IReadOnlyDictionary<string, NotificationKind> ByKey { get; } = sets.SelectMany(set => set.Kinds).ToDictionary(kind => kind.Key);
}
