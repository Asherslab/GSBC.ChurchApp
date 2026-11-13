using System.Text.Json.Serialization;
using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Media;

[ProtoContract]
public sealed class MediaRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Key { get; set; } = "";
    [ProtoMember(3)] public string ContentType { get; set; } = "";
    [ProtoMember(4)] public long Bytes { get; set; }
    [ProtoMember(5)] public int Width { get; set; }
    [ProtoMember(6)] public int Height { get; set; }
    [ProtoMember(7)] public string? Url { get; set; }
    [ProtoMember(8)] public bool IsPrivate { get; set; }
    [ProtoMember(9)] public string AltText { get; set; } = "";
    [ProtoMember(10)] public string FileName { get; set; } = "";
    [ProtoMember(11)] public MediaSizeName Version { get; set; }
    [ProtoMember(12)] public List<MediaSize> Sizes { get; set; } = [];
    [ProtoMember(13)] public string Folder { get; set; } = "";

    [JsonIgnore] public bool IsImage => ContentType.StartsWith("image/", StringComparison.Ordinal);

    [JsonIgnore] public bool IsVideo => ContentType.StartsWith("video/", StringComparison.Ordinal);

    [JsonIgnore] public bool IsAudio => ContentType.StartsWith("audio/", StringComparison.Ordinal);

    [JsonIgnore] public bool IsFont => ContentType.StartsWith("font/", StringComparison.Ordinal);

    public MediaRef At(MediaSizeName size)
    {
        int wanted = MediaSizes.WidthOf(size);
        if (Sizes.Where(smaller => smaller.Width >= wanted).MinBy(smaller => smaller.Width) is not { } fit)
            return this;

        return new MediaRef
        {
            Id = Id,
            Key = fit.Key,
            ContentType = fit.ContentType,
            Bytes = fit.Bytes,
            Width = fit.Width,
            Height = fit.Height,
            Url = fit.Url,
            IsPrivate = IsPrivate,
            AltText = AltText,
            FileName = FileName,
            Version = fit.Name,
        };
    }
}

[ProtoContract]
public sealed class MediaSize
{
    [ProtoMember(1)] public MediaSizeName Name { get; set; }
    [ProtoMember(2)] public string Key { get; set; } = "";
    [ProtoMember(3)] public string ContentType { get; set; } = "";
    [ProtoMember(4)] public long Bytes { get; set; }
    [ProtoMember(5)] public int Width { get; set; }
    [ProtoMember(6)] public int Height { get; set; }
    [ProtoMember(7)] public string? Url { get; set; }
}

public enum MediaSizeName
{
    Original = 0,
    ForPhones = 1,
    Thumbnail = 2,
}

public static class MediaSizes
{
    public const int ForPhonesWidth = 1280;
    public const int ThumbnailWidth = 480;

    public static IReadOnlyList<MediaSizeName> Made { get; } = [MediaSizeName.ForPhones, MediaSizeName.Thumbnail];

    public static int WidthOf(MediaSizeName size) => size switch
    {
        MediaSizeName.ForPhones => ForPhonesWidth,
        MediaSizeName.Thumbnail => ThumbnailWidth,
        _ => int.MaxValue,
    };

    public static string LabelOf(MediaSizeName size) => size switch
    {
        MediaSizeName.ForPhones => "For phones",
        MediaSizeName.Thumbnail => "Thumbnail",
        _ => "Original",
    };

    public static string Weight(long bytes) => bytes < 1024 * 1024 ? $"{Math.Max(1, bytes / 1024d):0}\u00A0KB" : $"{bytes / 1024d / 1024d:0.#}\u00A0MB";

    public static IReadOnlyList<string> ShownAs(MediaRef version)
    {
        string dimensions = version.Width > 0 ? $"{version.Width}×{version.Height}" : "";
        return version.Version switch
        {
            MediaSizeName.ForPhones => [$"The app shows a {dimensions} copy", Weight(version.Bytes)],
            MediaSizeName.Thumbnail => [$"The app shows a {dimensions} thumbnail", Weight(version.Bytes)],
            _ => dimensions.Length > 0 ? ["The app shows the original", dimensions, Weight(version.Bytes)] : ["The app shows the original", Weight(version.Bytes)],
        };
    }
}
