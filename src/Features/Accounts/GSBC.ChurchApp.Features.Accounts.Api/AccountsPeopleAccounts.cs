using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountsPeopleAccounts(AccountsDbContext db, OneTimeCodes codes, AccountsAudit audit, IServiceProvider services, TimeProvider time)
    : IPeopleAccounts
{
    private const int MaximumNameLength = 80;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async ValueTask<Guid> AddWithoutSignInAsync(string name, Guid addedBy, CancellationToken cancellationToken)
    {
        Guid id = Guid.CreateVersion7();
        string userName = $"family-{id:N}";
        db.Users.Add(new AccountUser
        {
            Id = id,
            DisplayName = ValidName(name),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = Now,
            AddedByFamily = true,
        });
        audit.Record(AuditActions.AddedByFamily, id, addedBy);
        Changed(id);
        await db.SaveChangesAsync(cancellationToken);
        return id;
    }

    public async ValueTask<FamilySignInCode> MakeSignInCodeAsync(Guid userId, Guid madeBy, CancellationToken cancellationToken)
    {
        AccountUser user = await LiveAsync(userId, cancellationToken);
        if (user.Disabled)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{user.DisplayName}'s account is turned off. Ask the church office."));
        string code = await codes.IssueShortCodeAsync(user.Id, CodePurpose.FamilySignIn, OneTimeCodes.FamilySignInLifetime, cancellationToken);
        audit.Record(AuditActions.FamilyCodeMade, user.Id, madeBy);
        await db.SaveChangesAsync(cancellationToken);
        return new FamilySignInCode(code, Now + OneTimeCodes.FamilySignInLifetime);
    }

    public async ValueTask<Guid?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        string normalized = email.Trim().ToUpperInvariant();
        return await db.Users.AsNoTracking()
            .Where(user => user.NormalizedEmail == normalized && user.DeletedAt == null)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyDictionary<Guid, PersonAccount>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        HashSet<Guid> withPasskeys = (await db.Set<IdentityUserPasskey<Guid>>().AsNoTracking()
            .Where(passkey => ids.Contains(passkey.UserId)).Select(passkey => passkey.UserId).Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
        return await db.Users.AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id,
                user => new PersonAccount(user.Id, user.DisplayName, withPasskeys.Contains(user.Id), user.AddedByFamily, user.DeletedAt != null),
                cancellationToken);
    }

    public async ValueTask RenameAsync(Guid userId, string name, Guid actorId, CancellationToken cancellationToken)
    {
        AccountUser user = await LiveAsync(userId, cancellationToken);
        if (!user.AddedByFamily)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{user.DisplayName} changes their own name on their Account page."));
        user.DisplayName = ValidName(name);
        Changed(user.Id);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask RemoveAsync(Guid userId, Guid actorId, CancellationToken cancellationToken)
    {
        if (await db.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken))
            await services.GetRequiredService<AccountDeletion>().DeleteAsync(userId, actorId, cancellationToken);
    }

    private async Task<AccountUser> LiveAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "No such person."));

    private void Changed(Guid userId) => db.FeedChanges.Add(FeedChangeRecord.Marker(AccountsFeeds.People, userId.ToString(), Now));

    private static string ValidName(string name)
    {
        string trimmed = name.Trim();
        return trimmed.Length is > 0 and <= MaximumNameLength
            ? trimmed
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"A name is 1 to {MaximumNameLength} characters."));
    }
}

internal sealed class PersonScopeObjects(AccountsDbContext db) : IScopeObjects
{
    public ScopeKind Kind => ScopeKind.Person;

    public async ValueTask<bool> ExistsAsync(string objectId, CancellationToken cancellationToken) =>
        Guid.TryParse(objectId, out Guid id) && await db.Users.AsNoTracking().AnyAsync(user => user.Id == id && user.DeletedAt == null, cancellationToken);
}
