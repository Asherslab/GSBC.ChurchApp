using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Media.Api;

internal static class MediaRefs
{
    public const int MaximumTextLength = 200;

    public static MediaRef Of(MediaItem item, IBlobStore blobs) => new()
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
}
