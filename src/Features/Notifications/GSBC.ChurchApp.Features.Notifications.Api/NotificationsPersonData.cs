using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationsPersonData(NotificationsDbContext db) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        await db.Deliveries.Where(delivery => delivery.UserId == userId && delivery.SentAt == null && delivery.GaveUpAt == null).ExecuteDeleteAsync(cancellationToken);
        await db.Inbox.Where(entry => entry.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Preferences.Where(preference => preference.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Devices.Where(device => device.UserId == userId).ExecuteUpdateAsync(set => set.SetProperty(device => device.UserId, (Guid?)null), cancellationToken);
    }
}
