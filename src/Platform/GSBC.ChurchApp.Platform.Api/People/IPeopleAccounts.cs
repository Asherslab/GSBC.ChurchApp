namespace GSBC.ChurchApp.Platform.Api.People;

public interface IPeopleAccounts
{
    ValueTask<Guid> AddWithoutSignInAsync(string name, Guid addedBy, CancellationToken cancellationToken);

    ValueTask<Guid> AddWithoutSignInAsync(Guid id, string name, Guid addedBy, CancellationToken cancellationToken) => AddWithoutSignInAsync(name, addedBy, cancellationToken);

    ValueTask<FamilySignInCode> MakeSignInCodeAsync(Guid userId, Guid madeBy, CancellationToken cancellationToken);

    ValueTask<Guid?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<Guid, PersonAccount>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    ValueTask RenameAsync(Guid userId, string name, Guid actorId, CancellationToken cancellationToken);

    ValueTask RemoveAsync(Guid userId, Guid actorId, CancellationToken cancellationToken);
}

public sealed record FamilySignInCode(string Code, DateTime ExpiresAt);

public sealed record PersonAccount(Guid Id, string Name, bool CanSignIn, bool AddedByFamily, bool Deleted);

internal sealed class NoPeopleAccounts : IPeopleAccounts
{
    public ValueTask<Guid> AddWithoutSignInAsync(string name, Guid addedBy, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No feature keeps accounts.");

    public ValueTask<FamilySignInCode> MakeSignInCodeAsync(Guid userId, Guid madeBy, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No feature keeps accounts.");

    public ValueTask<Guid?> FindByEmailAsync(string email, CancellationToken cancellationToken) => ValueTask.FromResult<Guid?>(null);

    public ValueTask<IReadOnlyDictionary<Guid, PersonAccount>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, PersonAccount>>(new Dictionary<Guid, PersonAccount>());

    public ValueTask RenameAsync(Guid userId, string name, Guid actorId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask RemoveAsync(Guid userId, Guid actorId, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
