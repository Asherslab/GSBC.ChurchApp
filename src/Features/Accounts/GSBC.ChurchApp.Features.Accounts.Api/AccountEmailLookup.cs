using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Email;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountEmailLookup(AccountsDbContext db) : IAccountEmails
{
    public async ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await db.Users
            .Where(user => userIds.Contains(user.Id) && !user.Disabled && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(cancellationToken);
}
