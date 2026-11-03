using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountsPeopleIndex(AccountsDbContext db) : IPeopleIndex
{
    public async ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(user => !user.Disabled).Select(user => user.Id).ToListAsync(cancellationToken);
}
