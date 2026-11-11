namespace GSBC.ChurchApp.Platform.Api.People;

public interface ISafeguardingStandings
{
    ValueTask<IReadOnlyDictionary<Guid, SafeguardingStanding>> OfAsync(IReadOnlyCollection<Guid> people, DateOnly on, CancellationToken cancellationToken);
}

public sealed record SafeguardingStanding(Guid PersonId, bool HoldsBlueCard, bool TrainingCurrent)
{
    public static SafeguardingStanding Unknown(Guid personId) => new(personId, HoldsBlueCard: false, TrainingCurrent: false);
}

internal sealed class NoSafeguardingStandings : ISafeguardingStandings
{
    public ValueTask<IReadOnlyDictionary<Guid, SafeguardingStanding>> OfAsync(IReadOnlyCollection<Guid> people, DateOnly on, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, SafeguardingStanding>>(new Dictionary<Guid, SafeguardingStanding>());
}
