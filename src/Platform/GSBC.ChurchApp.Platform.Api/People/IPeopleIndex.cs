namespace GSBC.ChurchApp.Platform.Api.People;

public interface IPeopleIndex
{
    ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken);
}

public sealed record IndexedPerson(Guid Id, string Name, string Email, bool Disabled);

internal sealed class NoPeople : IPeopleIndex
{
    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<Guid>>([]);

    public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<IndexedPerson>>([]);

    public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<IndexedPerson>>([]);

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
}
