using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaLibrary(
    MediaDbContext db, MediaFolders folders, IBlobStore blobs, IAccessEvaluator access, PermissionCatalog catalog, TimeProvider time)
{
    public static readonly TimeSpan UploadWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PrivateLinkLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PendingKeptFor = TimeSpan.FromDays(1);
    private const int MaximumPage = 200;

    public async Task<UploadTicket> BeginUploadAsync(ClaimsPrincipal user, BeginUploadRequest request, CancellationToken cancellationToken)
    {
        if (!MediaTypes.TryGetExtension(request.ContentType, out _))
            throw Invalid($"'{request.ContentType}' files can't be uploaded. Use one of: {string.Join(", ", MediaTypes.Accepted)}.");
        if (request.FileName.Length > MediaRefs.MaximumTextLength || request.AltText.Length > MediaRefs.MaximumTextLength)
            throw Invalid("Keep the file name and description under 200 characters.");
        if (request.PrivateTo is { } audience)
            await CheckAudienceAsync(user, audience, cancellationToken);
        if (MediaTypes.IsFont(request.ContentType) && !await access.IsAllowedAsync(user, MediaPermissions.UploadFontsNode, null, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Uploading font files needs permission to, since the church must be licensed to use the font in an app. Choose one from Google Fonts instead."));
        if (MediaTypes.IsFont(request.ContentType) && request.PrivateTo is not null)
            throw Invalid("Fonts are public: anyone using the app downloads them.");

        Guid? folderId = await folders.UploadFolderAsync(request.FolderId, cancellationToken);
        DateTime now = Now;
        await db.PendingUploads.Where(upload => upload.ExpiresAt < now - PendingKeptFor).ExecuteDeleteAsync(cancellationToken);

        PendingUpload pending = new()
        {
            Id = Guid.CreateVersion7(),
            UploadedBy = CallerId(user),
            ContentType = request.ContentType.ToLowerInvariant(),
            FileName = request.FileName.Trim(),
            AltText = request.AltText.Trim(),
            AudiencePermission = request.PrivateTo?.Permission,
            AudienceScopeId = NullIfEmpty(request.PrivateTo?.ScopeId),
            ExpiresAt = now + UploadWindow,
            FolderId = folderId,
        };
        db.PendingUploads.Add(pending);
        await db.SaveChangesAsync(cancellationToken);

        Uri url = await blobs.PresignUploadAsync(BlobBucket.Private, pending.StagingKey, pending.ContentType, UploadWindow);
        return new UploadTicket { UploadId = pending.Id.ToString(), Url = url.ToString(), ContentType = pending.ContentType, ExpiresAt = pending.ExpiresAt };
    }

    public async Task<MediaRef> CompleteUploadAsync(ClaimsPrincipal user, string uploadId, CancellationToken cancellationToken)
    {
        Guid caller = CallerId(user);
        PendingUpload pending = await db.PendingUploads
            .SingleOrDefaultAsync(upload => upload.Id == ParseId(uploadId) && upload.UploadedBy == caller, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That upload has expired. Start again."));

        if (await blobs.FindAsync(BlobBucket.Private, pending.StagingKey, cancellationToken) is null)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "The file hasn't arrived yet."));

        InspectedContent content;
        await using (Stream staged = await blobs.OpenAsync(BlobBucket.Private, pending.StagingKey, cancellationToken))
            content = await MediaInspector.InspectAsync(staged, pending.ContentType, cancellationToken);

        if (!content.MatchesType)
        {
            await DiscardAsync(pending, cancellationToken);
            throw Invalid($"That file isn't really {pending.ContentType}.");
        }

        MediaTypes.TryGetExtension(pending.ContentType, out string extension);
        string key = ContentAddress.KeyFor(content.Sha256, extension);
        BlobBucket bucket = pending.AudiencePermission is null ? BlobBucket.Public : BlobBucket.Private;
        if (await blobs.FindAsync(bucket, key, cancellationToken) is null)
        {
            await blobs.CopyAsync(BlobBucket.Private, pending.StagingKey, bucket, key,
                new BlobHeaders(pending.ContentType, ContentAddress.ImmutableCacheControl), cancellationToken);
        }

        MediaItem item = new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            ContentType = pending.ContentType,
            Bytes = content.Bytes,
            Width = content.Dimensions.Width,
            Height = content.Dimensions.Height,
            FileName = pending.FileName,
            AltText = pending.AltText,
            AudiencePermission = pending.AudiencePermission,
            AudienceScopeId = pending.AudienceScopeId,
            UploadedBy = caller,
            UploadedAt = Now,
            FolderId = pending.FolderId is { } folder && await db.Folders.AnyAsync(existing => existing.Id == folder, cancellationToken) ? folder : null,
        };
        db.Items.Add(item);
        Mark(item.Id);
        await DiscardAsync(pending, cancellationToken);
        return MediaRefs.Of(item, blobs);
    }

    public async Task<MediaRef> StoreAsync(ClaimsPrincipal user, byte[] bytes, string contentType, string fileName, CancellationToken cancellationToken)
    {
        using MemoryStream content = new(bytes);
        InspectedContent inspected = await MediaInspector.InspectAsync(content, contentType, cancellationToken);
        if (!inspected.MatchesType)
            throw new RpcException(new Status(StatusCode.Unavailable, $"That download isn't really {contentType}."));

        MediaTypes.TryGetExtension(contentType, out string extension);
        string key = ContentAddress.KeyFor(inspected.Sha256, extension);
        if (await db.Items.AsNoTracking().FirstOrDefaultAsync(item => item.Key == key && item.AudiencePermission == null, cancellationToken) is { } existing)
            return MediaRefs.Of(existing, blobs);

        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
        {
            content.Position = 0;
            await blobs.PutAsync(BlobBucket.Public, key, content, new BlobHeaders(contentType, ContentAddress.ImmutableCacheControl), cancellationToken);
        }

        MediaItem item = new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            ContentType = contentType,
            Bytes = inspected.Bytes,
            FileName = fileName,
            UploadedBy = CallerId(user),
            UploadedAt = Now,
        };
        db.Items.Add(item);
        Mark(item.Id);
        await db.SaveChangesAsync(cancellationToken);
        return MediaRefs.Of(item, blobs);
    }

    public async Task<MediaLink> GetLinkAsync(ClaimsPrincipal user, string id, CancellationToken cancellationToken)
    {
        MediaItem item = await db.Items.AsNoTracking().SingleOrDefaultAsync(item => item.Id == ParseId(id), cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such media."));

        if (item.AudiencePermission is not { } permission)
            return new MediaLink { Url = blobs.PublicUrlOf(item.Key).ToString(), ExpiresAt = DateTime.MaxValue.ToUniversalTime() };

        if (!await access.IsAllowedAsync(user, permission, item.AudienceScopeId, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Not permitted."));

        DateTime expiresAt = Now + PrivateLinkLifetime;
        Uri url = await blobs.PresignDownloadAsync(BlobBucket.Private, item.Key, PrivateLinkLifetime);
        return new MediaLink { Url = url.ToString(), ExpiresAt = expiresAt };
    }

    public async Task<MediaList> ListAsync(ClaimsPrincipal user, MediaQuery query, CancellationToken cancellationToken)
    {
        int take = query.Take is > 0 and <= MaximumPage ? query.Take : MaximumPage;
        IQueryable<MediaItem> scope = await ScopeAsync(user, query, cancellationToken);
        MediaCounts counts = new()
        {
            All = await scope.CountAsync(cancellationToken),
            Pictures = await Filtered(scope, MediaFilter.Pictures).CountAsync(cancellationToken),
            Documents = await Filtered(scope, MediaFilter.Documents).CountAsync(cancellationToken),
            Unused = await Filtered(scope, MediaFilter.Unused).CountAsync(cancellationToken),
        };

        List<Listed> items = await Filtered(scope, query.Filter)
            .OrderByDescending(item => item.UploadedAt)
            .Skip(Math.Max(0, query.Skip))
            .Take(take + 1)
            .Select(item => new Listed(item, db.Uses.Count(use => use.MediaId == item.Id)))
            .ToListAsync(cancellationToken);

        return new MediaList
        {
            HasMore = items.Count > take,
            Counts = counts,
            Items = items.Take(take).Select(row => new MediaItemInfo
            {
                Media = MediaRefs.Of(row.Item, blobs),
                Uses = row.Uses,
                UploadedAt = row.Item.UploadedAt,
                UploadedBy = row.Item.UploadedBy.ToString(),
                PrivateTo = row.Item.AudiencePermission is null ? null : new MediaAudience { Permission = row.Item.AudiencePermission, ScopeId = row.Item.AudienceScopeId },
                FolderId = row.Item.FolderId?.ToString(),
            }).ToList(),
        };
    }

    private async Task<IQueryable<MediaItem>> ScopeAsync(ClaimsPrincipal user, MediaQuery query, CancellationToken cancellationToken)
    {
        IQueryable<MediaItem> items = db.Items.AsNoTracking();
        Guid[] hidden = [.. await folders.HiddenAsync(user, cancellationToken)];
        string search = query.Search.Trim();
        if (search.Length > 0)
        {
            string pattern = $"%{search.Replace("%", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal)}%";
            return items
                .Where(item => item.FolderId == null || !hidden.Contains(item.FolderId.Value))
                .Where(item => EF.Functions.ILike(item.FileName, pattern) || EF.Functions.ILike(item.AltText, pattern));
        }

        if (string.IsNullOrEmpty(query.FolderId))
            return items.Where(item => item.FolderId == null);

        Guid folderId = Guid.TryParse(query.FolderId, out Guid parsed) && await db.Folders.AnyAsync(folder => folder.Id == parsed, cancellationToken)
            ? parsed
            : throw new RpcException(new Status(StatusCode.NotFound, "That folder has been deleted."));
        if (hidden.Contains(folderId))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only people it was shared with can see what's in this folder."));
        return items.Where(item => item.FolderId == folderId);
    }

    private IQueryable<MediaItem> Filtered(IQueryable<MediaItem> items, MediaFilter filter) => filter switch
    {
        MediaFilter.Pictures => items.Where(item => item.ContentType.StartsWith("image/")),
        MediaFilter.Documents => items.Where(item => !item.ContentType.StartsWith("image/")),
        MediaFilter.Unused => items.Where(item => !db.Uses.Any(use => use.MediaId == item.Id)),
        _ => items,
    };

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        Guid mediaId = ParseId(id);
        MediaItem item = await db.Items.SingleOrDefaultAsync(item => item.Id == mediaId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such media."));

        if (item.FolderId is { } folderId && await db.Folders.AnyAsync(folder => folder.Id == folderId && folder.SystemArea != null, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Files the app keeps for itself can't be deleted here."));

        int uses = await db.Uses.CountAsync(use => use.MediaId == mediaId, cancellationToken);
        if (uses > 0)
            throw InUse(uses);

        db.Items.Remove(item);
        Mark(item.Id, deleted: true);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw InUse(await db.Uses.CountAsync(use => use.MediaId == mediaId, cancellationToken));
        }

        bool bytesStillListed = await db.Items.AnyAsync(other => other.Key == item.Key && (other.AudiencePermission == null) == (item.AudiencePermission == null), cancellationToken);
        if (!bytesStillListed)
            await blobs.DeleteAsync(item.IsPrivate ? BlobBucket.Private : BlobBucket.Public, item.Key, cancellationToken);
    }

    private static RpcException InUse(int uses) =>
        new(new Status(StatusCode.FailedPrecondition, uses == 1 ? "It's still used in 1 place. Remove it there first." : $"It's still used in {uses} places. Remove it there first."));

    private async Task CheckAudienceAsync(ClaimsPrincipal user, MediaAudience audience, CancellationToken cancellationToken)
    {
        if (!catalog.ByNode.ContainsKey(audience.Permission))
            throw Invalid($"'{audience.Permission}' isn't a permission.");
        if (!await access.IsAllowedAsync(user, audience.Permission, NullIfEmpty(audience.ScopeId), cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You can only make a file private to people you're one of."));
    }

    private async Task DiscardAsync(PendingUpload pending, CancellationToken cancellationToken)
    {
        db.PendingUploads.Remove(pending);
        await db.SaveChangesAsync(cancellationToken);
        await blobs.DeleteAsync(BlobBucket.Private, pending.StagingKey, cancellationToken);
    }

    private void Mark(Guid mediaId, bool deleted = false) => db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, mediaId.ToString(), Now, deleted));

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static Guid CallerId(ClaimsPrincipal user) =>
        Caller.UserId(user) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw new RpcException(new Status(StatusCode.NotFound, "No such media."));

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private sealed record Listed(MediaItem Item, int Uses);
}
