using System.Security.Claims;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Chats;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Chats.Api.Tests;

internal sealed class Church : IPermissionHolders, IAccessEvaluator, IRoleAssignments, IFamilies, ISafeguardingStandings, IChatPlaces, IPeopleIndex, INotifications
{
    public const string TeamNode = "ministries.team.view";

    private readonly Dictionary<Guid, string> _names = [];
    private readonly Dictionary<Guid, List<Grant>> _grants = [];
    private readonly List<(Guid Person, string Role, string? Place)> _roles = [];
    private readonly Dictionary<Guid, FamilyFacts> _families = [];
    private readonly HashSet<Guid> _carded = [];

    public Guid MinistryId { get; } = Guid.NewGuid();

    public bool ChatsOn { get; set; } = true;

    public List<Notification> Raised { get; } = [];

    public Guid Person(string name)
    {
        Guid id = Guid.NewGuid();
        _names[id] = name;
        _families[id] = new FamilyFacts(id, 30, true, [], []);
        return id;
    }

    public Guid Child(string name, Guid parent)
    {
        Guid id = Person(name);
        _families[id] = new FamilyFacts(id, 14, false, [parent], []);
        return id;
    }

    public void OnTeam(Guid person) => Grant(person, TeamNode, Scope.Of(ScopeKind.Ministry, MinistryId.ToString()));

    public void OffTeam(Guid person) => _grants[person].RemoveAll(grant => grant.Pattern.Text == TeamNode);

    public void Leads(Guid person)
    {
        OnTeam(person);
        Grant(person, ChatsPermissions.ManageNode, Scope.Of(ScopeKind.Ministry, MinistryId.ToString()));
        Grant(person, ChatsPermissions.CheckNode, Scope.Of(ScopeKind.Ministry, MinistryId.ToString()));
    }

    public void Audits(Guid person) => Grant(person, ChatsPermissions.AuditNode, Scope.Global);

    public void Carded(params Guid[] people) => _carded.UnionWith(people);

    private void Grant(Guid person, string node, Scope scope)
    {
        if (!_grants.TryGetValue(person, out List<Grant>? grants))
            _grants[person] = grants = [];
        grants.Add(new Grant(PermissionPattern.Parse(node), scope));
    }

    private GrantSet GrantsOf(Guid person) => new(_grants.GetValueOrDefault(person) ?? []);

    public ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new PermissionHolders(_names.Keys.Where(person => GrantsOf(person).IsAllowed(permission, objectId)).ToList(), []));

    public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantsOf(userId));

    public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        if (!_roles.Contains((userId, roleKey, objectId)))
            _roles.Add((userId, roleKey, objectId));
        return ValueTask.CompletedTask;
    }

    public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        _roles.Remove((userId, roleKey, objectId));
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<RoleHolder>>(_roles.Where(role => role.Role == roleKey && role.Place == objectId).Select(role => new RoleHolder(role.Person)).ToList());

    public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(actorId == personId || _families.GetValueOrDefault(personId)?.Parents.Contains(actorId) == true);

    public ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, FamilyFacts>>(people.Where(_families.ContainsKey).Distinct().ToDictionary(person => person, person => _families[person]));

    public ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyDictionary<Guid, SafeguardingStanding>> OfAsync(IReadOnlyCollection<Guid> people, DateOnly on, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, SafeguardingStanding>>(
            people.Distinct().ToDictionary(person => person, person => new SafeguardingStanding(person, _carded.Contains(person), false)));

    public ValueTask<IReadOnlyDictionary<Guid, ChatPlace>> PlacesAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, ChatPlace>>(ministryIds.Contains(MinistryId) ? new Dictionary<Guid, ChatPlace> { [MinistryId] = Place } : []);

    public ValueTask<IReadOnlyList<ChatPlace>> AllAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<ChatPlace>>([Place]);

    private ChatPlace Place => new(MinistryId, "Sample Ministry", ChatsOn,
        new Dictionary<string, Audience> { [ChatAudiences.Team] = Audience.HoldingAt(TeamNode, Scope.Of(ScopeKind.Ministry, MinistryId.ToString())) });

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(people.Where(_names.ContainsKey).Distinct().ToDictionary(person => person, person => _names[person]));

    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlySet<Guid>> NotInvitedYetAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
    {
        Raised.Add(notification);
        return Task.CompletedTask;
    }

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) => RaiseAsync(mostSpecificFirst[0], cancellationToken);

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
}
