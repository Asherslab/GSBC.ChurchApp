using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

internal sealed class Holders : IPermissionHolders, IPeopleIndex
{
    private readonly Dictionary<(string, string?), PermissionHolders> _granted = [];

    public IReadOnlyList<Guid> Everyone { get; set; } = [];

    public Dictionary<Guid, string> Names { get; } = [];

    public void Grant(Permission permission, string? objectId, IReadOnlyList<Guid> people, IReadOnlyList<Guid> phones) =>
        _granted[(permission.Node, objectId)] = new PermissionHolders(people, phones);

    public ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_granted.GetValueOrDefault((permission.Node, objectId), PermissionHolders.None));

    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Everyone);

    public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<IndexedPerson>>([]);

    public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<IndexedPerson>>([]);

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(Names.Where(name => people.Contains(name.Key)).ToDictionary());
}
