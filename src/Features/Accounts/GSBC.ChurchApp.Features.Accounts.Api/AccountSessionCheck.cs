using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountSessionCheck(AccountsDbContext db, TimeProvider time) : ISignedInSessions
{
    public async ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out Guid id))
            return false;
        DateTime now = time.GetUtcNow().UtcDateTime;
        return await db.Sessions.AsNoTracking().AnyAsync(session => session.Id == id && session.ExpiresAt > now, cancellationToken);
    }
}
