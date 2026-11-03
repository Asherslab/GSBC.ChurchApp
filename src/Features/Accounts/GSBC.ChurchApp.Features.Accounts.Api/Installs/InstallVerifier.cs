using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Installs;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api.Installs;

internal sealed class InstallVerifier(AccountsDbContext db, TimeProvider time) : IInstallVerifier
{
    public async ValueTask<Guid?> VerifyAsync(InstallProof proof, CancellationToken cancellationToken)
    {
        AppInstall? install = await db.Installs.SingleOrDefaultAsync(candidate => candidate.KeyId == proof.KeyId, cancellationToken);
        if (install is null || install.Blocked)
            return null;

        long? counter = AppAttest.VerifyAssertion(install.PublicKey, install.AppId, install.Counter, proof.Assertion, proof.ClientData);
        if (counter is null)
            return null;

        int advanced = await db.Installs
            .Where(candidate => candidate.Id == install.Id && candidate.Counter < counter)
            .ExecuteUpdateAsync(set => set
                .SetProperty(candidate => candidate.Counter, counter.Value)
                .SetProperty(candidate => candidate.LastSeenAt, time.GetUtcNow().UtcDateTime), cancellationToken);
        return advanced == 1 ? install.Id : null;
    }
}
