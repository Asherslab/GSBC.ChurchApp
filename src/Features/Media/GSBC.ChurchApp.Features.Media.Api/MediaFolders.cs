using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaFolders(MediaDbContext db, IAccessEvaluator access, IEnumerable<MediaSystemArea> areas, IBlobStore blobs, MediaSizer sizer, TimeProvider time) : IMediaSystemFolders
{
    public const int MaximumNameLength = 80;
    private const string MadeByTheApp = "Made by the app.";

    private readonly Dictionary<string, MediaSystemArea> _areas = areas.ToDictionary(area => area.Key);

    public async Task<FolderTree> ListAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<MediaFolder> folders = await db.Folders.AsNoTracking().ToListAsync(cancellationToken);
        Dictionary<Guid, int> files = await db.Items.AsNoTracking()
            .Where(item => item.FolderId != null)
            .GroupBy(item => item.FolderId!.Value)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        HashSet<Guid> hidden = await HiddenAsync(user, folders, cancellationToken);

        return new FolderTree
        {
            Folders = folders
                .Where(folder => !hidden.Contains(folder.Id) || folder.ParentId is null)
                .OrderBy(folder => folder.IsSystem)
                .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(folder => new FolderInfo
                {
                    Id = folder.Id.ToString(),
                    ParentId = folder.ParentId?.ToString(),
                    Name = folder.Name,
                    IsSystem = folder.IsSystem,
                    Caption = folder is { IsSystem: true, ParentId: null } ? AreaOf(folder)?.Caption ?? MadeByTheApp : "",
                    Files = hidden.Contains(folder.Id) ? 0 : files.GetValueOrDefault(folder.Id),
                    CanOpen = !hidden.Contains(folder.Id),
                    UnusedDeletable = folder.IsSystem && AreaOf(folder) is { StaffDeleteUnused: true },
                })
                .ToList(),
        };
    }

    public async Task<HashSet<Guid>> HiddenAsync(ClaimsPrincipal user, CancellationToken cancellationToken) =>
        await HiddenAsync(user, await db.Folders.AsNoTracking().Where(folder => folder.SystemArea != null).ToListAsync(cancellationToken), cancellationToken);

    public Task<Guid?> UploadFolderAsync(string? id, CancellationToken cancellationToken) => StaffFolderAsync(id, cancellationToken);

    public MediaSystemArea? Area(string key) => _areas.GetValueOrDefault(key);

    public async Task<Guid> AreaFolderAsync(MediaSystemArea area, CancellationToken cancellationToken) =>
        (await SystemFolderAsync(area.Key, null, area.Name, null, cancellationToken)).Id;

    public async Task<FolderInfo> CreateAsync(ClaimsPrincipal user, CreateFolderRequest request, CancellationToken cancellationToken)
    {
        string name = NameOf(request.Name);
        Guid? parentId = await StaffFolderAsync(request.ParentId, cancellationToken);
        await EnsureNameFreeAsync(parentId, name, except: null, cancellationToken);

        MediaFolder folder = new() { Id = Guid.CreateVersion7(), ParentId = parentId, Name = name, CreatedBy = CallerId(user), CreatedAt = Now };
        db.Folders.Add(folder);
        Mark(folder.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new FolderInfo { Id = folder.Id.ToString(), ParentId = parentId?.ToString(), Name = name, CanOpen = true };
    }

    public async Task RenameAsync(RenameFolderRequest request, CancellationToken cancellationToken)
    {
        MediaFolder folder = await StaffFolderToChangeAsync(request.Id, cancellationToken);
        string name = NameOf(request.Name);
        await EnsureNameFreeAsync(folder.ParentId, name, folder.Id, cancellationToken);
        folder.Name = name;
        Mark(folder.Id);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveAsync(MoveRequest request, CancellationToken cancellationToken)
    {
        Guid? into = await StaffFolderAsync(request.Into, cancellationToken);
        List<Guid> itemIds = request.ItemIds.Select(ParseId).ToList();
        List<Guid> folderIds = request.FolderIds.Select(ParseId).ToList();

        List<MediaItem> items = await db.Items.Where(item => itemIds.Contains(item.Id)).ToListAsync(cancellationToken);
        if (items.Count != itemIds.Distinct().Count())
            throw new RpcException(new Status(StatusCode.NotFound, "A file you chose has been deleted. Choose again."));
        HashSet<Guid> systemFolders = [.. await db.Folders.Where(folder => folder.SystemArea != null).Select(folder => folder.Id).ToListAsync(cancellationToken)];
        CheckMovable(items, systemFolders);

        List<MediaFolder> folders = [];
        foreach (Guid id in folderIds)
        {
            MediaFolder folder = await StaffFolderToChangeAsync(id.ToString(), cancellationToken);
            if (into is { } target && await IsWithinAsync(target, folder.Id, cancellationToken))
                throw Invalid($"'{folder.Name}' can't go inside itself.");
            if (folder.ParentId != into)
                await EnsureNameFreeAsync(into, folder.Name, folder.Id, cancellationToken);
            folders.Add(folder);
        }

        foreach (MediaItem item in items.Where(item => item.FolderId != into))
        {
            item.FolderId = into;
            db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, item.Id.ToString(), Now));
        }
        foreach (MediaFolder folder in folders.Where(folder => folder.ParentId != into))
        {
            folder.ParentId = into;
            Mark(folder.Id);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static void CheckMovable(IEnumerable<MediaItem> items, IReadOnlySet<Guid> systemFolders)
    {
        if (items.Any(item => item.FolderId is { } folder && systemFolders.Contains(folder)))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Files the app keeps for itself can't be moved."));
    }

    public async Task DeleteAsync(DeleteFolderRequest request, CancellationToken cancellationToken)
    {
        MediaFolder folder = await StaffFolderToChangeAsync(request.Id, cancellationToken);
        List<MediaItem> files = await db.Items.Where(item => item.FolderId == folder.Id).ToListAsync(cancellationToken);
        List<MediaFolder> subfolders = await db.Folders.Where(child => child.ParentId == folder.Id).ToListAsync(cancellationToken);

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (files.Count + subfolders.Count > 0)
        {
            if (!request.MoveContents)
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"'{folder.Name}' isn't empty: {Contents(files.Count, subfolders.Count)}. Choose where they go first."));
            if (await StaffFolderAsync(request.MoveContentsInto, cancellationToken) is { } into && await IsWithinAsync(into, folder.Id, cancellationToken))
                throw Invalid($"Its contents can't go inside '{folder.Name}' while it's being deleted. Choose another folder.");
            await MoveAsync(new MoveRequest
            {
                ItemIds = [.. files.Select(item => item.Id.ToString())],
                FolderIds = [.. subfolders.Select(child => child.Id.ToString())],
                Into = request.MoveContentsInto,
            }, cancellationToken);
        }

        db.Folders.Remove(folder);
        db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, FolderKey(folder.Id), Now, deleted: true));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new RpcException(new Status(StatusCode.Aborted, $"Something was just added to '{folder.Name}'. Try again."));
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task FileAsync(MediaSystemArea area, string? scopeId, string scopeName, IReadOnlyCollection<MediaRef> media, CancellationToken cancellationToken)
    {
        MediaFolder root = await SystemFolderAsync(area.Key, null, area.Name, null, cancellationToken);
        MediaFolder folder = scopeId is null ? root : await SystemFolderAsync(area.Key, scopeId, scopeName, root.Id, cancellationToken);

        List<Guid> ids = media.Select(item => ParseId(item.Id)).ToList();
        List<MediaItem> items = await db.Items.Where(item => ids.Contains(item.Id) && item.FolderId != folder.Id).ToListAsync(cancellationToken);
        foreach (MediaItem item in items)
        {
            item.FolderId = folder.Id;
            db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, item.Id.ToString(), Now));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task NameAsync(MediaSystemArea area, string mediaId, string fileName, CancellationToken cancellationToken)
    {
        Guid id = ParseId(mediaId);
        MediaFolder folder = await SystemFolderAsync(area.Key, null, area.Name, null, cancellationToken);
        if (await db.Items.SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.FolderId == folder.Id, cancellationToken) is not { } item || item.FileName == fileName)
            return;
        item.FileName = fileName;
        db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, item.Id.ToString(), Now));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgetAsync(MediaSystemArea area, IReadOnlyCollection<string> mediaIds, CancellationToken cancellationToken)
    {
        List<Guid> ids = mediaIds.Select(id => Guid.TryParse(id, out Guid parsed) ? parsed : Guid.Empty).Where(id => id != Guid.Empty).ToList();
        if (ids.Count > 0)
            await ForgetWhereAsync(db.Items.Where(item => item.Area == area.Key && ids.Contains(item.Id)), cancellationToken);
    }

    public Task ForgetUnusedAsync(MediaSystemArea area, DateTime uploadedBefore, CancellationToken cancellationToken) =>
        ForgetWhereAsync(db.Items.Where(item => item.Area == area.Key && item.UploadedAt < uploadedBefore), cancellationToken);

    private async Task ForgetWhereAsync(IQueryable<MediaItem> candidates, CancellationToken cancellationToken)
    {
        List<MediaItem> items = await candidates.Where(item => !db.Uses.Any(use => use.MediaId == item.Id)).ToListAsync(cancellationToken);
        if (items.Count == 0)
            return;
        List<Guid> ids = items.Select(item => item.Id).ToList();
        List<string> renditions = await db.Renditions.Where(rendition => ids.Contains(rendition.MediaId)).Select(rendition => rendition.Key).ToListAsync(cancellationToken);
        foreach (MediaItem item in items)
        {
            db.Items.Remove(item);
            db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, item.Id.ToString(), Now, deleted: true));
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (IGrouping<bool, MediaItem> privacy in items.GroupBy(item => item.IsPrivate))
            await MediaRemoval.DeleteUnlistedAsync(db, blobs, privacy.Key, [.. privacy.Select(item => item.Key), .. privacy.Key ? renditions : []], cancellationToken);
    }

    public async Task<MediaRef> KeepAsync(MediaSystemArea area, KeptFile file, CancellationToken cancellationToken)
    {
        MediaFolder folder = await SystemFolderAsync(area.Key, null, area.Name, null, cancellationToken);
        string sha256 = ContentAddress.Sha256Of(file.Bytes);
        string key = ContentAddress.KeyFor(sha256, file.Extension);
        if (await db.Items.AsNoTracking().FirstOrDefaultAsync(item => item.Key == key && item.FolderId == folder.Id, cancellationToken) is { } kept)
            return await MediaRefs.OfAsync(db, kept, blobs, cancellationToken);

        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
            await blobs.PutAsync(BlobBucket.Public, key, new MemoryStream(file.Bytes), new BlobHeaders(file.ContentType, ContentAddress.ImmutableCacheControl), cancellationToken);
        MediaItem item = new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            Sha256 = sha256,
            ContentType = file.ContentType,
            Bytes = file.Bytes.Length,
            Width = file.Width,
            Height = file.Height,
            FileName = file.FileName,
            UploadedAt = Now,
            FolderId = folder.Id,
        };
        await sizer.SizeAsync(item, _ => Task.FromResult(file.Bytes), cancellationToken);
        db.Items.Add(item);
        Mark(item.Id);
        await db.SaveChangesAsync(cancellationToken);
        return await MediaRefs.OfAsync(db, item, blobs, cancellationToken);
    }

    public async Task<MediaRef> KeepStoredAsync(MediaSystemArea area, StoredFile file, CancellationToken cancellationToken)
    {
        MediaFolder folder = await SystemFolderAsync(area.Key, null, area.Name, null, cancellationToken);
        if (await db.Items.AsNoTracking().FirstOrDefaultAsync(item => item.Sha256 == file.Sha256 && item.FolderId == folder.Id, cancellationToken) is { } kept)
            return await MediaRefs.OfAsync(db, kept, blobs, cancellationToken);
        if (await blobs.FindAsync(BlobBucket.Public, file.Key, cancellationToken) is null)
            throw new InvalidOperationException($"There's no stored file at {file.Key} to keep.");

        MediaItem item = new()
        {
            Id = Guid.CreateVersion7(),
            Key = file.Key,
            Sha256 = file.Sha256,
            ContentType = file.ContentType,
            Bytes = file.Bytes,
            FileName = file.FileName,
            UploadedAt = Now,
            FolderId = folder.Id,
        };
        await sizer.SizeStoredAsync(item, cancellationToken);
        db.Items.Add(item);
        Mark(item.Id);
        await db.SaveChangesAsync(cancellationToken);
        return await MediaRefs.OfAsync(db, item, blobs, cancellationToken);
    }

    private async Task<MediaFolder> SystemFolderAsync(string area, string? scope, string name, Guid? parentId, CancellationToken cancellationToken)
    {
        string trimmed = name.Trim() is { Length: > 0 } given ? given[..Math.Min(given.Length, MaximumNameLength)] : "Untitled";
        if (await db.Folders.SingleOrDefaultAsync(folder => folder.SystemArea == area && folder.SystemScope == scope, cancellationToken) is { } existing)
        {
            if (existing.Name != trimmed)
            {
                existing.Name = trimmed;
                Mark(existing.Id);
                await db.SaveChangesAsync(cancellationToken);
            }
            return existing;
        }

        MediaFolder created = new() { Id = Guid.CreateVersion7(), ParentId = parentId, Name = trimmed, SystemArea = area, SystemScope = scope, CreatedAt = Now };
        db.Folders.Add(created);
        Mark(created.Id);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return created;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return await db.Folders.SingleAsync(folder => folder.SystemArea == area && folder.SystemScope == scope, cancellationToken);
        }
    }

    private Task<HashSet<Guid>> HiddenAsync(ClaimsPrincipal user, IReadOnlyCollection<MediaFolder> folders, CancellationToken cancellationToken) =>
        HiddenAsync(access, _areas, user, folders, cancellationToken);

    internal static async Task<HashSet<Guid>> HiddenAsync(
        IAccessEvaluator access, IReadOnlyDictionary<string, MediaSystemArea> areas, ClaimsPrincipal user, IReadOnlyCollection<MediaFolder> folders, CancellationToken cancellationToken)
    {
        List<MediaFolder> system = folders.Where(folder => folder.IsSystem).ToList();
        if (system.Count == 0 || await access.IsAllowedAsync(user, MediaPermissions.SystemViewNode, null, cancellationToken))
            return [];

        HashSet<Guid> hidden = [];
        foreach (MediaFolder folder in system)
        {
            bool inAudience = AreaOf(areas, folder) is { } area && await access.IsAllowedAsync(user, area.Audience.Node, folder.SystemScope, cancellationToken);
            if (!inAudience)
                hidden.Add(folder.Id);
        }
        return hidden;
    }

    private MediaSystemArea? AreaOf(MediaFolder folder) => AreaOf(_areas, folder);

    public HashSet<string> StaffDeleteUnusedAreas() => [.. _areas.Values.Where(area => area.StaffDeleteUnused).Select(area => area.Key)];

    private static MediaSystemArea? AreaOf(IReadOnlyDictionary<string, MediaSystemArea> areas, MediaFolder folder) =>
        folder.SystemArea is { } key ? areas.GetValueOrDefault(key) : null;

    private async Task<Guid?> StaffFolderAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        return StaffFolder(await FolderAsync(id, cancellationToken));
    }

    internal static Guid StaffFolder(MediaFolder folder) =>
        folder.IsSystem
            ? throw new RpcException(new Status(StatusCode.FailedPrecondition, $"'{folder.Name}' is kept by the app. Choose one of your own folders."))
            : folder.Id;

    private async Task<MediaFolder> StaffFolderToChangeAsync(string id, CancellationToken cancellationToken) =>
        StaffFolderToChange(await FolderAsync(id, cancellationToken));

    internal static MediaFolder StaffFolderToChange(MediaFolder folder) =>
        folder.IsSystem
            ? throw new RpcException(new Status(StatusCode.FailedPrecondition, $"'{folder.Name}' is kept by the app, so it can't be renamed, moved or deleted."))
            : folder;

    private async Task<MediaFolder> FolderAsync(string id, CancellationToken cancellationToken)
    {
        Guid folderId = Guid.TryParse(id, out Guid parsed) ? parsed : throw NoSuchFolder();
        return await db.Folders.SingleOrDefaultAsync(folder => folder.Id == folderId, cancellationToken) ?? throw NoSuchFolder();
    }

    private async Task<bool> IsWithinAsync(Guid folderId, Guid ancestorId, CancellationToken cancellationToken) =>
        IsWithin(await db.Folders.AsNoTracking().ToDictionaryAsync(folder => folder.Id, folder => folder.ParentId, cancellationToken), folderId, ancestorId);

    internal static bool IsWithin(IReadOnlyDictionary<Guid, Guid?> parents, Guid folderId, Guid ancestorId)
    {
        for (Guid? current = folderId; current is { } id; current = parents.GetValueOrDefault(id))
        {
            if (id == ancestorId)
                return true;
        }
        return false;
    }

    private async Task EnsureNameFreeAsync(Guid? parentId, string name, Guid? except, CancellationToken cancellationToken)
    {
        bool taken = await db.Folders.AnyAsync(folder => folder.ParentId == parentId && folder.Id != except && folder.Name.ToLower() == name.ToLower(), cancellationToken);
        if (taken)
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"There's already a folder called '{name}' there."));
    }

    private static string NameOf(string requested)
    {
        string name = requested.Trim();
        if (name.Length == 0)
            throw Invalid("Give the folder a name.");
        if (name.Length > MaximumNameLength)
            throw Invalid($"Keep folder names under {MaximumNameLength} characters.");
        return name;
    }

    private static string Contents(int files, int folders) => (files, folders) switch
    {
        (_, 0) => files == 1 ? "1 file" : $"{files} files",
        (0, _) => folders == 1 ? "1 folder" : $"{folders} folders",
        _ => $"{(files == 1 ? "1 file" : $"{files} files")} and {(folders == 1 ? "1 folder" : $"{folders} folders")}",
    };

    private void Mark(Guid folderId) => db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, FolderKey(folderId), Now));

    private static string FolderKey(Guid folderId) => $"folder/{folderId}";

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static Guid CallerId(ClaimsPrincipal user) =>
        Caller.UserId(user) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw new RpcException(new Status(StatusCode.NotFound, "No such media."));

    private static RpcException NoSuchFolder() => new(new Status(StatusCode.NotFound, "That folder has been deleted."));

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
