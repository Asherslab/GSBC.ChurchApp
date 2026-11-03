using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Accounts.Admin;

internal sealed class InvitationsOfficeWeek(AccountsClient accounts, IAccess access, TimeProvider clock) : IOfficeWeekSource
{
    private static readonly TimeSpan QuietFor = TimeSpan.FromDays(2);
    private const string Href = "people?show=invited";

    public IReadOnlyCollection<string> Feeds { get; } = [AccountsFeeds.People];

    public async Task<OfficeWeek> LoadAsync(DateTime from, DateTime until)
    {
        if (!access.Can(AccountsPermissions.Manage))
            return OfficeWeek.Nothing;

        DateTime now = clock.GetUtcNow().UtcDateTime;
        List<OfficeTask> tasks = (await accounts.ListInvitationsAsync()).Invitations
            .Where(invitation => now - invitation.InvitedAt >= QuietFor || invitation.ExpiresAt <= now)
            .Select(invitation => invitation.ExpiresAt <= now
                ? new OfficeTask(invitation.DisplayName, "Their invitation expired before they accepted it", Href, "Send again", invitation.ExpiresAt)
                : new OfficeTask(invitation.DisplayName, $"Hasn't accepted their invitation{InvitedBy(invitation)} yet", Href, "Follow up", invitation.InvitedAt))
            .ToList();
        return new OfficeWeek(tasks, []);
    }

    private static string InvitedBy(InvitationInfo invitation) => invitation.InvitedBy is { } inviter ? $" from {inviter}" : "";
}
