using System.Security.Claims;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachMedia(IMediaReferences references, IMediaSystemFolders folders, OutreachMinistries ministries)
{
    private const long LargestPhoto = 15 * 1024 * 1024;

    public static readonly MediaSystemArea ReportPhotos = new(OutreachAreas.ReportPhotos, "Outreach reports",
        "Made by the app: photos walkers add to a report about a place, deleted once the report is dealt with", OutreachPermissions.Review,
        Files: [new AreaFileType("image/jpeg", LargestPhoto), new AreaFileType("image/png", LargestPhoto), new AreaFileType("image/heic", LargestPhoto)]);

    private static string Owner(Guid report) => $"outreach.report/{report}";

    public async Task<MediaRef?> KeepAsync(Guid report, MediaRef? photo, CancellationToken cancellationToken)
    {
        if (photo is null || photo.Id.Length == 0)
            return null;
        IReadOnlyList<MediaRef> verified = await references.VerifyAsync([photo], cancellationToken);
        if (verified.Count == 0)
            return null;
        await references.UseAsync(Owner(report), verified, cancellationToken);
        string? scope = (await ministries.ScopesAsync(cancellationToken))[0];
        await folders.FileAsync(ReportPhotos, scope, ReportPhotos.Name, verified, cancellationToken);
        return verified[0];
    }

    public async Task ForgetAsync(Guid report, MediaRef? photo, CancellationToken cancellationToken)
    {
        await references.UseAsync(Owner(report), [], cancellationToken);
        if (photo is not null && photo.Id.Length > 0)
            await folders.ForgetAsync(ReportPhotos, [photo.Id], cancellationToken);
    }
}

internal sealed class ReportPhotoAccess(IAccessEvaluator access) : IMediaAreaAccess
{
    public string Area => OutreachAreas.ReportPhotos;

    public Task<bool> MayUploadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) =>
        Task.FromResult(Caller.UserId(user) is not null);

    public async Task<bool> MayReadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) =>
        await access.IsAllowedAsync(user, OutreachPermissions.ReviewNode, scopeId.Length == 0 ? null : scopeId, cancellationToken);

    public Task<bool> MayUploadFromInstallAsync(Guid install, string scopeId, CancellationToken cancellationToken) => Task.FromResult(true);
}
