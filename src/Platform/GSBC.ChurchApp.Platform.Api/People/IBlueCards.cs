namespace GSBC.ChurchApp.Platform.Api.People;

public interface IBlueCards
{
    ValueTask<IReadOnlySet<Guid>> HoldingCurrentAsync(IReadOnlyCollection<Guid> people, DateOnly today, CancellationToken cancellationToken);
}

internal sealed class NoBlueCards : IBlueCards
{
    public ValueTask<IReadOnlySet<Guid>> HoldingCurrentAsync(IReadOnlyCollection<Guid> people, DateOnly today, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
}
