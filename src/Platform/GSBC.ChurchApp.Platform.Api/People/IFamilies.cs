namespace GSBC.ChurchApp.Platform.Api.People;

public interface IFamilies
{
    ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken);

    ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken);
}

public sealed record PersonReach(Guid PersonId, bool Minor, bool Directly, IReadOnlyList<Guid> Parents)
{
    public bool ThroughParents => Parents.Count > 0;
}

public sealed record ActingFor(Guid ActorId, Guid PersonId);

internal sealed class NoFamilies : IFamilies
{
    public ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new PersonReach(personId, Minor: false, Directly: true, Parents: []));

    public ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken) => ValueTask.FromResult(actorId == personId);
}
