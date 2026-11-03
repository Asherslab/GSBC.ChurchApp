namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IPermissionHolders
{
    ValueTask<IReadOnlyList<Guid>> HoldersOfAsync(string node, CancellationToken cancellationToken);
}

internal sealed class NoPermissionHolders : IPermissionHolders
{
    public ValueTask<IReadOnlyList<Guid>> HoldersOfAsync(string node, CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<Guid>>([]);
}
