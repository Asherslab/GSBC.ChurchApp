using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountsPeopleIndex(AccountsDbContext db) : IPeopleIndex
{
    public async ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(user => !user.Disabled).Select(user => user.Id).ToListAsync(cancellationToken);

    public async ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(user => people.Contains(user.Id)).ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
}
