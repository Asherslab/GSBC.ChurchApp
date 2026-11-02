using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaReferences(MediaDbContext db, IBlobStore blobs) : IMediaReferences
{
    public async Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken)
    {
        List<Guid> ids = claimed.Select(media => Guid.TryParse(media.Id, out Guid id) ? id : throw NotInLibrary(media)).ToList();
        Dictionary<Guid, MediaItem> items = await db.Items.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return claimed.Zip(ids, (media, id) => Verified(media, items.GetValueOrDefault(id))).ToList();
    }

    public async Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        HashSet<Guid> ids = [.. verified.Select(media => Guid.Parse(media.Id))];

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Uses.Where(use => use.Owner == owner).ExecuteDeleteAsync(cancellationToken);
        db.Uses.AddRange(ids.Select(id => new MediaUse { MediaId = id, Owner = owner }));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "An image or file was deleted from the media library while you were editing. Choose it again."));
        }
        finally
        {
            db.ChangeTracker.Clear();
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private MediaRef Verified(MediaRef claimed, MediaItem? item)
    {
        if (item is null || (claimed.Key.Length > 0 && claimed.Key != item.Key))
            throw NotInLibrary(claimed);

        MediaRef verified = MediaRefs.Of(item, blobs);
        string altText = claimed.AltText.Trim();
        if (altText.Length > 0)
            verified.AltText = altText.Length <= MediaRefs.MaximumTextLength ? altText : throw Invalid("Keep image descriptions under 200 characters.");
        return verified;
    }

    private static RpcException NotInLibrary(MediaRef claimed) =>
        Invalid($"'{(claimed.FileName.Length > 0 ? claimed.FileName : claimed.Id)}' isn't in the media library.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
