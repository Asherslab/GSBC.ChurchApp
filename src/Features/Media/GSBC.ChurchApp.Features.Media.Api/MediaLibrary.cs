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
    MediaDbContext db, MediaFolders folders, MediaSizer sizer, MediaHolders holders, IBlobStore blobs, IAccessEvaluator access, PermissionCatalog catalog, TimeProvider time)
{
    private static readonly TimeSpan UploadWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PrivateLinkLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PendingKeptFor = TimeSpan.FromDays(1);
    private const int MaximumPage = 200;

    public async Task<UploadTicket> BeginUploadAsync(ClaimsPrincipal user, BeginUploadRequest request, CancellationToken cancellationToken)
    {
        bool knownAudience = request.PrivateTo is { } audience && catalog.ByNode.ContainsKey(audience.Permission);
        bool amongAudience = knownAudience && await access.IsAllowedAsync(user, request.PrivateTo!.Permission, NullIfEmpty(request.PrivateTo.ScopeId), cancellationToken);
        bool mayUploadFonts = MediaTypes.IsFont(request.ContentType) && await access.IsAllowedAsync(user, MediaPermissions.UploadFontsNode, null, cancellationToken);
        CheckUpload(request, knownAudience, amongAudience, mayUploadFonts);

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

    internal static void CheckUpload(BeginUploadRequest request, bool knownAudience, bool amongAudience, bool mayUploadFonts)
    {
        if (!MediaTypes.TryGetExtension(request.ContentType, out _))
            throw Invalid($"'{request.ContentType}' files can't be uploaded. Use one of: {string.Join(", ", MediaTypes.Accepted)}.");
        if (request.FileName.Length > MediaRefs.MaximumTextLength || request.AltText.Length > MediaRefs.MaximumTextLength)
            throw Invalid("Keep the file name and description under 200 characters.");
        if (request.PrivateTo is { } audience && !knownAudience)
            throw Invalid($"'{audience.Permission}' isn't a permission.");
        if (request.PrivateTo is not null && !amongAudience)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You can only make a file private to people you're one of."));
        if (MediaTypes.IsFont(request.ContentType) && !mayUploadFonts)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Uploading font files needs permission to, since the church must be licensed to use the font in an app. Choose one from Google Fonts instead."));
        if (MediaTypes.IsFont(request.ContentType) && request.PrivateTo is not null)
            throw Invalid("Fonts are public: anyone using the app downloads them.");
    }

    public async Task<UploadedMedia> CompleteUploadAsync(ClaimsPrincipal user, CompleteUploadRequest request, CancellationToken cancellationToken)
    {
        Guid caller = CallerId(user);
        PendingUpload pending = await db.PendingUploads
            .SingleOrDefaultAsync(upload => upload.Id == ParseId(request.UploadId) && upload.UploadedBy == caller, cancellationToken)
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

        if (!request.AddAgain)
        {
            List<MediaItem> same = await SameContentAsync(user, content.Sha256, pending.ContentType, pending.AudiencePermission, pending.AudienceScopeId, cancellationToken);
            SameContentChoice choice = Choose(same, request.PutBackInto);
            if (choice.Kept is { } match)
            {
                await DiscardAsync(pending, cancellationToken);
                return new UploadedMedia { Item = await InfoOfAsync(match, cancellationToken), AlreadyInLibrary = true };
            }
            if (choice.PutBack is { } putBack)
                return await PutBackAsync(pending, putBack.Id, cancellationToken);
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
            Sha256 = content.Sha256,
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
        await sizer.SizeAsync(item, token => MediaSizer.BytesOfAsync(blobs, BlobBucket.Private, pending.StagingKey, token), cancellationToken);
        db.Items.Add(item);
        Mark(item.Id);
        await DiscardAsync(pending, cancellationToken);
        return new UploadedMedia { Item = await InfoOfAsync(item, cancellationToken) };
    }

    internal static SameContentChoice Choose(IReadOnlyList<MediaItem> same, string? putBackInto)
    {
        if (same.FirstOrDefault(item => item.KeepsOriginal) is { } kept)
            return new SameContentChoice(kept, null);

        List<MediaItem> removed = same.Where(item => !item.KeepsOriginal).ToList();
        MediaItem? putBack = putBackInto is { Length: > 0 } wanted ? removed.FirstOrDefault(item => item.Id.ToString() == wanted) : removed.FirstOrDefault();
        if (putBack is null && putBackInto is { Length: > 0 })
            throw new RpcException(new Status(StatusCode.NotFound, "That picture isn't in the library any more. Add it as a new picture instead."));
        return new SameContentChoice(null, putBack);
    }

    private async Task<UploadedMedia> PutBackAsync(PendingUpload pending, Guid mediaId, CancellationToken cancellationToken)
    {
        MediaItem item = await db.Items.SingleAsync(item => item.Id == mediaId, cancellationToken);
        BlobBucket bucket = item.IsPrivate ? BlobBucket.Private : BlobBucket.Public;
        if (await blobs.FindAsync(bucket, item.Key, cancellationToken) is null)
            await blobs.CopyAsync(BlobBucket.Private, pending.StagingKey, bucket, item.Key, new BlobHeaders(item.ContentType, ContentAddress.ImmutableCacheControl), cancellationToken);

        item.OriginalRemovedAt = null;
        Mark(item.Id);
        await DiscardAsync(pending, cancellationToken);
        await holders.RefreshAsync([item.Id], cancellationToken);
        return new UploadedMedia { Item = await InfoOfAsync(item, cancellationToken), PutBack = true };
    }

    public async Task<MediaItemInfo> RemoveOriginalAsync(string id, CancellationToken cancellationToken)
    {
        Guid mediaId = ParseId(id);
        MediaItem item = await db.Items.SingleOrDefaultAsync(item => item.Id == mediaId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such media."));
        if (!item.KeepsOriginal)
            return await InfoOfAsync(item, cancellationToken);
        CheckOriginalCanGo(await db.Renditions.AsNoTracking().Where(rendition => rendition.MediaId == mediaId).ToListAsync(cancellationToken));
        if (!await holders.CanAllRefreshAsync(mediaId, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                "Somewhere that uses it, such as a draft that isn't published yet, can't switch to a smaller size by itself. Publish or close the draft first."));

        item.OriginalRemovedAt = Now;
        Mark(item.Id);
        await db.SaveChangesAsync(cancellationToken);
        await holders.RefreshAsync([item.Id], cancellationToken);
        await DeleteUnlistedAsync(item.IsPrivate, [item.Key], cancellationToken);
        return await InfoOfAsync(item, cancellationToken);
    }

    internal static void CheckOriginalCanGo(IReadOnlyCollection<MediaRendition> sizes)
    {
        if (sizes.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "It's already small, so the original is the only copy. There's nothing smaller to keep in its place."));
        if (!sizes.Any(size => size.Width >= MediaSizes.ForPhonesWidth))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "The app shows this original on full-width screens, so it stays."));
    }

    private async Task DeleteUnlistedAsync(bool isPrivate, IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys.Distinct())
        {
            bool listed = await db.Items.AnyAsync(other => other.Key == key && other.OriginalRemovedAt == null && (other.AudiencePermission == null) != isPrivate, cancellationToken)
                || await db.Renditions.AnyAsync(rendition => rendition.Key == key
                    && db.Items.Any(other => other.Id == rendition.MediaId && (other.AudiencePermission == null) != isPrivate), cancellationToken);
            if (!listed)
                await blobs.DeleteAsync(isPrivate ? BlobBucket.Private : BlobBucket.Public, key, cancellationToken);
        }
    }

    private Task<bool> InSystemFolderAsync(MediaItem item, CancellationToken cancellationToken) =>
        item.FolderId is { } folderId
            ? db.Folders.AnyAsync(folder => folder.Id == folderId && folder.SystemArea != null, cancellationToken)
            : Task.FromResult(false);

    public async Task<MediaMatches> FindByHashAsync(ClaimsPrincipal user, FindByHashRequest request, CancellationToken cancellationToken)
    {
        string sha256 = request.Sha256.Trim().ToLowerInvariant();
        if (!ContentAddress.IsSha256(sha256))
            throw Invalid("That isn't a SHA-256 hash.");

        List<MediaItem> matches = await SameContentAsync(user, sha256, request.ContentType.ToLowerInvariant(),
            NullIfEmpty(request.PrivateTo?.Permission), NullIfEmpty(request.PrivateTo?.ScopeId), cancellationToken);
        MediaMatches found = new();
        foreach (MediaItem match in matches)
            (match.KeepsOriginal ? found.Items : found.RemovedOriginals).Add(await InfoOfAsync(match, cancellationToken));
        return found;
    }

    private async Task<List<MediaItem>> SameContentAsync(
        ClaimsPrincipal user, string sha256, string contentType, string? audiencePermission, string? audienceScopeId, CancellationToken cancellationToken)
    {
        if (audiencePermission is not null && !await access.IsAllowedAsync(user, audiencePermission, audienceScopeId, cancellationToken))
            return [];

        Guid caller = CallerId(user);
        bool seesLibrary = await access.IsAllowedAsync(user, MediaPermissions.ViewNode, null, cancellationToken);
        List<MediaItem> sameBytes = await db.Items.AsNoTracking()
            .Where(item => item.Sha256 == sha256 && item.ContentType == contentType)
            .OrderBy(item => item.UploadedAt)
            .ToListAsync(cancellationToken);
        HashSet<Guid> systemFolders = [.. await db.Folders.AsNoTracking().Where(folder => folder.SystemArea != null).Select(folder => folder.Id).ToListAsync(cancellationToken)];
        return SameContent(sameBytes, audiencePermission, audienceScopeId, caller, seesLibrary, systemFolders);
    }

    internal static List<MediaItem> SameContent(
        IEnumerable<MediaItem> sameBytes, string? audiencePermission, string? audienceScopeId, Guid caller, bool seesLibrary, IReadOnlySet<Guid> systemFolders) =>
        sameBytes
            .Where(item => item.AudiencePermission == audiencePermission && item.AudienceScopeId == audienceScopeId)
            .Where(item => seesLibrary || item.UploadedBy == caller)
            .Where(item => item.FolderId is not { } folder || !systemFolders.Contains(folder))
            .ToList();

    private async Task<MediaItemInfo> InfoOfAsync(MediaItem item, CancellationToken cancellationToken) =>
        InfoOf(item, await db.Uses.CountAsync(use => use.MediaId == item.Id, cancellationToken), (await MediaRefs.RenditionsOfAsync(db, [item.Id], cancellationToken))[item.Id]);

    private MediaItemInfo InfoOf(MediaItem item, int uses, IReadOnlyCollection<MediaRendition> renditions) => new()
    {
        Media = MediaRefs.Of(item, renditions, blobs),
        Original = item.ContentType.StartsWith("image/", StringComparison.Ordinal) ? MediaRefs.OriginalOf(item) : null,
        Sizing = item.Sizing,
        Uses = uses,
        UploadedAt = item.UploadedAt,
        UploadedBy = item.UploadedBy.ToString(),
        PrivateTo = item.AudiencePermission is null ? null : new MediaAudience { Permission = item.AudiencePermission, ScopeId = item.AudienceScopeId },
        FolderId = item.FolderId?.ToString(),
    };

    public async Task<MediaRef> StoreAsync(ClaimsPrincipal user, byte[] bytes, string contentType, string fileName, CancellationToken cancellationToken)
    {
        using MemoryStream content = new(bytes);
        InspectedContent inspected = await MediaInspector.InspectAsync(content, contentType, cancellationToken);
        if (!inspected.MatchesType)
            throw new RpcException(new Status(StatusCode.Unavailable, $"That download isn't really {contentType}."));

        MediaTypes.TryGetExtension(contentType, out string extension);
        string key = ContentAddress.KeyFor(inspected.Sha256, extension);
        if (await db.Items.AsNoTracking()
                .Where(item => item.Sha256 == inspected.Sha256 && item.ContentType == contentType && item.AudiencePermission == null)
                .OrderBy(item => item.UploadedAt)
                .FirstOrDefaultAsync(cancellationToken) is { } existing)
            return await MediaRefs.OfAsync(db, existing, blobs, cancellationToken);

        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
        {
            content.Position = 0;
            await blobs.PutAsync(BlobBucket.Public, key, content, new BlobHeaders(contentType, ContentAddress.ImmutableCacheControl), cancellationToken);
        }

        MediaItem item = new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            Sha256 = inspected.Sha256,
            ContentType = contentType,
            Bytes = inspected.Bytes,
            FileName = fileName,
            UploadedBy = CallerId(user),
            UploadedAt = Now,
        };
        await sizer.SizeAsync(item, _ => Task.FromResult(bytes), cancellationToken);
        db.Items.Add(item);
        Mark(item.Id);
        await db.SaveChangesAsync(cancellationToken);
        return await MediaRefs.OfAsync(db, item, blobs, cancellationToken);
    }

    public async Task<MediaLink> GetLinkAsync(ClaimsPrincipal user, LinkRequest request, CancellationToken cancellationToken)
    {
        Guid mediaId = await MediaAliases.ResolveAsync(db, ParseId(request.Id), cancellationToken);
        MediaItem item = await db.Items.AsNoTracking().SingleOrDefaultAsync(item => item.Id == mediaId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such media."));
        MediaRef media = await MediaRefs.OfAsync(db, item, blobs, cancellationToken);
        string key = request.Key.Length == 0 || request.Key == media.Key || media.Sizes.Any(size => size.Key == request.Key)
            ? request.Key.Length == 0 ? media.Key : request.Key
            : throw new RpcException(new Status(StatusCode.NotFound, "That size of it isn't kept any more."));

        if (item.AudiencePermission is not { } permission)
            return new MediaLink { Url = blobs.PublicUrlOf(key).ToString(), ExpiresAt = DateTime.MaxValue.ToUniversalTime() };

        if (!await access.IsAllowedAsync(user, permission, item.AudienceScopeId, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Not permitted."));

        DateTime expiresAt = Now + PrivateLinkLifetime;
        Uri url = await blobs.PresignDownloadAsync(BlobBucket.Private, key, PrivateLinkLifetime);
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
            LargeOriginals = await Filtered(scope, MediaFilter.LargeOriginals).CountAsync(cancellationToken),
        };

        List<Listed> items = await Filtered(scope, query.Filter)
            .OrderByDescending(item => item.UploadedAt)
            .Skip(Math.Max(0, query.Skip))
            .Take(take + 1)
            .Select(item => new Listed(item, db.Uses.Count(use => use.MediaId == item.Id)))
            .ToListAsync(cancellationToken);

        List<Listed> shown = items.Take(take).ToList();
        Dictionary<Guid, List<MediaRendition>> renditions = await MediaRefs.RenditionsOfAsync(db, shown.Select(row => row.Item.Id), cancellationToken);
        return new MediaList
        {
            HasMore = items.Count > take,
            Counts = counts,
            Items = shown.Select(row => InfoOf(row.Item, row.Uses, renditions[row.Item.Id])).ToList(),
        };
    }

    private async Task<IQueryable<MediaItem>> ScopeAsync(ClaimsPrincipal user, MediaQuery query, CancellationToken cancellationToken)
    {
        IQueryable<MediaItem> items = db.Items.AsNoTracking();
        List<Guid> hidden = [.. await folders.HiddenAsync(user, cancellationToken)];
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
        MediaFilter.LargeOriginals => items.Where(item => item.OriginalRemovedAt == null && item.Bytes > MediaTypes.LargeOriginalBytes
            && db.Renditions.Any(rendition => rendition.MediaId == item.Id)),
        _ => items,
    };

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        Guid mediaId = ParseId(id);
        MediaItem item = await db.Items.SingleOrDefaultAsync(item => item.Id == mediaId, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such media."));

        if (await InSystemFolderAsync(item, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Files the app keeps for itself can't be deleted here."));

        int uses = await db.Uses.CountAsync(use => use.MediaId == mediaId, cancellationToken);
        if (uses > 0)
            throw InUse(uses);

        List<string> keys = [item.Key, .. await db.Renditions.Where(rendition => rendition.MediaId == mediaId).Select(rendition => rendition.Key).ToListAsync(cancellationToken)];
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

        await DeleteUnlistedAsync(item.IsPrivate, keys, cancellationToken);
    }

    private static RpcException InUse(int uses) =>
        new(new Status(StatusCode.FailedPrecondition, uses == 1 ? "It's still used in 1 place. Remove it there first." : $"It's still used in {uses} places. Remove it there first."));

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

internal sealed record SameContentChoice(MediaItem? Kept, MediaItem? PutBack);
