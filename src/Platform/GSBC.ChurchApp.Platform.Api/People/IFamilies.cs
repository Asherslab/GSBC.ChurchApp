namespace GSBC.ChurchApp.Platform.Api.People;

public interface IFamilies
{
    ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken);

    ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken);

    ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken);
}

public sealed record FamilyFacts(Guid PersonId, int? Age, bool CanSignIn, IReadOnlyList<Guid> Parents, IReadOnlyList<Guid> Children)
{
    public bool Minor => Age < 18;
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

    public ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, FamilyFacts>>(people.Distinct().ToDictionary(person => person, person => new FamilyFacts(person, null, true, [], [])));

    public ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No feature keeps families.");
}
