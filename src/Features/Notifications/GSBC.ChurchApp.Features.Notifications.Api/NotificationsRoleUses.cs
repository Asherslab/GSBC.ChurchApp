using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationsRoleUses(NotificationsDbContext db) : IRoleUses
{
    public async ValueTask<IReadOnlyList<string>> UsesAsync(Guid roleId, CancellationToken cancellationToken)
    {
        string node = RoleMembership.NodeOf(roleId);
        return await db.Announcements.AsNoTracking()
            .Where(announcement => announcement.AudienceNode == node && announcement.State != AnnouncementState.Sent && announcement.State != AnnouncementState.Cancelled)
            .OrderBy(announcement => announcement.Title)
            .Select(announcement => $"the announcement “{announcement.Title}”")
            .ToListAsync(cancellationToken);
    }
}
