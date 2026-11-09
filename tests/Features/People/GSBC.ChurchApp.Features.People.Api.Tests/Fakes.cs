using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

internal sealed class FakeAccounts(IServiceProvider services) : IPeopleAccounts
{
    private readonly Dictionary<Guid, PersonAccount> _people = [];

    public List<Guid> Removed { get; } = [];

    public Guid Add(string name, bool canSignIn, bool addedByFamily = false)
    {
        Guid id = Guid.CreateVersion7();
        _people[id] = new PersonAccount(id, name, canSignIn, addedByFamily, Deleted: false);
        return id;
    }

    public ValueTask<Guid> AddWithoutSignInAsync(string name, Guid addedBy, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Add(name, canSignIn: false, addedByFamily: true));

    public ValueTask<FamilySignInCode> MakeSignInCodeAsync(Guid userId, Guid madeBy, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new FamilySignInCode("ABCD-EFGH", DateTime.UnixEpoch));

    public ValueTask<Guid?> FindByEmailAsync(string email, CancellationToken cancellationToken) => ValueTask.FromResult<Guid?>(null);

    public ValueTask<IReadOnlyDictionary<Guid, PersonAccount>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, PersonAccount>>(ids.Distinct().Where(_people.ContainsKey).ToDictionary(id => id, id => _people[id]));

    public ValueTask RenameAsync(Guid userId, string name, Guid actorId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public async ValueTask RemoveAsync(Guid userId, Guid actorId, CancellationToken cancellationToken)
    {
        if (_people[userId].Deleted)
            return;
        _people[userId] = _people[userId] with { Deleted = true };
        Removed.Add(userId);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        foreach (IPersonData feature in scope.ServiceProvider.GetServices<IPersonData>())
            await feature.ForgetAsync(userId, cancellationToken);
    }
}

internal sealed class RecordedRoles : IRoleAssignments
{
    public List<(Guid UserId, string ObjectId, DateTime? Until)> Given { get; } = [];
    public List<string> ForgottenPlaces { get; } = [];

    public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        Given.RemoveAll(given => given.UserId == userId && given.ObjectId == objectId);
        Given.Add((userId, objectId!, expiresAt));
        return ValueTask.CompletedTask;
    }

    public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        Given.RemoveAll(given => given.UserId == userId && given.ObjectId == objectId);
        return ValueTask.CompletedTask;
    }

    public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken)
    {
        ForgottenPlaces.AddRange(objectIds);
        Given.RemoveAll(given => objectIds.Contains(given.ObjectId));
        return ValueTask.CompletedTask;
    }

    public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class NoEmails : IAccountEmails
{
    public ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
}

internal sealed class QuietNotifications : INotifications
{
    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
}
