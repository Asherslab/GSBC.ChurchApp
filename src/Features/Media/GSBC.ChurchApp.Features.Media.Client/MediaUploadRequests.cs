using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Uploads;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaUploadRequests : IUploadRequests
{
    public const string Service = "/gsbc.media.Media/";
    public const string FindByHash = Service + "FindByHash";
    public const string BeginUpload = Service + "BeginUpload";
    public const string CompleteUpload = Service + "CompleteUpload";

    public string Accepted(PickableFiles kinds) => string.Join(",", Usable(kinds));

    public string? ContentTypeOf(string fileName, string reported, PickableFiles kinds)
    {
        string contentType = MediaTypes.ContentTypeOf(fileName, reported).ToLowerInvariant();
        return Usable(kinds).Contains(contentType) ? contentType : null;
    }

    public string Unusable(string fileName, PickableFiles kinds) => kinds switch
    {
        PickableFiles.Pictures => $"{fileName} isn't a picture this app can use. Choose a JPEG, PNG, WebP or GIF.",
        PickableFiles.PicturesAndVideos => $"{fileName} isn't a picture or video this app can send. Choose a screenshot, photo or screen recording.",
        _ => $"{fileName} isn't a picture or PDF this app can use.",
    };

    public QueuedUpload For(StagedFile file, UploadTarget target, IReadOnlyList<MediaSlot> slots)
    {
        MediaAudience? privateTo = target.PrivateTo is { } permission ? new MediaAudience { Permission = permission, ScopeId = target.PrivateToScope } : null;
        return new QueuedUpload(file,
            new QueuedCall(FindByHash, Encode(new FindByHashRequest
            {
                Sha256 = file.Sha256, ContentType = file.ContentType, PrivateTo = privateTo, Area = target.Area, AreaScopeId = target.AreaScopeId,
            })),
            new QueuedCall(BeginUpload, Encode(new BeginUploadRequest
            {
                FileName = file.Name, ContentType = file.ContentType, Bytes = file.Bytes, AltText = target.AltText, PrivateTo = privateTo,
                Area = target.Area, AreaScopeId = target.AreaScopeId,
            })),
            new QueuedCall(CompleteUpload, Encode(new CompleteUploadRequest())),
            slots);
    }

    private static IEnumerable<string> Usable(PickableFiles kinds) => kinds switch
    {
        PickableFiles.Pictures => MediaTypes.Accepted.Where(IsPicture),
        PickableFiles.PicturesAndVideos => [.. MediaTypes.Accepted.Where(IsPicture), MediaTypes.Mp4, MediaTypes.QuickTime],
        _ => MediaTypes.Accepted.Where(type => !MediaTypes.IsFont(type)),
    };

    private static bool IsPicture(string type) => type.StartsWith("image/", StringComparison.Ordinal);

    private static byte[] Encode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T message)
    {
        using MemoryStream body = new();
        MediaContractsModel.Instance.Serialize(body, message);
        return body.ToArray();
    }
}
