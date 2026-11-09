using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountDeletion(AccountsDbContext db, IEnumerable<IPersonData> features, AccountsAudit audit, TimeProvider time, ILogger<AccountDeletion> logger)
{
    public async Task DeleteAsync(Guid userId, Guid actorId, CancellationToken cancellationToken)
    {
        AccountUser user = await db.Users.SingleOrDefaultAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such account."));
        foreach (IPersonData feature in features)
            if (await feature.WhyKeepAsync(userId, cancellationToken) is { } reason)
                throw new RpcException(new Status(StatusCode.FailedPrecondition, reason));

        await using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.Set<IdentityUserPasskey<Guid>>().Where(passkey => passkey.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.Set<IdentityUserClaim<Guid>>().Where(claim => claim.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.Set<IdentityUserLogin<Guid>>().Where(login => login.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.Set<IdentityUserToken<Guid>>().Where(token => token.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.Sessions.Where(session => session.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.Codes.Where(code => code.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.ReviewCodes.Where(code => code.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            if (user.Email is { } email)
                await db.Audit.Where(entry => entry.UserId == userId && entry.Detail == email).ExecuteUpdateAsync(set => set.SetProperty(entry => entry.Detail, (string?)null), cancellationToken);

            Tombstone(user, time.GetUtcNow().UtcDateTime);
            audit.Record(AuditActions.Deleted, userId, actorId);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            await ForgetAsync(user, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Forgetting deleted account {UserId} failed; the clean-up tries again", userId);
        }
    }

    public async Task FinishAsync(CancellationToken cancellationToken)
    {
        foreach (AccountUser user in await db.Users.Where(user => user.DeletedAt != null && user.ForgottenAt == null).ToListAsync(cancellationToken))
            await ForgetAsync(user, cancellationToken);
    }

    public async Task ForgetEverywhereAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (IPersonData feature in features)
            await feature.ForgetAsync(userId, cancellationToken);
    }

    private static void Tombstone(AccountUser user, DateTime now)
    {
        string placeholder = $"deleted-{user.Id:N}";
        user.Email = null;
        user.NormalizedEmail = null;
        user.EmailConfirmed = false;
        user.UserName = placeholder;
        user.NormalizedUserName = placeholder.ToUpperInvariant();
        user.PhoneNumber = null;
        user.PasswordHash = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.Disabled = true;
        user.DeletedAt = now;
    }

    private async Task ForgetAsync(AccountUser user, CancellationToken cancellationToken)
    {
        await ForgetEverywhereAsync(user.Id, cancellationToken);
        user.ForgottenAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }
}
