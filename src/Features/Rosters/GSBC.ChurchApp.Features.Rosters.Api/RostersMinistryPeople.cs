using GSBC.ChurchApp.Platform.Api.Ministries;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RostersMinistryPeople(RosterPublisher publisher) : IMinistryPeopleChanged
{
    public async Task ChangedAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken)
    {
        foreach (Guid ministry in ministryIds)
            await publisher.PublishAsync(ministry, cancellationToken);
    }
}
