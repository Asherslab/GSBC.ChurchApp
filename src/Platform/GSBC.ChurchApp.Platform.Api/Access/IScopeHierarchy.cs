using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IScopeHierarchy
{
    ValueTask<IReadOnlyList<string>> ChildrenOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken);

    ValueTask<string?> ParentOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken);
}

internal sealed class FlatScopeHierarchy : IScopeHierarchy
{
    public ValueTask<IReadOnlyList<string>> ChildrenOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);

    public ValueTask<string?> ParentOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<string?>(null);
}
