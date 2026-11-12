using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryFileMedia(IMediaReferences references, IMediaSystemFolders folders, IMediaAudiences audiences)
{
    public static readonly MediaSystemArea Area = new(MinistryFileArea.Key, "Ministry files", "Made by the app: documents leaders add to their ministries",
        MinistriesPermissions.TeamView, StaffDeleteUnused: true, Uploaders: MinistriesPermissions.Post);

    public async Task<Guid?> VerifyAsync(Guid? mediaId, CancellationToken cancellationToken) =>
        mediaId is { } id ? Guid.Parse((await references.VerifyAsync([new MediaRef { Id = id.ToString() }], cancellationToken))[0].Id) : null;

    public async Task UseAsync(Guid fileId, Guid? mediaId, Guid ministryId, string ministryName, CancellationToken cancellationToken)
    {
        List<MediaRef> used = mediaId is { } id ? [new MediaRef { Id = id.ToString() }] : [];
        await references.UseAsync(Owner(fileId), used, cancellationToken);
        if (used.Count > 0)
            await folders.FileAsync(Area, ministryId.ToString(), ministryName, used, cancellationToken);
    }

    public async Task ReleaseAsync(IEnumerable<Guid> fileIds, CancellationToken cancellationToken)
    {
        foreach (Guid fileId in fileIds)
            await references.UseAsync(Owner(fileId), [], cancellationToken);
    }

    public Task<bool> ShareAsync(Guid fileId, Guid mediaId, string audience, Guid ministryId, CancellationToken cancellationToken) =>
        audiences.ShareAsync(Area, mediaId.ToString(), Owner(fileId), Audiences.NodeOf(audience), ministryId.ToString(), cancellationToken);

    private static string Owner(Guid fileId) => $"ministries.file/{fileId}";
}
