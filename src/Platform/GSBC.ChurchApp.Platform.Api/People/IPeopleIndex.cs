namespace GSBC.ChurchApp.Platform.Api.People;

public interface IPeopleIndex
{
    ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken);
}

internal sealed class NoPeople : IPeopleIndex
{
    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<Guid>>([]);

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
}
