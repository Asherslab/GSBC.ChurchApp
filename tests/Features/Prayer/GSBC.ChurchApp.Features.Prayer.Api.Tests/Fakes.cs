using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Api.People;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

internal sealed class RecordedNotifications : INotifications
{
    public List<Notification> Raised { get; } = [];
    public List<string> Settled { get; } = [];

    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
    {
        Raised.Add(NotificationLinks.Checked(notification));
        return Task.CompletedTask;
    }

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken)
    {
        Raised.AddRange(mostSpecificFirst.Select(NotificationLinks.Checked));
        return Task.CompletedTask;
    }

    public Task SettleAsync(string subject, CancellationToken cancellationToken)
    {
        Settled.Add(subject);
        return Task.CompletedTask;
    }

    public List<NotificationDigestItem> Collected { get; } = [];

    public Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken)
    {
        Collected.Add(item);
        return Task.CompletedTask;
    }

    public List<string> Forgotten { get; } = [];

    public Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken)
    {
        Forgotten.Add(group);
        return Task.CompletedTask;
    }
}

internal sealed class KnownPeople : IPeopleIndex, IFamilies
{
    public Dictionary<Guid, string> Names { get; } = [];
    public Dictionary<Guid, int> Ages { get; } = [];

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(people.Where(Names.ContainsKey).ToDictionary(person => person, person => Names[person]));

    public ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, FamilyFacts>>(people.ToDictionary(person => person,
            person => new FamilyFacts(person, Ages.TryGetValue(person, out int age) ? age : null, true, [], [])));

    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlySet<Guid>> NotInvitedYetAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
}
