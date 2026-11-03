using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IPermissionHolders
{
    ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken);
}

public sealed record PermissionHolders(IReadOnlyList<Guid> People, IReadOnlyList<Guid> Phones)
{
    public static readonly PermissionHolders None = new([], []);
}

internal sealed class NoPermissionHolders : IPermissionHolders
{
    public ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(PermissionHolders.None);
}
