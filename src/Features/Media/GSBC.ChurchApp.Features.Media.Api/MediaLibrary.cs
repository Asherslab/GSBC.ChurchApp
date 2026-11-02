using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaLibrary(
    MediaDbContext db, IBlobStore blobs, IAccessEvaluator access, PermissionCatalog catalog, TimeProvider time)
{
    public static readonly TimeSpan UploadWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PrivateLinkLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PendingKeptFor = TimeSpan.FromDays(1);
    private const int MaximumPage = 200;
    private const int MaximumTextLength = 200;

    public async Task<UploadTicket> BeginUploadAsync(ClaimsPrincipal user, BeginUploadRequest request, CancellationToken cancellationToken)
    {
        if (!MediaTypes.TryGetExtension(request.ContentType, out _))
            throw Invalid($"'{request.ContentType}' files can't be uploaded. Use one of: {string.Join(", ", MediaTypes.Accepted)}.");
        if (request.FileName.Length > MaximumTextLength || request.AltText.Length > MaximumTextLength)
            throw Invalid("Keep the file name and description under 200 characters.");
        if (request.PrivateTo is { } audience)
            await CheckAudienceAsync(user, audience, cancellationToken);

        DateTime now = Now;
        await db.PendingUploads.Where(upload => upload.ExpiresAt < now - PendingKeptFor).ExecuteDeleteAsync(cancellationToken);

        PendingUpload pending = new()
        {
            Id = Guid.CreateVersion7(),
            UploadedBy = CallerId(user),
            ContentType = request.ContentType.ToLowerInvariant(),
            FileName = request.FileName.Trim(),
            AltText = request.AltText.Trim(),
            Width = Math.Max(0, request.Width),
            Height = Math.Max(0, request.Height),
            AudiencePermission = request.PrivateTo?.Permission,
            AudienceScopeId = NullIfEmpty(request.PrivateTo?.ScopeId),
            ExpiresAt = now + UploadWindow,
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
            Width = pending.Width,
            Height = pending.Height,
            FileName = pending.FileName,
            AltText = pending.AltText,
            AudiencePermission = pending.AudiencePermission,
            AudienceScopeId = pending.AudienceScopeId,
            UploadedBy = caller,
            UploadedAt = Now,
        };
        db.Items.Add(item);
        await DiscardAsync(pending, cancellationToken);
        return RefOf(item);
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

    public async Task<MediaList> ListAsync(MediaQuery query, CancellationToken cancellationToken)
    {
        int take = query.Take is > 0 and <= MaximumPage ? query.Take : MaximumPage;
        List<MediaItem> items = await db.Items.AsNoTracking()
            .OrderByDescending(item => item.UploadedAt)
            .Skip(Math.Max(0, query.Skip))
            .Take(take + 1)
            .ToListAsync(cancellationToken);

        return new MediaList
        {
            HasMore = items.Count > take,
            Items = items.Take(take).Select(item => new MediaItemInfo
            {
                Media = RefOf(item),
                UploadedAt = item.UploadedAt,
                UploadedBy = item.UploadedBy.ToString(),
                PrivateTo = item.AudiencePermission is null ? null : new MediaAudience { Permission = item.AudiencePermission, ScopeId = item.AudienceScopeId },
            }).ToList(),
        };
    }

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

    private MediaRef RefOf(MediaItem item) => new()
    {
        Id = item.Id.ToString(),
        Key = item.Key,
        ContentType = item.ContentType,
        Bytes = item.Bytes,
        Width = item.Width,
        Height = item.Height,
        Url = item.IsPrivate ? null : blobs.PublicUrlOf(item.Key).ToString(),
        IsPrivate = item.IsPrivate,
        AltText = item.AltText,
        FileName = item.FileName,
    };

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static Guid CallerId(ClaimsPrincipal user) =>
        Caller.UserId(user) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw new RpcException(new Status(StatusCode.NotFound, "No such media."));

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
