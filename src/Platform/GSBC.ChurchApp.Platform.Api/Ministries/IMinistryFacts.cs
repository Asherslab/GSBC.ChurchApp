namespace GSBC.ChurchApp.Platform.Api.Ministries;

public interface IMinistryFacts
{
    ValueTask<MinistryFacts?> FindAsync(Guid ministryId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<MinistryFacts>> UsingAsync(string part, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<MinistryMember>> TeamAsync(Guid ministryId, CancellationToken cancellationToken);
}

public sealed record MinistryFacts(Guid Id, string Name, bool Archived, IReadOnlyList<string> PartsOff)
{
    public bool Uses(string part) => !PartsOff.Contains(part);
}

public sealed record MinistryMember(Guid PersonId, bool Leads);

internal sealed class NoMinistryFacts : IMinistryFacts
{
    public ValueTask<MinistryFacts?> FindAsync(Guid ministryId, CancellationToken cancellationToken) => ValueTask.FromResult<MinistryFacts?>(null);

    public ValueTask<IReadOnlyList<MinistryFacts>> UsingAsync(string part, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<MinistryFacts>>([]);

    public ValueTask<IReadOnlyList<MinistryMember>> TeamAsync(Guid ministryId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<MinistryMember>>([]);
}
