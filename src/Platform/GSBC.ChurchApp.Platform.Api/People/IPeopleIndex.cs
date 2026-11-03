namespace GSBC.ChurchApp.Platform.Api.People;

public interface IPeopleIndex
{
    ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken);
}

internal sealed class NoPeople : IPeopleIndex
{
    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<Guid>>([]);
}
