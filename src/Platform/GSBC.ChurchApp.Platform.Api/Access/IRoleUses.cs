namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IRoleUses
{
    ValueTask<IReadOnlyList<string>> UsesAsync(Guid roleId, CancellationToken cancellationToken);
}
