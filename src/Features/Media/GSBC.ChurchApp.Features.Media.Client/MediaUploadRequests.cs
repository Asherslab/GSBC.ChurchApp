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

    public string Accepted(bool imagesOnly) => string.Join(",", Usable(imagesOnly));

    public string? ContentTypeOf(string fileName, string reported, bool imagesOnly)
    {
        string contentType = MediaTypes.ContentTypeOf(fileName, reported).ToLowerInvariant();
        return Usable(imagesOnly).Contains(contentType) ? contentType : null;
    }

    public string Unusable(string fileName, bool imagesOnly) => imagesOnly
        ? $"{fileName} isn't a picture this app can use. Choose a JPEG, PNG, WebP or GIF."
        : $"{fileName} isn't a picture or PDF this app can use.";

    public QueuedUpload For(StagedFile file, UploadTarget target, IReadOnlyList<MediaSlot> slots)
    {
        MediaAudience? privateTo = target.PrivateTo is { } permission ? new MediaAudience { Permission = permission, ScopeId = target.PrivateToScope } : null;
        return new QueuedUpload(file,
            new QueuedCall(FindByHash, Encode(new FindByHashRequest { Sha256 = file.Sha256, ContentType = file.ContentType, PrivateTo = privateTo })),
            new QueuedCall(BeginUpload, Encode(new BeginUploadRequest
            {
                FileName = file.Name, ContentType = file.ContentType, Bytes = file.Bytes, AltText = target.AltText, PrivateTo = privateTo,
            })),
            new QueuedCall(CompleteUpload, Encode(new CompleteUploadRequest())),
            slots);
    }

    private static IEnumerable<string> Usable(bool imagesOnly) =>
        MediaTypes.Accepted.Where(type => !MediaTypes.IsFont(type) && (!imagesOnly || type.StartsWith("image/", StringComparison.Ordinal)));

    private static byte[] Encode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T message)
    {
        using MemoryStream body = new();
        MediaContractsModel.Instance.Serialize(body, message);
        return body.ToArray();
    }
}
