namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IPhoneRoles
{
    ValueTask GiveAsync(Guid installId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken);

    ValueTask TakeAwayAsync(Guid installId, string roleKey, string? objectId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<HeldRole>> HeldAsync(Guid installId, CancellationToken cancellationToken);

    ValueTask MoveToPersonAsync(Guid installId, Guid userId, CancellationToken cancellationToken);
}

public sealed record HeldRole(string RoleKey, string? ObjectId, DateTime? ExpiresAt);
